# 📜 Game Design Document: Projectile Dash (MVP)

## 1. High-Level Concept
**Elevator Pitch:** An anime-inspired, 2.5D physics-driven platform fighter where mobility and offense are tied to a dynamic projectile system. Players utilize a twin-stick control scheme to shoot projectiles, instantly teleport to them, and manipulate a highly elastic, "rubbery" physics engine. Masterful control of inertia, recoil, and momentum preservation is required to execute fast-paced, vertical combat and dynamic aerial combos on floating arenas.

**Win Condition:** Smash Bros-style elimination. Players build up damage on opponents to increase their knockback multiplier, ultimately launching them off the floating arena into the blast zones.

---

## 2. Core Design Philosophy: "Elastic Mobility vs. Heavy Impacts"
The game is built on a custom kinematic physics engine defined by extreme contrasts, operating entirely on a 2.5D plane (X and Y axes only):
* **Rubbery Movement:** Character mobility feels hyper-elastic. Players slide, drift, and snap around the arena. Gravity is non-linear, providing floaty "hang-time" at the peak of jumps and rapid descents.
* **Heavy Combat:** When attacks connect, the elastic feel is instantly interrupted by brutal heft. Heavy hits trigger dramatic hitstop (freeze frames), violent camera shake, and complete momentum overriding.
* **Law of Inertia:** Actions do not rigidly lock the player in place. Charging, shooting, and teleporting respect and preserve the player's current velocity vector unless acted upon by recoil or an enemy strike.
* **The 2.5D Constraint:** All logic, collisions, and movement occur strictly on the X/Y plane (Z=0). Visuals may occupy 3D space, but 360-degree aiming is calculated as a 2D Vector (X, Y) relative to the player.

---

## 3. Control Scheme (Gamepad)
The control scheme is designed for uninterrupted 360-degree combat and fluid movement on the 2.5D plane.

* **Left Stick:** Movement (Run left/right on X-axis, dictate jump trajectory, influence momentum).
* **Right Stick:** Aim Projectile (Full 360-degree independent aiming on the X/Y axes).
* **Right Trigger (RT/R2):** Shoot Projectile (Tap to shoot, Hold to charge, Release to fire).
* **Left Trigger (LT/L2):** Teleport to active projectile.
* **Action Button 1 (A/Cross):** Jump.
* **Action Button 2 (X/Square):** Normal Melee Attack (3-hit combo).

---

## 4. The Physics & Combat Engine
Realistic physics are abandoned in favor of a highly stylized, mathematically manipulated system.

### Physics Behaviors
* **Momentum Preservation:** Friction is a gradual slide, not a hard stop. If a player runs and begins charging a shot, they slide across the floor.
* **Vector Teleportation:** Teleporting updates the player's spatial coordinates instantly, but the velocity vector remains **100% untouched**. To prevent clipping into geometry, the player is spawned at `projectile.position + (Vector2.normalize(player_to_projectile) * -offset)`.
* **The Bounce Rule:** Impacts with environment surfaces trigger elastic reactions.
    * **Ground Bounces:** High-speed downward knockback into the stage surface reverses Y-velocity while retaining 80% X-axis momentum.
    * **Wall Bounces:** Horizontal impacts at high velocity reverse X-velocity with a configurable elasticity factor.

### The Attack & Recoil Tier System
Every attack, projectile, and resulting recoil falls into three strict categories:

| Tier | Name | Attacker Recoil | Victim Knockback | Visual/Game Feel |
| :--- | :--- | :--- | :--- | :--- |
| **Tier 1** | Stagger | None | None | Applies *Hitstun* only. Used for linking combos. |
| **Tier 2** | Rubber Launch | Low / Drifting | Bouncy / Medium | Launches opponent. High chance of Ground Bounces. |
| **Tier 3** | Heavy Blast | Heavy / Sliding | Linear / Lethal | Massive Hitstop. Total momentum override. Camera shake. |

---

## 5. The Dynamic Projectile System
Projectiles are the core anchor for both offense and movement. They possess a "Weight" parameter (Min to Max) that dictates their speed, damage, and the recoil applied to the shooter.

