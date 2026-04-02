namespace ProjectileDash.Combat
{
    /// <summary>
    /// Common interface for any object that can receive damage from a projectile impact.
    /// Implement this on player characters, NPC enemies, or destructible environment pieces.
    /// 
    /// The damage pipeline is intentionally minimal here — the Combat Architect can expand
    /// TakeDamage with knockback vectors, damage types, invincibility frames, etc. as the
    /// game grows without modifying the projectile system.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this object.
        /// </summary>
        /// <param name="amount">Final damage after all multipliers have been applied.</param>
        /// <param name="recoilTier">1 = Stagger, 2 = Rubber Launch, 3 = Heavy Blast.</param>
        /// <param name="sourcePosition">2D world position of the impact/blast center, for knockback direction calculation.</param>
        void TakeDamage(float amount, int recoilTier, UnityEngine.Vector2 sourcePosition);
    }
}
