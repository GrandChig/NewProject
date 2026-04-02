using UnityEngine;

namespace ProjectileDash.Combat
{
    // ─── Enums live here so they are accessible to any script without coupling ───

    public enum OverflowRule
    {
        /// <summary>Automatically destroys the oldest active projectile before firing a new one.</summary>
        DestroyOldest,
        /// <summary>Blocks the fire action entirely until the player is below the active limit.</summary>
        PreventFiring
    }

    public enum TargetingOrder
    {
        /// <summary>LT teleports to whichever projectile was fired first.</summary>
        OldestFirst,
        /// <summary>LT teleports to whichever projectile was fired most recently.</summary>
        NewestFirst,
        /// <summary>LT teleports to whichever active projectile is currently closest to the nearest enemy.</summary>
        ClosestToEnemy
    }

    /// <summary>
    /// Master ScriptableObject blueprint for a projectile type.
    /// All stats, stage limits, and behavior module slots live here so designers
    /// can create new "attack presets" in the Project window without touching code.
    /// 
    /// Behavior slots use [SerializeReference] so any concrete class that implements
    /// the interface — including nested ScriptableObject subclasses — can be assigned
    /// directly in the Inspector via the Unity 2021+ polymorphic serialization drawer.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectileDash/Projectile Data", fileName = "NewProjectileData")]
    public class ProjectileData : ScriptableObject
    {
        // ─────────────────────────── Universal Stats ─────────────────────────────

        [Header("Core Stats")]
        [Tooltip("Base damage dealt on impact, before ChargeResult.DamageMultiplier is applied.")]
        [SerializeField] public float BaseDamage = 10f;

        [Tooltip("Base travel speed in world units per second. Scaled by ChargeResult.SpeedMultiplier.")]
        [SerializeField] public float BaseSpeed = 15f;

        [Tooltip("Maximum distance in world units the projectile can travel before auto-destroying. " +
                 "Measured as cumulative displacement each frame.")]
        [SerializeField] public float MaxRange = 30f;

        [Tooltip("Duration in seconds of the hitstop freeze applied to both players on impact.")]
        [SerializeField] public float HitstopDuration = 0.08f;

        [Tooltip("1 = Stagger (no recoil), 2 = Rubber Launch, 3 = Heavy Blast. " +
                 "Overridden by ChargeResult.RecoilTierOverride if > 0.")]
        [SerializeField, Range(1, 3)] public int RecoilTier = 1;

        // ─────────────────────────── Stage Limits ────────────────────────────────

        [Header("Stage & Targeting Limits")]
        [Tooltip("Maximum number of instances of this projectile allowed on stage per player at once. " +
                 "Example: 1 for DBZ Energy Blast, 8 for Assassin Kunais.")]
        [SerializeField] public int MaxActiveOnStage = 1;

        [Tooltip("What to do when the player tries to fire and the limit is already reached.")]
        [SerializeField] public OverflowRule OverflowRule = OverflowRule.DestroyOldest;

        [Tooltip("Which active projectile LT targets when the player presses Teleport.")]
        [SerializeField] public TargetingOrder TargetingOrder = TargetingOrder.OldestFirst;

        // ─────────────────────────── Prefab ──────────────────────────────────────

        [Header("Prefab")]
        [Tooltip("The GameObject to Instantiate. Must have a BaseProjectile component.")]
        [SerializeField] public GameObject ProjectilePrefab;

        // ─────────────────────────── Behavior Slots ──────────────────────────────

        [Header("Behavior Slots")]
        [Tooltip("Charge slot — how holding Shoot alters the projectile. " +
                 "Leave null for a no-charge / instant-fire preset.")]
        [SerializeField] public ChargeBehavior ChargeBehavior;

        [Tooltip("Spawn slot — how projectile(s) enter the world at fire time. " +
                 "E.g. SingleSpawn, ConeSpread, DelayedSequence.")]
        [SerializeField] public SpawnBehavior SpawnBehavior;

        [Tooltip("Flight slot — how the projectile moves each frame (transform-based, no forces). " +
                 "E.g. LinearFlight, BoomerangFlight.")]
        [SerializeField] public FlightBehavior FlightBehavior;

        [Tooltip("Impact slot — what happens when the projectile hits a collider. " +
                 "E.g. DirectImpact, ExplosiveImpact, StickToWall.")]
        [SerializeField] public ImpactBehavior ImpactBehavior;

        [Tooltip("Teleport Reaction slot — what the projectile does when the player arrives at it. " +
                 "E.g. PersistAndShield, ConsumeAndNode, Detonate.")]
        [SerializeField] public TeleportReaction TeleportReaction;
    }
}
