using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Abstract base for teleport reaction.
    /// Called when the player warps to the projectile.
    /// </summary>
    public abstract class TeleportReaction : ScriptableObject
    {
        public abstract bool OnPlayerTeleportedTo(BaseProjectile projectile);
    }
}