### Projectile Lifecycle & Mechanics
* **Tap Fire:** Spawns a Tier 1 projectile. Low weight, zero recoil to the player, travels quickly. Good for rapid micro-teleports.
* **Hold & Charge:** Spawns a Tier 2 or Tier 3 projectile. The longer the trigger is held, the heavier the projectile becomes.
* **Recoil Firing:** Releasing a fully charged Tier 3 projectile applies massive reverse-momentum to the player. This pushes them backward through the air or slides them violently backward on the ground, serving as an evasive tool or recovery mechanic.

### Character/Loadout Variations
* **The Brawler (DBZ Style):** Single, massive energy blast. Teleporting to it mid-flight while running applies the player's momentum to a devastating Tier 3 melee follow-up.
* **The Assassin (Minato Style):** Up to 4 light kunais on screen at once. Zero recoil. The player can chain LT/L2 presses to rapidly teleport between all active kunais, creating confusing, jagged movement paths.

---

## 6. Player State Machine
The state machine uses a **Layered Architecture** to allow for deep overlapping of actions without interrupting momentum.
* **Movement Layer:** Governs the base velocity (Running, Sliding, Airborne, Launched).
* **Action Layer:** Governs inputs (Neutral, Charging, Shooting, Melee).

### Grounded States
* **Idle:** Standard standing state.
* **Running:** Building momentum vector via Left Stick.
* **Sliding:** Left Stick neutral; momentum decaying via friction.
* **Charging/Shooting:** Upper body locks to aiming; lower body continues current momentum state (Running or Sliding).
* **Landing:** Transition from aerial to grounded; preserves horizontal momentum into a slide.

### Aerial States
* **Jumping:** Upward velocity applied; subject to non-linear gravity hang-time.
* **Falling:** Exponential downward velocity.
* **Aerial Charging/Shooting:** Mid-air aiming; gravity follows standard curves while charging, creating "falling tension" for the player. Firing heavy projectiles alters the fall trajectory via recoil.

### Teleport & Combat States
* **Teleport Relocation:** 1-frame coordinate update; momentum carries over perfectly. Can be used during `Launched` state as an emergency recovery (configurable toggle).
* **Melee Combo (1-2-3):** Forward-moving hitboxes; distance traveled scales with existing momentum. For 5 frames after a teleport, Melee inherits a "Flash Boost" distance multiplier.
* **Hitstun (Tier 1):** Momentum pauses; player locked out of inputs briefly.
* **Launched (Tier 2/3):** Momentum overwritten by attack vector; tumbling state; vulnerable to Bounces or blast zone elimination. Teleportation is permitted but difficult.


## 7. Dynamic Camera System (Cinemachine)
The camera system uses Unity's Cinemachine to create a dynamic, highly responsive 2.5D spectator view. It automatically frames the action, dictates the visual play space, and scales smoothly to accommodate the game's high-speed teleportation mechanics.

### The Target Group & Priority Weights
The camera tracks a `CinemachineTargetGroup` that dynamically updates based on active elements in the arena. Elements are weighted to prioritize players over projectiles.
* **Players (High Priority - Weight 1.0):** The camera will always ensure players are in focus. The midpoint between all living players acts as the camera's anchor.
* **Active Projectiles (Low Priority - Weight ~0.3):** Fired projectiles are added to the Target Group but carry significantly less pull. The camera will *attempt* to keep them on screen to aid teleportation tracking, but it will not sacrifice framing the players to do so.

### Dynamic Zoom & Smoothing
The camera dynamically zooms (via Z-axis movement or FOV adjustments) based on the distance between the elements in the Target Group.
* **Minimum/Maximum Zoom:** Hard limits prevent the camera from zooming in uncomfortably close during melee, or zooming out so far that the sprites become unreadable. 
* **Teleportation Dampening:** The system utilizes heavily customized X/Y and Zoom dampening values. When a player instantly teleports across the arena, the camera does not snap in 1 frame. Instead, it smoothly and rapidly catches up to the new center point to prevent motion sickness.

