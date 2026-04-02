using UnityEngine;

namespace ProjectileDash.Combat
{
    public struct ChargeResult
    {
        public float DamageMultiplier;
        public float SpeedMultiplier;
        public float SizeMultiplier;
        public int RecoilTierOverride;

        public static ChargeResult Default => new ChargeResult
        {
            DamageMultiplier  = 1f,
            SpeedMultiplier   = 1f,
            SizeMultiplier    = 1f,
            RecoilTierOverride = 0
        };
    }

    /// <summary>
    /// Abstract base for charge behavior.
    /// Drives what happens when holding the shoot button.
    /// </summary>
    public abstract class ChargeBehavior : ScriptableObject
    {
        public abstract void OnChargeStart();
        public abstract void OnChargeTick(float dt);
        public abstract ChargeResult OnChargeRelease();

        /// <summary>
        /// Returns the current multipliers based on how long the button has been held.
        /// Useful for real-time UI bars or trajectory line scaling.
        /// </summary>
        public abstract ChargeResult GetCurrentResult();
    }
}
