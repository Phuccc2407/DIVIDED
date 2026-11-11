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

    private CharacterController controller;
    
    private Animator animator;

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

    protected override void OnSpawned()
    {
        base.OnSpawned();

        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

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
        mainGameView?.PlayerHUD.SetStamina(StaminaNormalized);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"IsOwner: {isOwner} | IsServer: {isServer}");
    }

    private void Update()
    {
        if (!isOwner) return;
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
            return;

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

        mainGameView?.PlayerHUD.SetStamina(StaminaNormalized);
        float speed = canSprint ? sprintSpeed : moveSpeed;

        controller.Move(move * speed * Time.deltaTime);

        // animator.SetFloat("Forward", moveInput.y);
        // animator.SetFloat("Sideways", moveInput.x);

        if (jumpPressed && grounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
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