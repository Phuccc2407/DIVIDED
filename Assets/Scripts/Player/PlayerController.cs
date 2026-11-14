using UnityEngine;
using UnityEngine.InputSystem;
using PurrNet;
using System.Collections.Generic;
using PurrNet.StateMachine;
using UnityEngine.XR;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : NetworkBehaviour
{
    public static bool IsLocalPlayerSpawned { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField, Min(0.1f)] private float maxStamina = 5f;
    [SerializeField, Min(0.1f)] private float sprintDrainPerSecond = 1f;
    [SerializeField, Min(0.1f)] private float staminaRecoveryPerSecond = 1.5f;
    [SerializeField, Min(0f)] private float sprintRecoveryDelay = 0.75f;
    [SerializeField] private float jumpForce = 1.2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private List<Renderer> renderers = new List<Renderer>();
    [SerializeField] private StateMachine stateMachine;
    [SerializeField] private List<StateNode> weaponStates = new List<StateNode>();
    [SerializeField]private PlayerInput playerInput;
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float animationDamping = 0.1f;
    [SerializeField, Min(0.02f)] private float animationSyncInterval = 0.1f;

    private CharacterController controller;
    [Header("Player SFX (optional)")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip[] glassFootstepClips;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField, Min(0.1f)] private float walkStepDistance = 1.8f;
    [SerializeField, Min(0.1f)] private float runStepDistance = 2.2f;
    [SerializeField, Range(0f, 1f)] private float playerSfxVolume = 0.7f;
    private AudioSource playerSfx;
    private float stepDistance, airborneTime;

    private void PlayMovementSound(int kind)
    {
        AudioClip clip = kind == 2 ? jumpSound : kind == 3 ? landingSound : null;
        AudioClip[] bank = kind == 1 && glassFootstepClips != null && glassFootstepClips.Length > 0
            ? glassFootstepClips : footstepClips;
        if (kind < 2 && bank != null && bank.Length > 0) clip = bank[Random.Range(0, bank.Length)];
        if (clip == null) return;
        if (playerSfx == null)
        {
            var emitter = new GameObject("Player SFX");
            emitter.transform.SetParent(transform, false);
            playerSfx = emitter.AddComponent<AudioSource>();
            playerSfx.playOnAwake = false;
            playerSfx.spatialBlend = 1f;
            playerSfx.minDistance = 1.5f;
            playerSfx.maxDistance = 22f;
            playerSfx.rolloffMode = AudioRolloffMode.Linear;
            playerSfx.dopplerLevel = 0f;
        }
        playerSfx.pitch = kind < 2 ? Random.Range(0.94f, 1.06f) : 1f;
        playerSfx.PlayOneShot(clip, playerSfxVolume * (SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume : 1f));
    }

    private void EmitMovementSound(int kind)
    {
        PlayMovementSound(kind);
        SendMovementSound(kind);
    }

    [ServerRpc]
    private void SendMovementSound(int kind)
    {
        if (kind >= 0 && kind <= 3) ReceiveMovementSound(kind);
    }

    [ObserversRpc(runLocally: true)]
    private void ReceiveMovementSound(int kind)
    {
        if (!isOwner) PlayMovementSound(kind);
    }
    
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
    private static readonly int VerticalSpeedParameter = Animator.StringToHash("VerticalSpeed");
    private bool animationReady;
    private float nextAnimationSync;
    private bool previousAnimationGrounded = true;
    private bool hasRemoteAnimation;
    private float remoteSpeed, remoteVerticalSpeed;
    private bool remoteGrounded;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;

    private float verticalRotation;
    private bool jumpPressed;
    private bool sprintHeld;
    private float stamina;
    private float sprintReleasedAt;
    private MainGameView mainGameView;

    public float StaminaNormalized => maxStamina <= 0f ? 0f : stamina / maxStamina;

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        var character = GetComponent<CharacterController>();
        bool wasEnabled = character != null && character.enabled;
        if (wasEnabled) character.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        velocity = Vector3.zero;
        jumpPressed = false;
        nextAnimationSync = 0f;
        stepDistance = airborneTime = 0f;
        if (wasEnabled) character.enabled = true;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            bool speed = false, grounded = false, vertical = false;
            foreach (var parameter in animator.parameters)
            {
                speed |= parameter.nameHash == SpeedParameter && parameter.type == AnimatorControllerParameterType.Float;
                grounded |= parameter.nameHash == GroundedParameter && parameter.type == AnimatorControllerParameterType.Bool;
                vertical |= parameter.nameHash == VerticalSpeedParameter && parameter.type == AnimatorControllerParameterType.Float;
            }
            animationReady = speed && grounded && vertical;
            if (!animationReady) Debug.LogWarning("Player Animator needs Speed (Float), Grounded (Bool), VerticalSpeed (Float). Assign Assets/Prefabs/Player.controller.", this);
        }

        if (!isOwner)
        {
            if (playerCamera != null)
                Destroy(playerCamera.gameObject);

            playerInput.enabled = false;
            return;
        }
        else
        {
            IsLocalPlayerSpawned = true;
            foreach (var rend in renderers)
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        playerInput.enabled = true;
        stamina = maxStamina;
        InstanceHandler.TryGetInstance(out mainGameView);
        mainGameView?.PlayerHUD?.SetStamina(StaminaNormalized);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"IsOwner: {isOwner} | IsServer: {isServer}");
    }

    private void Update()
    {
        if (!isOwner)
        {
            if (hasRemoteAnimation) ApplyAnimation(remoteSpeed, remoteGrounded, remoteVerticalSpeed);
            return;
        }
        if(playerInput == null) return;

        ReadInput();
        HandleLook();
        HandleMovement();
        HandleWeaponSwitching();

        jumpPressed = false;
    }

    private void ReadInput()
    {
        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        lookInput = playerInput.actions["Look"].ReadValue<Vector2>();

        sprintHeld = playerInput.actions["Sprint"].IsPressed();

        if (playerInput.actions["Jump"].triggered)
            jumpPressed = true;
    }

    private void HandleMovement()
    {
        if (controller == null) return;

        if (PlayerInputBlocker.IsBlocked)
        {
            stepDistance = airborneTime = 0f;
            UpdateAnimation(0f, controller.isGrounded, 0f);
            return;
        }

        bool grounded = controller.isGrounded;

        if (grounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        move = Vector3.ClampMagnitude(move, 1f);

        bool canSprint = sprintHeld && moveInput.sqrMagnitude > 0.01f && stamina > 0f;
        if (canSprint)
        {
            stamina = Mathf.Max(0f, stamina - sprintDrainPerSecond * Time.deltaTime);
            sprintReleasedAt = Time.time;
        }
        else if (Time.time >= sprintReleasedAt + sprintRecoveryDelay)
        {
            stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * Time.deltaTime);
        }

        if (mainGameView == null) InstanceHandler.TryGetInstance(out mainGameView);
        mainGameView?.PlayerHUD?.SetStamina(StaminaNormalized);
        float speed = canSprint ? sprintSpeed : moveSpeed;

        Vector3 previousPosition = transform.position;
        controller.Move(move * speed * Time.deltaTime);

        if (jumpPressed && grounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            EmitMovementSound(2);
            stepDistance = 0f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        Vector3 displacement = transform.position - previousPosition;
        displacement.y = 0f;
        if (!controller.isGrounded)
        {
            airborneTime += Time.deltaTime;
            stepDistance = 0f;
        }
        else
        {
            if (airborneTime > 0.12f) EmitMovementSound(3);
            airborneTime = 0f;
            stepDistance += displacement.magnitude;
            if (moveInput.sqrMagnitude < 0.01f) stepDistance = 0f;
            if (stepDistance >= (canSprint ? runStepDistance : walkStepDistance))
            {
                stepDistance = 0f;
                bool glass = Physics.Raycast(transform.TransformPoint(controller.center), Vector3.down,
                    out RaycastHit groundHit, controller.height * 0.5f + 0.4f, ~0, QueryTriggerInteraction.Ignore)
                    && groundHit.collider.GetComponentInParent<GlassBridgePanel>() != null;
                EmitMovementSound(glass ? 1 : 0);
            }
        }
        float horizontalSpeed = displacement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        float speed01 = horizontalSpeed <= moveSpeed
            ? 0.5f * horizontalSpeed / Mathf.Max(0.01f, moveSpeed)
            : Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(moveSpeed, sprintSpeed, horizontalSpeed));
        UpdateAnimation(Mathf.Clamp01(speed01), controller.isGrounded, velocity.y);
    }

    private void UpdateAnimation(float speed, bool grounded, float verticalSpeed)
    {
        ApplyAnimation(speed, grounded, verticalSpeed);
        if (Time.time < nextAnimationSync && grounded == previousAnimationGrounded) return;
        nextAnimationSync = Time.time + animationSyncInterval;
        previousAnimationGrounded = grounded;
        SendAnimationState(speed, grounded, verticalSpeed);
    }

    private void ApplyAnimation(float speed, bool grounded, float verticalSpeed)
    {
        if (!animationReady || animator == null) return;
        animator.SetFloat(SpeedParameter, speed, animationDamping, Time.deltaTime);
        animator.SetBool(GroundedParameter, grounded);
        animator.SetFloat(VerticalSpeedParameter, verticalSpeed);
    }

    [ServerRpc]
    private void SendAnimationState(float speed, bool grounded, float verticalSpeed)
    {
        if (float.IsNaN(speed) || float.IsInfinity(speed)
            || float.IsNaN(verticalSpeed) || float.IsInfinity(verticalSpeed)) return;
        ReceiveAnimationState(Mathf.Clamp01(speed), grounded, Mathf.Clamp(verticalSpeed, -100f, 100f));
    }

    [ObserversRpc(runLocally: true, bufferLast: true)]
    private void ReceiveAnimationState(float speed, bool grounded, float verticalSpeed)
    {
        if (isOwner) return;
        remoteSpeed = speed;
        remoteGrounded = grounded;
        remoteVerticalSpeed = verticalSpeed;
        hasRemoteAnimation = true;
    }

    private void HandleLook()
    {
        if (PlayerInputBlocker.IsBlocked)
            return;

        float sensitivity = SettingsManager.Instance != null
            ? SettingsManager.Instance.MouseSensitivity
            : lookSensitivity;
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation =
            Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleWeaponSwitching()
    {
        if (Keyboard.current == null || stateMachine == null || weaponStates.Count < 2) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            stateMachine.SetState(weaponStates[0]);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            stateMachine.SetState(weaponStates[1]);
        }
    }

    private void OnDisable()
    {
        if (!isOwner) return;

        IsLocalPlayerSpawned = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
