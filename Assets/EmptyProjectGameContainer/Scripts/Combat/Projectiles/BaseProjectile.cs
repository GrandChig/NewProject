using UnityEngine;
using ProjectileDash.Core;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Runtime MonoBehaviour attached to every instantiated projectile prefab.
    /// 
    /// PILLAR COMPLIANCE:
    ///   • Movement is exclusively via transform.position (no Rigidbody forces).
    ///   • Z is locked to 0 on every position write.
    ///   • Collision detection uses Physics2D (OnTriggerEnter2D) to enforce 2.5D.
    ///   • Registers/unregisters with DynamicCameraManager for framing.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class BaseProjectile : MonoBehaviour
    {
        private CircleCollider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();

            // Register with camera for framing
            if (DynamicCameraManager.Instance != null)
                DynamicCameraManager.Instance.RegisterProjectile(transform);
        }

        // ─────────────────── Runtime State ────────────────────────────────────────

        private ProjectileData _data;
        private PlayerProjectileManager _owner;

        /// <summary>Current velocity vector (world units/sec) used by FlightBehavior.</summary>
        public Vector2 CurrentVelocity => _velocity;
        private Vector2 _velocity;

        /// <summary>Allows Behaviors to override the current flight path (e.g. for Bouncing).</summary>
        public void SetVelocity(Vector2 newVelocity) => _velocity = newVelocity;

        /// <summary>Total distance traveled since spawn, checked against MaxRange.</summary>
        private float _distanceTraveled;

        /// <summary>Cached initial damage after charge multiplier is applied.</summary>
        public float FinalDamage { get; private set; }

        /// <summary>Active Recoil Tier after any charge override.</summary>
        public int ActiveRecoilTier { get; private set; }

        /// <summary>How many times this specific projectile has bounced off the environment.</summary>
        public int ActiveBounceCount { get; set; }

        /// <summary>True once DestroyProjectile has been called; prevents re-entrance.</summary>
        private bool _isDestroyed;

        // ─────────────────── Public Accessors ─────────────────────────────────────

        /// <summary>The ScriptableObject that defines all this projectile's stats and behaviors.</summary>
        public ProjectileData Data => _data;

        /// <summary>The PlayerProjectileManager that spawned this projectile.</summary>
        public PlayerProjectileManager Owner => _owner;

        // ─────────────────── Initialization ───────────────────────────────────────

        /// <summary>
        /// Called by ISpawnBehavior immediately after Instantiate.
        /// Must be called before the first Update.
        /// </summary>
        public void Initialize(ProjectileData data, Vector2 aimDir, PlayerProjectileManager owner, ChargeResult charge)
        {
            _data  = data;
            _owner = owner;

            // Apply charge multipliers to base stats
            FinalDamage     = data.BaseDamage  * charge.DamageMultiplier;
            float speed     = data.BaseSpeed   * charge.SpeedMultiplier;
            ActiveRecoilTier = (charge.RecoilTierOverride > 0 ? charge.RecoilTierOverride : data.RecoilTier);

            // Apply size multiplier to the transform (uniform)
            transform.localScale *= charge.SizeMultiplier;

            // Seat the projectile firmly on the Z=0 plane (CORE_PILLAR #3)
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            // Hand the initial velocity to the FlightBehavior so it can cache direction/state
            _velocity = aimDir.normalized * speed;
            data.FlightBehavior?.Initialize(_velocity);
        }

        // ─────────────────── Unity Lifecycle ──────────────────────────────────────

        private void Update()
        {
            if (_isDestroyed || _data == null) return;

            // ── 1. Ask FlightBehavior for this frame's velocity ──────────────────
            if (_data.FlightBehavior != null)
                _velocity = _data.FlightBehavior.GetVelocity(_velocity, Time.deltaTime);

            // ── 2. Swept Collision (CORE_PILLAR: Kinematic Movement) ──────────────
            float frameDistance = _velocity.magnitude * Time.deltaTime;
            Vector2 direction   = _velocity.normalized;

                if (frameDistance > 0)
                {
                    // PILLAR COMPLIANCE: Use a static query (CircleCast) like the CustomPhysicsController's BoxCast.
                    // This bypasses the Project Settings Collision Matrix and relies purely on the Data's LayerMask.
                    float radius = _collider.radius * transform.localScale.x;
                    RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, direction, frameDistance + 0.05f, _data.CollisionMask);

                    if (hit)
                    {
                        Debug.Log($"[BaseProjectile] Collision Detected with {hit.collider.name} at {hit.point} (Normal: {hit.normal})");
                        
                        // Move to the contact point + a tiny nudge along the normal to prevent sticking
                        transform.position = (Vector3)hit.centroid + (Vector3)hit.normal * 0.01f;
                        
                        // Trigger Impact with full hit data
                        _data?.ImpactBehavior?.OnImpact(this, hit);
                        
                        return;
                    }
                }

            // ── 3. Normal Movement (if no hit) ──────────────────────────────────
            Vector2 displacement = _velocity * Time.deltaTime;
            Vector3 targetPos    = transform.position + new Vector3(displacement.x, displacement.y, 0f);
            targetPos.z          = 0f;
            transform.position   = targetPos;

            // ── 4. Track range ───────────────────────────────────────────────────
            _distanceTraveled += displacement.magnitude;
            if (_distanceTraveled >= _data.MaxRange)
            {
                DestroyProjectile();
            }
        }

        // ─────────────────── Debug Gizmos ─────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (_collider == null) _collider = GetComponent<CircleCollider2D>();
            if (_collider == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _collider.radius * transform.localScale.x);
        }

        // ─────────────────── Teleport API ─────────────────────────────────────────

        /// <summary>
        /// Called by PlayerProjectileManager when the player executes a teleport to this projectile.
        /// CORE_PILLAR COMPLIANCE: this method ONLY handles the projectile side.
        /// The caller is responsible for updating player transform.position.
        /// CurrentVelocity on the player MUST NOT be touched here.
        /// </summary>
        public void OnPlayerTeleportedTo()
        {
            if (_isDestroyed) return;

            bool shouldDestroy = _data?.TeleportReaction?.OnPlayerTeleportedTo(this) ?? true;

            if (shouldDestroy)
                DestroyProjectile();
        }

        // ─────────────────── Destruction ──────────────────────────────────────────

        /// <summary>
        /// The single, canonical way to destroy a projectile.
        /// Unregisters from camera, removes from owner's active list, then destroys the GameObject.
        /// Call this from IImpactBehavior and ITeleportReaction — never call Destroy() directly.
        /// </summary>
        public void DestroyProjectile()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            // Notify the owner to remove from active list before destroying
            _owner?.OnProjectileDestroyed(this);

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Safety net: always unregister from camera even if destroyed by external means (e.g. blast zone)
            if (DynamicCameraManager.Instance != null)
                DynamicCameraManager.Instance.UnregisterProjectile(transform);
        }
    }
}
