using UnityEngine;

namespace ProjectileDash.Combat
{
    /// <summary>
    /// Abstract base for flight behavior.
    /// Controls translation each frame on the X/Y axes automatically via BaseProjectile.
    /// </summary>
    public abstract class FlightBehavior : ScriptableObject
    {
        public abstract void Initialize(Vector2 initialVelocity);
        public abstract Vector2 GetVelocity(Vector2 currentVelocity, float dt);
    }
}
