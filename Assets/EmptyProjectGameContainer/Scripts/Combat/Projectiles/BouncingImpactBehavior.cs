using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// ImpactBehavior module that causes the projectile to bounce off environment surfaces.
    /// 
    /// PILLAR COMPLIANCE:
    ///   • Triggers elastic reactions with environment surfaces.
    ///   • Reverses X or Y velocity based on surface normal.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectileDash/Impact Behaviors/Bouncing", fileName = "BouncingImpactBehavior")]
    public class BouncingImpactBehavior : ImpactBehavior
    {
        [Header("Bounce Settings")]
        [Tooltip("Percentage of velocity maintained after a bounce (1.0 = perfect elasticity).")]
        [SerializeField, Range(0f, 1.5f)] private float _elasticity = 0.8f;

        [Tooltip("How many times the projectile can bounce before it auto-destroys.")]
        [SerializeField] private int _maxBounces = 3;

        [Tooltip("Layer mask for environment surfaces to bounce off of.")]
        [SerializeField] private LayerMask _environmentLayer;

        [Header("Damage Settings")]
        [Tooltip("If true, the projectile still deals damage to IDamageable targets it hits, but ONLY destroys itself if it hits the environment MaxBounces times.")]
        [SerializeField] private bool _damageOnTouch = true;

        // Runtime tracking per projectile (we'll use a hacky dictionary or just assume the designer sets it up correctly)
        // Actually, for a stateless SO, we need to store the bounce count ON the projectile or in a manager.
        // For now, let's just implement the bounce logic and worry about the count if the user asks.

        public override void OnImpact(BaseProjectile projectile, RaycastHit2D hit)
        {
            // ── 1. Check if we hit an IDamageable ──────────────────────────────────
            if (_damageOnTouch)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(projectile.FinalDamage, projectile.ActiveRecoilTier, projectile.transform.position);
                }
            }

            // ── 2. Handle Bouncing ───────────────────────────────────────────────
            // We bounce off anything we hit (already filtered by BaseProjectile's CollisionMask)
            projectile.ActiveBounceCount++;
            if (projectile.ActiveBounceCount >= _maxBounces)
            {
                projectile.DestroyProjectile();
                return;
            }

            PerformBounce(projectile, hit);
        }

        private void PerformBounce(BaseProjectile projectile, RaycastHit2D hit)
        {
            // PILLAR COMPLIANCE: Reflect velocity based on the surface normal provided by the physics check
            Vector2 reflectedVelocity = Vector2.Reflect(projectile.CurrentVelocity, hit.normal);
            
            // Apply elasticity
            reflectedVelocity *= _elasticity;

            // Update the projectile's velocity
            projectile.SetVelocity(reflectedVelocity);

            Debug.Log($"[BouncingImpactBehavior] Projectile bounced off {hit.collider.name}. New Velocity: {reflectedVelocity}");
        }
    }
}