### Camera Bounds & The "Blast Zones"
The arena size and elimination zones are dictated by strict spatial rules, mirroring the *Super Smash Bros.* "Magnifying Glass" mechanic.
* **The Camera Confiner:** A `CinemachineConfiner` (or bounding box) dictates the absolute maximum limits the camera can pan horizontally or vertically. Even if players spread out indefinitely, the camera will stop moving at these edges.
* **The Off-Screen Danger Zone:** Players can move outside the camera's visual boundaries without instantly dying. When in this space, their character model is off-screen, and a UI bubble/magnifying glass appears at the edge of the screen showing their position.
* **The Blast Zones (Lethal Bounds):** Placed a set distance *outside* the maximum camera bounds. If a player is knocked into or moves into a Blast Zone trigger, they are instantly eliminated. 

### Game Feel & Impulses
The camera actively participates in the game's heavy hitstop combat feel.
* **Cinemachine Impulse:** Tier 3 Heavy Blasts and lethal impacts trigger predefined `CinemachineImpulseSource` profiles. This applies localized, intense screen shake that respects the 2.5D plane (shaking violently on X/Y, but remaining locked on Z) to communicate devastating force.

---

## 8. Modular Projectile System (Data-Driven Architecture)
To support a vast array of unique abilities without creating rigid, bloated code, the game utilizes a modular Strategy Pattern driven by Unity ScriptableObjects (SOs). Designers can mix and match behaviors like Lego bricks to create entirely new attacks without writing new code.

### Tier 1: The Master Data Container (`ProjectileData` SO)
Every projectile uses a master SO that acts as a blueprint. It contains the universal stats and predefined "slots" for custom behaviors.
* **Universal Core Stats:** `BaseDamage`, `BaseSpeed`, `MaxRange`, `HitstopDuration`, `RecoilTier` (1-3).
* **Stage & Targeting Limits:** 
    * `MaxActiveOnStage`: The maximum allowed instances of this projectile per player (e.g., 1 for DBZ blast, 8 for Kunais).
    * `OverflowRule`: What happens when the player tries to fire beyond the max limit? (Options: *Destroy Oldest* or *Prevent Firing*).
    * `TargetingOrder`: When pressing Teleport, how does the game pick the target? (Options: *Oldest First*, *Newest First*, *Closest to Enemy*).

### Tier 2: The Behavior Modules (Plug-and-Play Slots)
The Master Data container features five behavior slots. Designers plug specialized SOs or classes into these slots to completely change how the projectile functions.

| Behavior Slot | Purpose | Examples |
| :--- | :--- | :--- |
| **ChargeBehavior** | How holding the Shoot button alters the attack. | `ScaleAndDamage` (Grows larger), `CountCharge` (Increases number fired). |
| **SpawnBehavior** | How the projectile enters the world. | `SingleSpawn` (Fires straight), `ConeSpread` (Fires in a 45-degree fan), `DelayedSequence` (Machine gun style). |
| **FlightBehavior** | How the projectile moves through the 2.5D space. | `LinearFlight` (Straight line), `BoomerangFlight` (Reverses direction). |
| **ImpactBehavior** | What happens when it hits an enemy or wall. | `ExplosiveImpact` (AoE damage), `StickToWall` (Freezes in place). |
| **TeleportReaction** | What the projectile does when the player arrives at it. | `PersistAndShield` (Keeps flying), `ConsumeAndNode` (Instantly destroyed), `Detonate` (Explodes on arrival). |

### Example Loadouts (Designer Workflow)
By combining different modules, drastically different playstyles emerge:

* **The Heavy Brawler (DBZ Style):**
    * *Limits:* Max Active 1, Oldest First.
    * *Modules:* `ScaleAndDamage`, `SingleSpawn`, `LinearFlight`, `ExplosiveImpact`.
    * *Teleport Reaction:* **`PersistAndShield`**. (The player teleports behind the massive moving blast to use it as a battering ram or shield).

* **The Agile Assassin (Minato Style):**
    * *Limits:* Max Active 8, Oldest First, Destroy Oldest overflow.
    * *Modules:* `CountCharge` (Max 4), `ConeSpread`, `LinearFlight`, `StickToWall`.
    * *Teleport Reaction:* **`ConsumeAndNode`**. (The player chains rapid teleports through the spread of kunais, destroying each one as they arrive to cleanly link to the next).