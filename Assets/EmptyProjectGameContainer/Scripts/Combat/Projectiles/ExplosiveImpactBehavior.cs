using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Example ImpactBehavior module: AoE blast that damages all IDamageable targets in a radius.
    /// 
    /// On impact:
    ///   1. Uses Physics2D.OverlapCircleAll to find targets inside the blast radius.
    ///   2. Calls IDamageable.TakeDamage on each valid target.
    ///   3. Optionally spawns a VFX prefab at the impact point.
    ///   4. Requests self-destruction via projectile.DestroyProjectile().
    /// 
    /// Designer Usage: Assign as the ImpactBehavior slot in ProjectileData to make any
    /// projectile explode on contact (e.g., DBZ Energy Blast, Grenade).
    /// 
    /// PILLAR COMPLIANCE:
    ///   • Uses Physics2D.OverlapCircleAll — stays on the 2D/2.5D plane.
    ///   • Does not modify any velocity directly; force vectors should be fed back via
    ///     the CustomKinematicBody's velocity pool (stubbed here as a TODO for the Physics Architect).
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectileDash/Impact Behaviors/Explosive", fileName = "ExplosiveImpactBehavior")]
    public class ExplosiveImpactBehavior : ImpactBehavior
    {
        [Header("Explosion Settings")]
        [Tooltip("Radius of the explosion in world units.")]
        [SerializeField] private float _blastRadius = 3f;

        [Tooltip("Layer mask for what the explosion can hit (e.g., 'Hurtbox', 'Enemy').")]
        [SerializeField] private LayerMask _hitLayers;

        [Tooltip("Optional VFX prefab to spawn at the impact point. Leave null to skip.")]
        [SerializeField] private GameObject _explosionVfxPrefab;

        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public override void OnImpact(BaseProjectile projectile, Collider2D other)
        {
            Vector2 blastCentre = new Vector2(projectile.transform.position.x, projectile.transform.position.y);

            // ── 1. Spawn VFX ──────────────────────────────────────────────────────
            if (_explosionVfxPrefab != null)
            {
                // Spawn flat on Z=0 to respect the 2.5D plane
                Instantiate(_explosionVfxPrefab, new Vector3(blastCentre.x, blastCentre.y, 0f), Quaternion.identity);
            }

            // ── 2. Find all targets in blast radius (2D only) ─────────────────────
            Collider2D[] hits = Physics2D.OverlapCircleAll(blastCentre, _blastRadius, _hitLayers);

            foreach (Collider2D hit in hits)
            {
                // Skip the projectile's own collider if it has one
                if (hit.transform == projectile.transform) continue;

                // Attempt to apply damage via the IDamageable interface
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Pass FinalDamage (already factored with ChargeResult.DamageMultiplier)
                    damageable.TakeDamage(projectile.FinalDamage, projectile.ActiveRecoilTier, blastCentre);
                }
            }

            // ── 3. Trigger Hitstop (if any targets were hit) ─────────────────────
            if (hits.Length > 0 && projectile.Data.HitstopDuration > 0f)
            {
                // TODO: Call HitstopManager / TimeScaleController once that system is built.
                // Stub log for now so the designer sees it working in the console.
                Debug.Log($"[ExplosiveImpactBehavior] Hitstop triggered: {projectile.Data.HitstopDuration}s (Tier {projectile.ActiveRecoilTier})");
            }

            // ── 4. Destroy the projectile ─────────────────────────────────────────
            projectile.DestroyProjectile();
        }
    }
}
