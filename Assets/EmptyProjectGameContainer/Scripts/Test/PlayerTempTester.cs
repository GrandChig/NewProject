using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CustomPhysicsController))]
public class PlayerTempTester : MonoBehaviour
{
    private CustomPhysicsController physics;

    [Header("Testing Controls")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float jumpBurst = 20f;
    [SerializeField] private float dashBurst = 40f;

    private void Start()
    {
        physics = GetComponent<CustomPhysicsController>();
    }

    private void Update()
    {
        // 1. Horizontal Movement (Keyboard & Gamepad)
        float inputX = 0f;
        
        // Read Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;
        }

        // Read Gamepad (Overrides keyboard if moved)
        if (Gamepad.current != null)
        {
            float stickX = Gamepad.current.leftStick.x.ReadValue();
            float dpadX = Gamepad.current.dpad.x.ReadValue();
            
            if (Mathf.Abs(stickX) > 0.1f) inputX = Mathf.Sign(stickX);
            else if (Mathf.Abs(dpadX) > 0.1f) inputX = Mathf.Sign(dpadX);
        }

        if (inputX != 0)
        {
            physics.AddVelocity(new Vector2(inputX * acceleration * Time.deltaTime, 0));
        }

        // 2. Jump (Spacebar or Gamepad South Button - 'A' on Xbox, 'Cross' on PS)
        bool jumpPressed = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;

        if (jumpPressed && physics.IsGrounded)
        {
            physics.SetVelocity(new Vector2(physics.CurrentVelocity.x, jumpBurst));
        }

        // 3. Projectile System Handles Shooting (Right Trigger / Left Shift)
        // PlayerProjectileManager handles this internally now.
        
        // 4. Projectile System Handles Teleporting (Left Trigger / F Key)
        // PlayerProjectileManager handles this internally now.
    }
}
