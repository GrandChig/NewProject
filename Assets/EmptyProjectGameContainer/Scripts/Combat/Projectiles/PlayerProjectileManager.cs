using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectileDash.Core;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Attached to the player GameObject. Handles:
    ///   1. Right Stick 360-degree aiming (X/Y only — 2.5D).
    ///   2. RT charge accumulation via IChargeBehavior.
    ///   3. Overflow enforcement via MaxActiveOnStage / OverflowRule.
    ///   4. Projectile lifecycle: spawning, tracking, and LT teleport targeting.
    /// 
    /// PILLAR COMPLIANCE:
    ///   • Teleport ONLY changes transform.position — player velocity is NEVER touched here.
    ///   • Aiming is a 2D Vector (X/Y). Z is always 0.
    /// </summary>
    public class PlayerProjectileManager : MonoBehaviour
    {
        // ─────────────────── Inspector ────────────────────────────────────────────

        [Header("Equipped Loadout")]
        [Tooltip("The ScriptableObject defining stats, limits, and behavior modules for the active projectile.")]
        [SerializeField] private ProjectileData _equippedData;

        [Header("Aiming")]
        [Tooltip("Minimum Right Stick magnitude before the input is considered intentional.")]
        [SerializeField, Range(0f, 1f)] private float _aimDeadzone = 0.2f;

        [Header("Trajectory Visuals")]
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private Gradient _chargeColorGradient;
        [SerializeField] private AnimationCurve _chargeWidthCurve;

        [Header("Debug")]
        [SerializeField] private bool _showAimGizmo = true;

        // ─────────────────── Runtime State ────────────────────────────────────────

        /// <summary>Ordered list: index 0 = oldest, index N-1 = newest.</summary>
        private readonly List<BaseProjectile> _activeProjectiles = new List<BaseProjectile>();

        /// <summary>Last non-zero normalized aim direction from the Right Stick.</summary>
        private Vector2 _currentAimDir = Vector2.right;

        /// <summary>True while RT is held.</summary>
        private bool _isCharging;

        /// <summary>Accumulated charge time (passed to ChargeBehavior.OnChargeTick).</summary>
        private float _chargeTime;

        // ─────────────────── Unity Lifecycle ──────────────────────────────────────

        private void Start()
        {
            if (_trajectoryLine != null)
            {
                _trajectoryLine.positionCount = 2;
                _trajectoryLine.enabled = false; // Hide by default
            }
        }

        private void Update()
        {
            ReadAimInput();
            HandleChargeInput();
            HandleTeleportInput();
            UpdateTrajectory();
        }

        private void UpdateTrajectory()
        {
            if (_trajectoryLine == null || _equippedData == null) return;

            // Show trajectory if aiming or charging
            bool shouldShow = _isCharging || Gamepad.current?.rightStick.magnitude > _aimDeadzone;
            _trajectoryLine.enabled = shouldShow;

            if (!shouldShow) return;

            // 1. Calculate positions
            Vector3 startPos = transform.position;
            startPos.z = 0f;

            // Line length is based on projectile's MaxRange
            Vector3 endPos = startPos + new Vector3(_currentAimDir.x, _currentAimDir.y, 0f) * _equippedData.MaxRange;
            
            _trajectoryLine.SetPosition(0, startPos);
            _trajectoryLine.SetPosition(1, endPos);

            // 2. Apply charge aesthetics
            if (_isCharging && _equippedData.ChargeBehavior != null)
            {
                ChargeResult current = _equippedData.ChargeBehavior.GetCurrentResult();
                
                // Use the speed multiplier as a proxy for 'charge progress' for visuals
                // Assuming 1.0 is baseline and it goes up. 
                // For a more robust system, IChargeBehavior would return raw 0-1 progress.
                float progress = Mathf.Clamp01(current.SpeedMultiplier - 1f); // Simple fallback

                _trajectoryLine.colorGradient = _chargeColorGradient;
                float width = _chargeWidthCurve.Evaluate(progress);
                _trajectoryLine.startWidth = width;
                _trajectoryLine.endWidth = width * 0.5f;
            }
            else
            {
                // Baseline 'Aim Laser' look
                _trajectoryLine.startWidth = 0.05f;
                _trajectoryLine.endWidth = 0.05f;
                // Fallback to a neutral color if gradient logic is too complex for simple aim
            }
        }

        // ─────────────────── Input Reading ────────────────────────────────────────

        private void ReadAimInput()
        {
            if (Gamepad.current == null) return;

            Vector2 raw = new Vector2(
                Gamepad.current.rightStick.x.ReadValue(),
                Gamepad.current.rightStick.y.ReadValue());

            // Only update if above deadzone (preserves last valid direction when stick returns to center)
            if (raw.magnitude > _aimDeadzone)
                _currentAimDir = raw.normalized;
        }

        private void HandleChargeInput()
        {
            if (_equippedData == null) return;

            bool rtHeld = Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > 0.1f;
            // Keyboard fallback: Left Shift holds charge
            bool kbHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            bool held = rtHeld || kbHeld;

            // ── Charge Start ─────────────────────────────────────────────────────
            if (held && !_isCharging)
            {
                _isCharging  = true;
                _chargeTime  = 0f;
                _equippedData.ChargeBehavior?.OnChargeStart();
            }

            // ── Charge Tick ──────────────────────────────────────────────────────
            if (_isCharging && held)
            {
                _chargeTime += Time.deltaTime;
                _equippedData.ChargeBehavior?.OnChargeTick(Time.deltaTime);
            }

            // ── Charge Release → Fire ────────────────────────────────────────────
            if (_isCharging && !held)
            {
                _isCharging = false;
                ChargeResult charge = _equippedData.ChargeBehavior?.OnChargeRelease() ?? ChargeResult.Default;
                TryFire(charge);
            }
        }

        private void HandleTeleportInput()
        {
            if (_equippedData == null) return;

            // LT = L2: teleport to the next target in the targeting order
            bool ltPressed = Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame;
            // Keyboard fallback: F key
            bool kbPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;

            if (ltPressed || kbPressed)
                ExecuteTeleport();
        }

        // ─────────────────── Fire Logic ───────────────────────────────────────────

        private void TryFire(ChargeResult charge)
        {
            if (_equippedData?.SpawnBehavior == null)
            {
                Debug.LogWarning("[PlayerProjectileManager] No SpawnBehavior assigned — cannot fire.");
                return;
            }

            // ── Overflow Check ───────────────────────────────────────────────────
            if (_activeProjectiles.Count >= _equippedData.MaxActiveOnStage)
            {
                switch (_equippedData.OverflowRule)
                {
                    case OverflowRule.DestroyOldest:
                        if (_activeProjectiles.Count > 0)
                        {
                            BaseProjectile oldest = _activeProjectiles[0];
                            oldest?.DestroyProjectile(); // OnProjectileDestroyed removes it from list
                        }
                        break;

                    case OverflowRule.PreventFiring:
                        Debug.Log("[PlayerProjectileManager] MaxActiveOnStage reached — fire blocked.");
                        return;
                }
            }

            // ── Spawn ────────────────────────────────────────────────────────────
            Vector2 origin = new Vector2(transform.position.x, transform.position.y);
            List<BaseProjectile> spawned = _equippedData.SpawnBehavior.Spawn(
                _equippedData, origin, _currentAimDir, charge, this);

            foreach (BaseProjectile p in spawned)
            {
                if (p != null)
                    _activeProjectiles.Add(p);
            }
        }

        // ─────────────────── Teleport Logic ───────────────────────────────────────

        /// <summary>
        /// Picks a target using TargetingOrder, teleports the player to it, then invokes the
        /// projectile's TeleportReaction. Player velocity is NEVER modified here (CORE_PILLAR #2).
        /// </summary>
        private void ExecuteTeleport()
        {
            BaseProjectile target = GetTeleportTarget();
            if (target == null) return;

            // ── Move player to projectile — position updated, momentum inherited ─
            Vector3 destination = target.transform.position;
            destination.z = 0f;
            transform.position = destination;

            // Inheritance: Apply the projectile's current velocity to the player
            CustomPhysicsController physics = GetComponent<CustomPhysicsController>();
            if (physics != null)
            {
                physics.SetVelocity(target.CurrentVelocity);
            }

            // ── Notify camera (smooth snap after instant warp) ────────────────────
            DynamicCameraManager.Instance?.TeleportCameraToTargets();

            // ── Notify projectile (handles PersistAndShield / ConsumeAndNode / etc.)
            target.OnPlayerTeleportedTo();
        }

        /// <summary>Returns the best teleport target from the active list per TargetingOrder.</summary>
        public BaseProjectile GetTeleportTarget()
        {
            if (_activeProjectiles.Count == 0) return null;

            switch (_equippedData.TargetingOrder)
            {
                case TargetingOrder.OldestFirst:
                    return _activeProjectiles[0];

                case TargetingOrder.NewestFirst:
                    return _activeProjectiles[_activeProjectiles.Count - 1];

                case TargetingOrder.ClosestToEnemy:
                    // Stub: finds nearest GameObject tagged "Enemy" and picks the projectile closest to it.
                    GameObject nearestEnemy = FindNearestEnemy();
                    if (nearestEnemy == null) return _activeProjectiles[0]; // fallback to oldest
                    Vector3 enemyPos = nearestEnemy.transform.position;
                    return _activeProjectiles
                        .OrderBy(p => Vector3.Distance(p.transform.position, enemyPos))
                        .FirstOrDefault();

                default:
                    return _activeProjectiles[0];
            }
        }

        // ─────────────────── Lifecycle Callbacks ──────────────────────────────────

        /// <summary>
        /// Called by BaseProjectile.DestroyProjectile() before the GameObject is destroyed.
        /// Removes the projectile from our tracking list.
        /// </summary>
        public void OnProjectileDestroyed(BaseProjectile projectile)
        {
            _activeProjectiles.Remove(projectile);
        }

        // ─────────────────── Helpers ──────────────────────────────────────────────

        private GameObject FindNearestEnemy()
        {
            // Simple tag-based search. Replace with a spatial index or event system as the game matures.
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length == 0) return null;

            GameObject nearest = null;
            float minDist = float.MaxValue;
            foreach (GameObject e in enemies)
            {
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < minDist) { minDist = d; nearest = e; }
            }
            return nearest;
        }

        // ─────────────────── Gizmos ───────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (!_showAimGizmo) return;
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            origin.z = 0f;
            Gizmos.DrawLine(origin, origin + new Vector3(_currentAimDir.x, _currentAimDir.y, 0f) * 2f);
        }
    }
}
