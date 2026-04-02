# PROJECTILE DASH: CORE DEVELOPMENT PILLARS
**CRITICAL DIRECTIVE:** ALL agents must read and adhere to these rules before writing any code. If a request violates these rules, STOP and ask the user for clarification.

1. **UNITY ENGINE CONSTRAINTS:**
   - We are using Unity.
   - Do NOT use Unity's default `Rigidbody` physics for the player.
   - Do NOT use Unity's default `CharacterController` component.
   - Player movement MUST be handled via a 100% Custom Kinematic physics script manipulating `transform.position` via custom velocity vectors, calculated in `Update` (or `FixedUpdate` if strictly handling collision resolution).

2. **THE LAW OF MOMENTUM (VECTOR PRESERVATION):**
   - The player's `CurrentVelocity` (Vector2/Vector3 mapped to 2D) is sacred.
   - Actions like stopping, charging, or attacking do NOT instantly set velocity to zero. We use gradual friction/drag.
   - Teleportation ONLY changes `transform.position`. It MUST NEVER reset, alter, or zero out the `CurrentVelocity` vector. 

3. **ELASTIC PHYSICS (RUBBERY FEEL):**
   - The game is strictly 2.5D. All movement, aiming, and physics calculations happen ONLY on the X (horizontal) and Y (vertical) axes. Lock the Z-axis position to 0 for all gameplay logic. This means utilizing `Physics2D` or zeroed-extents 3D raycasts.
   - Bounces: High-speed impacts with environment surfaces MUST trigger elastic reactions.
     - **Ground Bounces:** Downward knockback into the floor must trigger a "Ground Bounce" (reversing the Y velocity while maintaining a high percentage of the X velocity).
     - **Wall Bounces:** Horizontal impacts at high speeds trigger a "Wall Bounce" (reversing the X velocity while maintaining a percentage of the Y velocity).

4. **MODULARITY & READABILITY:**
   - Keep scripts strictly decoupled. The State Machine should not do physics math; it should only tell the Physics script what state it is in. 
   - Ensure specific logic segments (like allowing Teleportation while "Launched") have modular checks (e.g., a "CanTeleportInState" system) so design iteration is easy and doesn't require hardcoding values.
   - Expose public variables (like Min/Max Weight, Gravity Multiplier, Friction, and Bounce Elasticity coefficients) to the Unity Inspector with `[SerializeField]` so the Game Designer can tweak them without touching code.