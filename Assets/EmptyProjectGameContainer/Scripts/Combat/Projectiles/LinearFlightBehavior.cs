using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Example FlightBehavior module: moves the projectile in a constant straight line.
    /// Zero acceleration, zero steering — just velocity held at the initial value.
    /// 
    /// Designer Usage: Assign as the FlightBehavior slot in any ProjectileData SO
    /// where you want simple straight-line movement (e.g., DBZ energy blast, rifle bullet).
    /// 
    /// PILLAR COMPLIANCE: Returns a Vector2; BaseProjectile applies it via transform.position.
    /// No Rigidbody forces are used.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectileDash/Flight Behaviors/Linear", fileName = "LinearFlightBehavior")]
    public class LinearFlightBehavior : FlightBehavior
    {
        private Vector2 _initialVelocity;

        /// <inheritdoc/>
        public override void Initialize(Vector2 initialVelocity)
        {
            // Cache the direction × speed supplied by BaseProjectile.Initialize.
            _initialVelocity = initialVelocity;
        }

        /// <inheritdoc/>
        public override Vector2 GetVelocity(Vector2 currentVelocity, float dt)
        {
            // Pure linear: velocity never changes. The projectile travels in a perfectly straight line.
            return _initialVelocity;
        }
    }
}
