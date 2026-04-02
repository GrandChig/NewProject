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
    [RequireComponent(typeof(Collider2D))]
    public class BaseProjectile : MonoBehaviour
    {
        // ─────────────────── Runtime State ────────────────────────────────────────

        private ProjectileData _data;
        private PlayerProjectileManager _owner;

        /// <summary>Current velocity vector (world units/sec) used by FlightBehavior.</summary>
        private Vector2 _velocity;

        /// <summary>Total distance traveled since spawn, checked against MaxRange.</summary>
        private float _distanceTraveled;

        /// <summary>Cached initial damage after charge multiplier is applied.</summary>
        public float FinalDamage { get; private set; }

        /// <summary>Active Recoil Tier after any charge override.</summary>
        public int ActiveRecoilTier { get; private set; }

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

        private void Awake()
        {
            // Register with camera the moment the GO is created (before Initialize may be called)
            if (DynamicCameraManager.Instance != null)
                DynamicCameraManager.Instance.RegisterProjectile(transform);
            else
                Debug.LogWarning($"[BaseProjectile] DynamicCameraManager.Instance is null — camera framing will not include this projectile.");
        }

        private void Update()
        {
            if (_isDestroyed || _data == null) return;

            // ── 1. Ask FlightBehavior for this frame's velocity ──────────────────
            if (_data.FlightBehavior != null)
                _velocity = _data.FlightBehavior.GetVelocity(_velocity, Time.deltaTime);

            // ── 2. Move via transform (never Rigidbody) ──────────────────────────
            Vector2 displacement = _velocity * Time.deltaTime;
            Vector3 newPos       = transform.position + new Vector3(displacement.x, displacement.y, 0f);
            newPos.z             = 0f; // Enforce 2.5D — Z is always 0 (CORE_PILLAR #3)
            transform.position   = newPos;

            // ── 3. Track range and destroy if limit exceeded ─────────────────────
            _distanceTraveled += displacement.magnitude;
            if (_distanceTraveled >= _data.MaxRange)
            {
                DestroyProjectile();
            }
        }

        // ─────────────────── Collision (2D — enforces 2.5D pillar) ────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDestroyed) return;

            _data?.ImpactBehavior?.OnImpact(this, other);
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
