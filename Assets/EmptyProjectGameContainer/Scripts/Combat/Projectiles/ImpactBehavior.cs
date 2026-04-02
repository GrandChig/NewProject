using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Abstract base for impact behavior.
    /// What happens when the projectile touches a 2D collider.
    /// </summary>
    public abstract class ImpactBehavior : ScriptableObject
    {
        public abstract void OnImpact(BaseProjectile projectile, RaycastHit2D hit);
    }
}
