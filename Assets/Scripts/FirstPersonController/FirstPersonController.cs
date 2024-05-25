using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FirstPersonController : MonoBehaviour
{
    #region Variables
    //-----------------------------------------------------------------------------------------------------

    public bool CanMove { get; private set; } = true;

    public bool IsSprinting
    {
        get { return canSprint && Input.GetKey(sprintKey); }
        private set { }
    }

    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAnimation && characterController.isGrounded;


    # region Functional Options
    //-----------------------------------------------------------------------------------------------------

    [Header("Functional Options")]
    [SerializeField] public bool canAim = true;
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadbob = true;
    [SerializeField] private bool WillSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool useFootsteps = true;
    [SerializeField] private bool useStamina = true;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Controls
    //-----------------------------------------------------------------------------------------------------

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode zoomKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Movement Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Movement Params")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8f;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Look Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Look Params")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 100)] private float upperLookLimit = 80f;
    [SerializeField, Range(1, 100)] private float lowerLookLimit = 80f;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Jumping Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Jumping Params")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30.0f;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Crouch Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Crouch Params")]
    [SerializeField] private float crouchHeigth = 0.5f;
    [SerializeField] private float standingHeigth = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    public bool isCrouching { get; private set; }
    private bool duringCrouchAnimation;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Headbob Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Headbob Params")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Zooming Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Zooming Params")]
    [SerializeField] private float timeToZoom = 0.2f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFOV;
    private Coroutine zoomRoutine;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Interaction Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Interaction Params")]
    [SerializeField] private Vector3 interactionRayPoint;
    [SerializeField] private float interactionRayDistance;
    private Interactable currentInteractable;
    [SerializeField] private LayerMask interactionLayer;
    public Image crossair; // playerUI script ? 

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Footstep Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Footstep Params")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultipler = 1.5f;
    [SerializeField] private float sprintStepMultipler = 0.6f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] woodClips;
    [SerializeField] private AudioClip[] metalClips;
    [SerializeField] private AudioClip[] grassClips;
    private float footstepTimer = 0;
    private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepMultipler : IsSprinting ? baseStepSpeed * sprintStepMultipler : baseStepSpeed;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Healing System Parameters
    //-----------------------------------------------------------------------------------------------------

    [Header("Healing System Parameters")]
    public float maxHealth = 100;
    [SerializeField] private float timeBeforeRegenStart = 3;
    [SerializeField] private float healthValueIncrement = 1;
    [SerializeField] private float healthTimeIncrement = 0.1f;
    public float currentHealth;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Stamina Params
    //-----------------------------------------------------------------------------------------------------

    [Header("Stamina Params")]
    [SerializeField] private float maxStamina = 100;
    [SerializeField] private float staminaUseMultiplier = 5;
    [SerializeField] private float timeBeforeStaminaRegenStart = 5;
    [SerializeField] private float staminaValueIncrement = 1;
    [SerializeField] private float staminaTimeIncrement = 0.1f;
    private float currentStamina;
    private Coroutine regeneratingStamina;
    public static Action<float> OnStaminaChange;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Sliding Params
    //-----------------------------------------------------------------------------------------------------

    private Vector3 hitPointNormal;
    private bool IsSliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
                return false;
        }
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Character Controller
    //-----------------------------------------------------------------------------------------------------

    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Camera
    //-----------------------------------------------------------------------------------------------------

    private Camera camera;

    //-----------------------------------------------------------------------------------------------------
    #endregion

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Unity Event Functions
    //-----------------------------------------------------------------------------------------------------

    private void Awake()
    {
        camera = Camera.main;

        characterController = GetComponent<CharacterController>();

        defaultYPos = camera.transform.localPosition.y;
        defaultFOV = camera.fieldOfView;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    //-----------------------------------------------------------------------------------------------------

    private void Update()
    {
        if (CanMove)
        {
            HandleMovementInputs();

            if (canAim)
                HandleMouseLook();

            if (canJump)
                HandleJump();

            if (canCrouch)
                HandleCrouch();

            if (canUseHeadbob)
                HandleHeadbob();

            if (canZoom)
                HandleZoom();

            if (useFootsteps)
                HandleFootSteps();

            if (canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
            }

            if (useStamina)
                HandleStamina();

            ApplyFinalMovements();
        }
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region HandleMovementInputs
    //-----------------------------------------------------------------------------------------------------

    private void HandleMovementInputs()
    {
        currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right * currentInput.y));
        moveDirection.y = moveDirectionY;
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle MouseLook
    //-----------------------------------------------------------------------------------------------------

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        camera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Jump
    //-----------------------------------------------------------------------------------------------------

    private void HandleJump()
    {
        if (ShouldJump)
            moveDirection.y = jumpForce;
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Crouch
    //-----------------------------------------------------------------------------------------------------

    private void HandleCrouch()
    {
        if (ShouldCrouch)
            StartCoroutine(CrouchStand());
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Headbob
    //-----------------------------------------------------------------------------------------------------

    private void HandleHeadbob()
    {
        if (!characterController.isGrounded) return;

        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);

            camera.transform.localPosition = new Vector3
            (
            camera.transform.localPosition.x,
            defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
            camera.transform.localPosition.z
            );
        }
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region HandleZoom
    //-----------------------------------------------------------------------------------------------------

    private void HandleZoom()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }

        if (Input.GetKeyUp(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Interaction Check
    //-----------------------------------------------------------------------------------------------------

    private void HandleInteractionCheck()
    {
        Debug.DrawRay(camera.ViewportPointToRay(interactionRayPoint).origin, camera.ViewportPointToRay(interactionRayPoint).direction, Color.red);
        if (Physics.Raycast(camera.ViewportPointToRay(interactionRayPoint), out RaycastHit hitInfo, interactionRayDistance))
        {
            if (((1 << hitInfo.collider.gameObject.layer) & interactionLayer) != 0 && (currentInteractable == null || hitInfo.collider.gameObject.GetInstanceID() != currentInteractable.GetInstanceID()))
            {
                hitInfo.collider.TryGetComponent(out currentInteractable);

                if (currentInteractable)
                {
                    currentInteractable.OnFocus();
                }
            }
        }
        else if (currentInteractable)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Interaction Input
    //-----------------------------------------------------------------------------------------------------

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey)
            && currentInteractable != null
            && Physics.Raycast(camera.ViewportPointToRay(interactionRayPoint), out RaycastHit hitInfo, interactionRayDistance, interactionLayer))
            currentInteractable.OnInteract();
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Foot Steps
    //-----------------------------------------------------------------------------------------------------

    private void HandleFootSteps()
    {
        if (!characterController.isGrounded) return;
        if (moveDirection == Vector3.zero) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(camera.transform.position, Vector3.down, out RaycastHit hitInfo, 3f))
            {
                // no audio clips right now.
                /*switch (hitInfo.collider.tag)
                {
                    case "":
                        break;

                }*/
            }
            footstepTimer = GetCurrentOffset;
        }
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Apply Heal 
    //-----------------------------------------------------------------------------------------------------

    public void ApplyHeal(float healValue)
    {
        currentHealth += healValue;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Handle Stamina
    //-----------------------------------------------------------------------------------------------------

    private void HandleStamina()
    {
        if (IsSprinting && currentInput != Vector2.zero) // kosuyorsak (x,z hareket halindeysek)
        {
            if (regeneratingStamina != null)
            {
                StopCoroutine(regeneratingStamina);
                regeneratingStamina = null;
            }

            OnStaminaChange?.Invoke(currentStamina);

            if (currentStamina < 0)
                currentStamina = 0;

            currentStamina -= staminaUseMultiplier * Time.deltaTime;

            if (currentStamina <= 0)
                canSprint = false;
        }


        if (!IsSprinting && currentStamina < maxStamina && regeneratingStamina == null)
            regeneratingStamina = StartCoroutine(RegeneratingStamina());
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region Apply Final Movements
    //-----------------------------------------------------------------------------------------------------

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (WillSlideOnSlopes && IsSliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region IE Crouch Stand
    //-----------------------------------------------------------------------------------------------------

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(camera.transform.position, Vector3.up, 1f)) yield break;

        duringCrouchAnimation = true;

        float timeElapsed = 0;

        float targetHeight = isCrouching ? standingHeigth : crouchHeigth;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        duringCrouchAnimation = false;
        isCrouching = !isCrouching;

    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region IE Togg leZoom
    //-----------------------------------------------------------------------------------------------------

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startingFOV = camera.fieldOfView;
        float timeElapsed = 0;

        while (timeElapsed < timeToZoom)
        {
            camera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        camera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region IE Regenerating Health
    //-----------------------------------------------------------------------------------------------------

    private IEnumerator RegeneratingHealth()
    {
        yield return new WaitForSeconds(timeBeforeRegenStart);

        WaitForSeconds timeToWait = new WaitForSeconds(healthTimeIncrement);

        while (currentHealth < maxHealth)
        {
            currentHealth += healthValueIncrement;

            if (currentHealth >= maxHealth) currentHealth = maxHealth;

            OnHeal?.Invoke(currentHealth);
            yield return timeToWait;
        }

        regeneratingHealth = null; // after everythings is completed
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion

    #region IE Regenerating Stamina
    //-----------------------------------------------------------------------------------------------------

    private IEnumerator RegeneratingStamina()
    {
        yield return new WaitForSeconds(timeBeforeStaminaRegenStart);

        WaitForSeconds timeToWait = new WaitForSeconds(staminaTimeIncrement);

        while (currentStamina < maxStamina)
        {
            currentStamina += staminaValueIncrement;

            if (currentStamina > 0)
                canSprint = true;

            if (currentStamina > maxStamina)
                currentStamina = maxStamina;

            OnStaminaChange?.Invoke(currentStamina);

            yield return timeToWait;
        }

        regeneratingStamina = null;
    }

    //-----------------------------------------------------------------------------------------------------
    #endregion
}