using System.Collections.Generic;
using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Example SpawnBehavior module: fires a single projectile in the aim direction.
    /// 
    /// This is the most fundamental spawn pattern — one projectile, no spread.
    /// Use as the SpawnBehavior slot for DBZ blasts, straight kunais, and rifle-style attacks.
    /// 
    /// PILLAR COMPLIANCE:
    ///   • Spawn position Z is zeroed before instantiation.
    ///   • BaseProjectile.Initialize is called immediately after Instantiate.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectileDash/Spawn Behaviors/Single Spawn", fileName = "SingleSpawnBehavior")]
    public class SingleSpawnBehavior : SpawnBehavior
    {
        /// <inheritdoc/>
        public override List<BaseProjectile> Spawn(
            ProjectileData          data,
            Vector2                 origin,
            Vector2                 aimDir,
            ChargeResult            charge,
            PlayerProjectileManager owner)
        {
            var results = new List<BaseProjectile>(1);

            if (data.ProjectilePrefab == null)
            {
                Debug.LogError("[SingleSpawnBehavior] ProjectileData.ProjectilePrefab is null — cannot spawn.");
                return results;
            }

            // ── Instantiate flat on Z=0 plane ─────────────────────────────────────
            Vector3 spawnPos = new Vector3(origin.x, origin.y, 0f);
            GameObject go    = Instantiate(data.ProjectilePrefab, spawnPos, Quaternion.identity);

            BaseProjectile projectile = go.GetComponent<BaseProjectile>();
            if (projectile == null)
            {
                Debug.LogError("[SingleSpawnBehavior] ProjectilePrefab does not have a BaseProjectile component!");
                Destroy(go);
                return results;
            }

            // ── Initialize (sets velocity, applies charge multipliers, registers camera) ──
            projectile.Initialize(data, aimDir, owner, charge);
            results.Add(projectile);

            return results;
        }
    }
}
