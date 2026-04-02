using System.Collections.Generic;
using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Abstract base for spawn behavior.
    /// Controls how exactly the projectiles enter the world.
    /// </summary>
    public abstract class SpawnBehavior : ScriptableObject
    {
        public abstract List<BaseProjectile> Spawn(
            ProjectileData data,
            Vector2        origin,
            Vector2        aimDir,
            ChargeResult   charge,
            PlayerProjectileManager owner);
    }
}
