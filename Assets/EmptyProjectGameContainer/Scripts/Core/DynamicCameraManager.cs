using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

namespace ProjectileDash.Core
{
    /// <summary>
    /// Serves as the central manager for gameplay cameras, dynamically prioritizing players
    /// and appropriately framing action and projectiles while respecting level boundaries.
    /// Adapted for Cinemachine 3.x structure.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class DynamicCameraManager : MonoBehaviour
    {
        public static DynamicCameraManager Instance { get; private set; }

        [Header("Cinemachine References")]
        [Tooltip("The main 2.5D virtual camera.")]
        [SerializeField] private CinemachineCamera vcam;
        
        [Tooltip("The target group that aggregates all players and visible objects.")]
        [SerializeField] private CinemachineTargetGroup targetGroup;
        
        [Tooltip("Confiner to keep the camera within the blast zones/stage bounds.")]
        [SerializeField] private Behaviour confiner; // Using Behaviour to accept CinemachineConfiner2D or 3D
        
        [Tooltip("Source for generating screen shake on heavy hits.")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Weight Configuration")]
        [Tooltip("Weight applied to player characters to prioritize their framing.")]
        [SerializeField] private float playerWeight = 1.0f;
        [Tooltip("Radius around the player to keep framed.")]
        [SerializeField] private float playerRadius = 2.0f;
        [Tooltip("Weight applied to projectiles so the camera favors players but tries to keep projectiles visible.")]
        [SerializeField] private float projectileWeight = 0.3f;
        [Tooltip("Radius around the projectile to keep framed.")]
        [SerializeField] private float projectileRadius = 0.5f;

        [Header("Zoom & Smoothing Thresholds")]
        [Tooltip("The minimum distance/orthographic size the camera can zoom in.")]
        [SerializeField] private float minZoom = 5f;
        [Tooltip("The maximum distance/orthographic size the camera can zoom out.")]
        [SerializeField] private float maxZoom = 15f;
        
        [Tooltip("Rigorous damping on X axis to rapidly and smoothly catch up to teleports.")]
        [SerializeField, Range(0f, 20f)] private float xDamping = 1.5f;
        [Tooltip("Rigorous damping on Y axis to rapidly and smoothly catch up to teleports.")]
        [SerializeField, Range(0f, 20f)] private float yDamping = 1.5f;

        [Header("Game Feel")]
        [Tooltip("Intensity of the violent screen shake for Tier 3 hitstops.")]
        [SerializeField] private float heavyHitShakeIntensity = 2f;

        // In Cinemachine 3 Target Group Cameras natively use Follow and Group Framing 
        private CinemachineFollow followComponent;
        private CinemachineGroupFraming groupFraming;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (vcam == null) vcam = GetComponent<CinemachineCamera>();
            
            if (vcam != null)
            {
                // Target Group Cameras in CM3 use CinemachineFollow on the Body stage
                followComponent = vcam.GetComponent<CinemachineFollow>();
                groupFraming = vcam.GetComponent<CinemachineGroupFraming>();
            }
            
            if (followComponent == null || groupFraming == null)
            {
                Debug.LogWarning("DynamicCameraManager: Missing CinemachineFollow or CinemachineGroupFraming on the Camera GameObject. Adjusting dynamically will not work.");
            }
            
            ApplyCameraSettings();
        }

        private void OnValidate()
        {
            if (vcam != null)
            {
                followComponent = vcam.GetComponent<CinemachineFollow>();
                groupFraming = vcam.GetComponent<CinemachineGroupFraming>();
                ApplyCameraSettings();
            }
        }

        /// <summary>
        /// Applies the Zoom and Damping settings exposed in our Inspector 
        /// to the underlying Cinemachine 3 components.
        /// </summary>
        private void ApplyCameraSettings()
        {
            if (vcam != null)
            {
                // In a Smash Bros 2.5D style game, the camera must never dynamically rotate to 'aim' at targets.
                // It must exclusively slide along the X/Y plane and only zoom.
                if (vcam.GetComponent<CinemachineRotationComposer>() != null)
                {
                    Debug.LogError("DynamicCameraManager: A CinemachineRotationComposer was found on this camera! " +
                                   "This will cause the camera to tilt/angle itself (like a Top-Down or 3D game). " +
                                   "Please remove the CinemachineRotationComposer component from the Camera GameObject to enforce strict 2D/2.5D rules.");
                }
            }

            if (followComponent != null)
            {
                // Ensure the camera rigorously catches up on X/Y axes through the TrackerSettings
                followComponent.TrackerSettings.PositionDamping = new Vector3(xDamping, yDamping, followComponent.TrackerSettings.PositionDamping.z);
                
                // Force World Space binding so the camera doesn't accidentally inherit rotations or try to orbit the target group
                followComponent.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            }

            if (groupFraming != null)
            {
                // Use Group Framing to handle dynamic zoom based on target separation
                groupFraming.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
                
                // Constrain the zooming so it never gets too close (sprite overlap) or too far (loss of detail)
                groupFraming.OrthoSizeRange = new Vector2(minZoom, maxZoom);
                
                // If using perspective, constrain the dolly distance
                groupFraming.DollyRange = new Vector2(-maxZoom, maxZoom); 
            }
        }

        #region Target Group Management

        public void AddPlayer(Transform playerTransform)
        {
            if (targetGroup != null)
                targetGroup.AddMember(playerTransform, playerWeight, playerRadius);
        }

        public void RemovePlayer(Transform playerTransform)
        {
            if (targetGroup != null)
                targetGroup.RemoveMember(playerTransform);
        }

        public void RegisterProjectile(Transform projectileTransform)
        {
            if (targetGroup != null)
                targetGroup.AddMember(projectileTransform, projectileWeight, projectileRadius);
        }

        public void UnregisterProjectile(Transform projectileTransform)
        {
            if (targetGroup != null)
                targetGroup.RemoveMember(projectileTransform);
        }

        /// <summary>
        /// For smooth catching up after an instant instant teleport or respawn.
        /// Forces Cinemachine to reset its position history so it doesn't drag across the map.
        /// </summary>
        public void TeleportCameraToTargets()
        {
            if (vcam != null)
            {
                vcam.PreviousStateIsValid = false;
            }
        }

        #endregion

        #region Game Feel & Screen Shake

        /// <summary>
        /// Triggers a violent X/Y localized shake, meant for Tier 3 hitstop impacts.
        /// Called directly by the Combat Engineer's damage execution scripts.
        /// </summary>
        public void TriggerHeavyHitstopShake()
        {
            if (impulseSource != null)
            {
                // Create a violent randomized directional shake strictly on X/Y to enforce 2.5D adherence
                Vector3 shakeVelocity = new Vector3(
                    Random.Range(-1f, 1f), 
                    Random.Range(-1f, 1f), 
                    0f
                ).normalized * heavyHitShakeIntensity;

                impulseSource.GenerateImpulse(shakeVelocity);
            }
        }

        #endregion

        #region Extensibility: Off-Screen Detection

        /// <summary>
        /// Conceptual check to determine if a target is outside the camera's current viewing frustum.
        /// Intended for the UI Manager to use when drawing "Magnifying Glass" bubbles for players knocked into blast zones.
        /// </summary>
        /// <param name="worldPos">World position of the player/object.</param>
        /// <param name="padding">Tolerance buffer. E.g., 0.05 means 5% off-screen before returning true.</param>
        /// <returns>True if strictly outside the clamped bounds.</returns>
        public bool IsTargetOffScreen(Vector3 worldPos, float padding = 0f)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return false;

            // Convert World to Viewport. Viewport bounds are 0 to 1 for X and Y.
            Vector3 viewportPos = mainCam.WorldToViewportPoint(worldPos);

            bool outOfBoundsX = viewportPos.x < (0f - padding) || viewportPos.x > (1f + padding);
            bool outOfBoundsY = viewportPos.y < (0f - padding) || viewportPos.y > (1f + padding);
            
            // Check if behind the camera (strict fallback for Z depth issues)
            bool behindCamera = viewportPos.z < 0f;

            return outOfBoundsX || outOfBoundsY || behindCamera;
        }

        #endregion
    }
}
