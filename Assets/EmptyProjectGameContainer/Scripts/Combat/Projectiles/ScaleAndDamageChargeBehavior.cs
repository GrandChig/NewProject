using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Example ChargeBehavior: Grows the projectile's damage, speed, and size over time
    /// as the player holds the charge button.
    /// 
    /// Designer Usage: Create an asset from this class and assign it to a ProjectileData's 
    /// ChargeBehavior slot. Configure the curves to define how 'heavy' the shot feels.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectileDash/Charge Behaviors/Scale and Damage", fileName = "ScaleAndDamageCharge")]
    public class ScaleAndDamageChargeBehavior : ChargeBehavior
    {
        [Header("Tiers")]
        [Tooltip("How many seconds of hold time to reach full charge.")]
        [SerializeField] private float _maxChargeTime = 1.5f;

        [Header("Multipliers")]
        [SerializeField] private AnimationCurve _damageMultiplierCurve = AnimationCurve.Linear(0, 1, 1, 3);
        [SerializeField] private AnimationCurve _speedMultiplierCurve = AnimationCurve.Linear(0, 1, 1, 2);
        [SerializeField] private AnimationCurve _sizeMultiplierCurve = AnimationCurve.Linear(0, 1, 1, 1.5f);

        [Header("Recoil Upgrades")]
        [Tooltip("Recoil tier applies at 100% charge (e.g. set to 3 for a Heavy Blast at max charge).")]
        [SerializeField] private int _maxChargeRecoilTier = 3;

        private float _currentHoldTime;

        public override void OnChargeStart()
        {
            _currentHoldTime = 0f;
        }

        public override void OnChargeTick(float dt)
        {
            _currentHoldTime += dt;
        }

        public override ChargeResult OnChargeRelease()
        {
            return GetCurrentResult();
        }

        public override ChargeResult GetCurrentResult()
        {
            float t = Mathf.Clamp01(_currentHoldTime / _maxChargeTime);

            return new ChargeResult
            {
                DamageMultiplier = _damageMultiplierCurve.Evaluate(t),
                SpeedMultiplier = _speedMultiplierCurve.Evaluate(t),
                SizeMultiplier = _sizeMultiplierCurve.Evaluate(t),
                RecoilTierOverride = (t >= 1f) ? _maxChargeRecoilTier : 0
            };
        }
    }
}
