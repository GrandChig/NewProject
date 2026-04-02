using UnityEngine;

public class CustomPhysicsController : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float gravityBase = -9.81f;
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float hangTimeThreshold = 1f;
    [SerializeField] private float hangTimeMultiplier = 0.5f;

    [Header("Friction & Drag")]
    [SerializeField] private float groundFriction = 10f;
    [SerializeField] private float airDrag = 2f;

    [Header("Bounce Settings")]
    [SerializeField] private float bounceVelocityThreshold = 10f;
    [SerializeField] private float groundBounceElasticityY = 0.8f;
    [SerializeField] private float groundBounceMomentumRetainX = 0.8f;
    [SerializeField] private float wallBounceElasticityX = 0.8f;
    [SerializeField] private float wallBounceMomentumRetainY = 0.8f;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private Vector2 colliderSize = new Vector2(1f, 1f);
    [SerializeField] private float skinWidth = 0.02f;

    public Vector2 CurrentVelocity { get; private set; }
    public bool IsGrounded { get; private set; }

    public void AddVelocity(Vector2 velocity)
    {
        CurrentVelocity += velocity;
    }

    public void SetVelocity(Vector2 velocity)
    {
        CurrentVelocity = velocity;
    }

    public void ApplyFriction(float customFrictionAmount)
    {
        float signX = Mathf.Sign(CurrentVelocity.x);
        float speedX = Mathf.Abs(CurrentVelocity.x) - customFrictionAmount * Time.deltaTime;
        speedX = Mathf.Max(speedX, 0);
        CurrentVelocity = new Vector2(speedX * signX, CurrentVelocity.y);
    }

    private void Update()
    {
        HandleGravity();
        HandleFriction();
        MoveAndCollide();
        EnforceZLock();
    }

    private void HandleGravity()
    {
        if (IsGrounded && CurrentVelocity.y <= 0)
        {
            CurrentVelocity = new Vector2(CurrentVelocity.x, 0);
            return;
        }

        float gravityPull = gravityBase * gravityMultiplier;
        
        // Hang time logic (reduced gravity near peak of jump)
        if (Mathf.Abs(CurrentVelocity.y) < hangTimeThreshold)
        {
            gravityPull *= hangTimeMultiplier;
        }

        CurrentVelocity += new Vector2(0, gravityPull * Time.deltaTime);
    }

    private void HandleFriction()
    {
        float frictionToApply = IsGrounded ? groundFriction : airDrag;
        ApplyFriction(frictionToApply);
    }

    private void MoveAndCollide()
    {
        Vector2 displacement = CurrentVelocity * Time.deltaTime;
        IsGrounded = false;

        // X Axis Movement and Collision
        if (displacement.x != 0)
        {
            Vector2 dirX = new Vector2(displacement.x, 0).normalized;
            RaycastHit2D hitX = Physics2D.BoxCast(transform.position, colliderSize, 0f, dirX, Mathf.Abs(displacement.x) + skinWidth, environmentLayer);
            
            if (hitX)
            {
                displacement.x = (hitX.distance - skinWidth) * dirX.x;

                // High speed bounce check
                if (Mathf.Abs(CurrentVelocity.x) >= bounceVelocityThreshold)
                {
                    CurrentVelocity = new Vector2(-CurrentVelocity.x * wallBounceElasticityX, CurrentVelocity.y * wallBounceMomentumRetainY);
                }
                else
                {
                    CurrentVelocity = new Vector2(0, CurrentVelocity.y);
                }
            }
            transform.position += new Vector3(displacement.x, 0, 0);
        }

        // Y Axis Movement and Collision
        if (displacement.y != 0)
        {
            Vector2 dirY = new Vector2(0, displacement.y).normalized;
            RaycastHit2D hitY = Physics2D.BoxCast(transform.position, colliderSize, 0f, dirY, Mathf.Abs(displacement.y) + skinWidth, environmentLayer);
            
            if (hitY)
            {
                displacement.y = (hitY.distance - skinWidth) * dirY.y;

                if (hitY.normal.y > 0.5f)
                {
                    IsGrounded = true;
                }

                // High speed bounce check
                if (Mathf.Abs(CurrentVelocity.y) >= bounceVelocityThreshold)
                {
                    CurrentVelocity = new Vector2(CurrentVelocity.x * groundBounceMomentumRetainX, -CurrentVelocity.y * groundBounceElasticityY);
                }
                else
                {
                    CurrentVelocity = new Vector2(CurrentVelocity.x, 0);
                }
            }
            transform.position += new Vector3(0, displacement.y, 0);
        }
    }

    private void EnforceZLock()
    {
        Vector3 pos = transform.position;
        if (pos.z != 0)
        {
            pos.z = 0;
            transform.position = pos;
        }
    }
}
