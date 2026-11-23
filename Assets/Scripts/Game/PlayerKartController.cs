using GONet;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerKartController : MonoBehaviour
{
    [Header("Kart Stats")]
    public float acceleration = 30f;
    public float handling = 90f;
    public float speed = 20f;

    public enum WeightClass { Light, Medium, Heavy }
    public WeightClass weightClass = WeightClass.Medium;

    [Header("Driving State")]
    public bool canDrive = false;

    private float weightFactor;
    private float slopeFactor;
    private float handlingFactor;

    [Header("Drift & Boost")]
    public float driftMultiplier = 1.5f;
    public float boostMultiplier = 1.5f;
    public float boostDuration = 3f;

    [Header("Camera Settings")]
    public Transform playerCamera;
    public float tiltAmount = 5f;
    public float tiltSpeed = 5f;
    public float bobAmount = 0.05f;
    public float bobSpeed = 4f;
    private bool firstPerson = false;

    [Header("Camera View Switching (Left/Right Alt)")]
    //[Tooltip("Use the camera's original prefab position as first person when enabled.")]
    //public bool startInFirstPerson = true;

    [Tooltip("Position relative to the camera's parent. Adjust to frame the whole kart.")]
    public Vector3 thirdPersonLocalPosition =
        new Vector3(0f, 2.4f, -5.5f);

    [Tooltip("How far the third-person camera looks downward, in degrees.")]
    public float thirdPersonPitch = 10f;

    public float cameraPositionSpeed = 10f;

    [Header("Camera Shake")]
    public float collisionShakeIntensity = 0.05f;
    public float collisionShakeDuration = 0.25f;

    [Header("Engine & Drift Audio")]
    public AudioSource idleEngineAudio;
    public AudioSource engineAudio;
    public float engineMinPitch = 0.8f;
    public float engineMaxPitch = 2.0f;
    public float engineBoostPitch = 0.3f;

    [Header("Collision Effects")]
    public AudioSource collisionSound;
    public ParticleSystem collisionParticles;
    public float collisionSlowdownFactor = 0.6f;

    [Header("Boost Particle")]
    public ParticleSystem boostParticles;

    [Header("Stabilization Settings")]
    public float groundForce = 50f;

    [System.NonSerialized] public Rigidbody rb;

    private GONetParticipant gonetParticipant;
    private Collider col;

    private float moveInput;
    private float turnInput;

    private Vector3 camDefaultLocalPos;
    private bool isFirstPerson = false;
    private Vector3 cameraShakeOffset;

    private bool isBoosting = false;
    private bool isShaking = false;

    private float retainedSpeed = 0f;
    private float retainedSpeedDirection = 1f;

    private float reverseEngageSpeed = 0.5f;

    private RawImage HelmetOverlay;

    // ==================================
    // INITIALIZATION
    // ==================================

    void Awake()
    {
        gonetParticipant = GetComponent<GONetParticipant>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.centerOfMass = new Vector3(0, -0.6f, 0);

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        switch (weightClass)
        {
            case WeightClass.Light:
                rb.mass = 0.8f;
                weightFactor = 0.8f;
                slopeFactor = 0.8f;
                handlingFactor = 0.8f;
                break;

            case WeightClass.Medium:
                rb.mass = 1.5f;
                weightFactor = 1f;
                slopeFactor = 1f;
                handlingFactor = 1f;
                break;

            case WeightClass.Heavy:
                rb.mass = 2.5f;
                weightFactor = 1.3f;
                slopeFactor = 1.2f;
                handlingFactor = 1.3f;
                break;
        }

        // Set the starting camera perspective.
        

        if (playerCamera != null)
        {
            // Save original first-person position.
            camDefaultLocalPos = playerCamera.localPosition;

            playerCamera.localRotation = Quaternion.identity;

            // Start in third person if selected.
            if (!isFirstPerson)
            {
                playerCamera.localPosition =
                    thirdPersonLocalPosition;

                playerCamera.localRotation =
                    Quaternion.Euler(thirdPersonPitch, 0f, 0f);
            }
        }
    }

    // Only the locally controlled kart
    // should receive player input.
    private bool IsLocallyControlled =>
        gonetParticipant == null ||
        gonetParticipant.IsLocallyControlled;

    // ==================================
    // UPDATE
    // ==================================

    void Update()
    {
        if (HelmetOverlay == null)
            findHelmet();

        if (!IsLocallyControlled)
            return;

        // ALT KEY CAMERA SWITCHING
        if (Keyboard.current != null &&
            (Keyboard.current.leftAltKey.wasPressedThisFrame ||
             Keyboard.current.rightAltKey.wasPressedThisFrame))
        {
            isFirstPerson = !isFirstPerson;

              if (HelmetOverlay != null)
                HelmetOverlay.enabled = isFirstPerson;

            Debug.Log(
                "Camera switched to: " +
                (isFirstPerson
                    ? "First Person"
                    : "Third Person")
            );
        }

        

        ApplyInput();
        UpdateEngineSound();
    }

    void FixedUpdate()
    {
        if (!IsLocallyControlled)
            return;

        UpdatePosition();
        StabilizeKart();
    }

    void LateUpdate()
    {
        if (!IsLocallyControlled)
            return;

        HandleCamera();
    }

    // ==================================
    // PLAYER INPUT
    // ==================================

    void ApplyInput()
    {
        moveInput = InputManager.PlayerMoveInput.y;
        turnInput = InputManager.PlayerMoveInput.x;

        // Drifting
        if (InputManager.DriftAction.IsPressed())
            turnInput *= driftMultiplier;

        // Boosting
        if (InputManager.BoostAction.WasPressedThisFrame()
            && !isBoosting)
        {
            StartCoroutine(BoostCoroutine());
        }
    }

    // ==================================
    // ENGINE AUDIO
    // ==================================

    void UpdateEngineSound()
    {
        if (engineAudio == null || idleEngineAudio == null)
            return;

        float speedVal = rb.velocity.magnitude;
        bool isMoving = speedVal > 0.5f;

        float speedPercent =
            speedVal / (speed * weightFactor);

        engineAudio.pitch = Mathf.Lerp(
            engineMinPitch,
            engineMaxPitch,
            speedPercent
        );

        engineAudio.volume = Mathf.Lerp(
            0.2f,
            0.5f,
            speedPercent
        );

        if (isBoosting)
            engineAudio.pitch += engineBoostPitch;

        if (isMoving)
        {
            engineAudio.volume = Mathf.MoveTowards(
                engineAudio.volume,
                1f,
                Time.deltaTime * 3f
            );

            idleEngineAudio.volume = Mathf.MoveTowards(
                idleEngineAudio.volume,
                0f,
                Time.deltaTime * 3f
            );
        }
        else
        {
            idleEngineAudio.volume = Mathf.MoveTowards(
                idleEngineAudio.volume,
                0.8f,
                Time.deltaTime * 3f
            );

            engineAudio.volume = Mathf.MoveTowards(
                engineAudio.volume,
                0f,
                Time.deltaTime * 3f
            );
        }
    }

    // ==================================
    // KART MOVEMENT
    // ==================================

    void UpdatePosition()
    {
        if (!canDrive)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        float currentAcceleration =
            acceleration * (1f / weightFactor);

        if (isBoosting)
            currentAcceleration *= boostMultiplier;

        float halfHeight = col != null
            ? col.bounds.extents.y
            : 1f;

        Vector3 rayStart = col != null
            ? col.bounds.center + Vector3.up * 0.1f
            : transform.position + Vector3.up * 0.1f;

        float rayLength = halfHeight + 0.5f;

        bool grounded = Physics.Raycast(
            rayStart,
            Vector3.down,
            out RaycastHit hit,
            rayLength
        );

        if (grounded)
        {
            Quaternion slopeRotation =
                Quaternion.FromToRotation(
                    transform.up,
                    hit.normal
                ) * transform.rotation;

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    slopeRotation,
                    Time.fixedDeltaTime * 10f
                )
            );

            Vector3 slopeForward =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    hit.normal
                ).normalized;

            // FORWARD MOVEMENT
            if (moveInput > 0.1f)
            {
                rb.AddForce(
                    slopeForward *
                    currentAcceleration *
                    slopeFactor,
                    ForceMode.Acceleration
                );

                retainedSpeed = rb.velocity.magnitude;
                retainedSpeedDirection = 1f;
            }

            // BRAKING AND REVERSE
            else if (moveInput < -0.1f)
            {
                // Brake before reversing.
                if (Vector3.Dot(
                    rb.velocity,
                    transform.forward
                ) > reverseEngageSpeed)
                {
                    rb.velocity = Vector3.MoveTowards(
                        rb.velocity,
                        Vector3.zero,
                        currentAcceleration *
                        Time.fixedDeltaTime
                    );

                    retainedSpeed = rb.velocity.magnitude;
                    return;
                }

                // Reverse once slowed.
                rb.AddForce(
                    slopeForward *
                    moveInput *
                    currentAcceleration *
                    slopeFactor,
                    ForceMode.Acceleration
                );

                retainedSpeed = rb.velocity.magnitude;
                retainedSpeedDirection = -1f;
            }
            else
            {
                // COASTING
                if (rb.velocity.magnitude < retainedSpeed)
                {
                    Vector3 dir =
                        retainedSpeedDirection < 0
                            ? -transform.forward
                            : transform.forward;

                    rb.velocity = dir * retainedSpeed;
                }
            }
        }
        else
        {
            // MID-AIR FORWARD MOVEMENT
            if (moveInput > 0.1f)
            {
                rb.AddForce(
                    transform.forward *
                    currentAcceleration,
                    ForceMode.Acceleration
                );

                retainedSpeed = rb.velocity.magnitude;
                retainedSpeedDirection = 1f;
            }

            // MID-AIR BRAKING AND REVERSE
            else if (moveInput < -0.1f)
            {
                if (Vector3.Dot(
                    rb.velocity,
                    transform.forward
                ) > reverseEngageSpeed)
                {
                    rb.velocity = Vector3.MoveTowards(
                        rb.velocity,
                        Vector3.zero,
                        currentAcceleration *
                        Time.fixedDeltaTime
                    );

                    retainedSpeed = rb.velocity.magnitude;
                    return;
                }

                rb.AddForce(
                    transform.forward *
                    moveInput *
                    currentAcceleration,
                    ForceMode.Acceleration
                );

                retainedSpeed = rb.velocity.magnitude;
                retainedSpeedDirection = -1f;
            }
            else
            {
                if (rb.velocity.magnitude < retainedSpeed)
                {
                    Vector3 dir =
                        retainedSpeedDirection < 0
                            ? -transform.forward
                            : transform.forward;

                    rb.velocity = dir * retainedSpeed;
                }
            }
        }

        // MAXIMUM SPEED
        float currentMaxSpeed =
            speed * weightFactor *
            (isBoosting ? boostMultiplier : 1f);

        if (rb.velocity.magnitude > currentMaxSpeed)
        {
            rb.velocity =
                rb.velocity.normalized * currentMaxSpeed;
        }

        // TURNING
        float adjustedHandling =
            handling * handlingFactor;

        float turnAngle =
            turnInput *
            adjustedHandling *
            Time.fixedDeltaTime;

        rb.MoveRotation(
            rb.rotation *
            Quaternion.Euler(0, turnAngle, 0)
        );

        // ALIGN VELOCITY WITH KART DIRECTION
        if (rb.velocity.magnitude > 0.1f)
        {
            Vector3 dir =
                retainedSpeedDirection < 0
                    ? -transform.forward
                    : transform.forward;

            rb.velocity = dir * rb.velocity.magnitude;
        }
    }

    // ==================================
    // FIRST / THIRD PERSON CAMERA
    // ==================================

    void HandleCamera()
    {
        if (playerCamera == null)
            return;

        // Apply steering and drift tilt.
        float driftTilt =
            InputManager.DriftAction.IsPressed()
                ? 1.5f
                : 1f;

        float sideTilt =
            -turnInput * tiltAmount * driftTilt;

        // First person looks straight ahead.
        // Third person looks slightly downward.
        float pitch = isFirstPerson
            ? 0f
            : thirdPersonPitch;

        Quaternion targetRotation =
            Quaternion.Euler(
                pitch,
                0f,
                sideTilt
            );

        // Smooth camera rotation.
        playerCamera.localRotation =
            Quaternion.Slerp(
                playerCamera.localRotation,
                targetRotation,
                Mathf.Clamp01(
                    Time.deltaTime * tiltSpeed
                )
            );

        // Camera bobbing.
        float speedVal = rb.velocity.magnitude;

        float bobOffset =
            Mathf.Sin(Time.time * bobSpeed) *
            bobAmount *
            Mathf.Clamp01(
                speedVal / Mathf.Max(speed, 0.01f)
            );

        // Select camera position.
        Vector3 viewPosition = isFirstPerson
            ? camDefaultLocalPos
            : thirdPersonLocalPosition;

        Vector3 targetPosition =
            viewPosition +
            new Vector3(0f, bobOffset, 0f) +
            cameraShakeOffset;

        // Smooth transition between camera views.
        playerCamera.localPosition =
            Vector3.Lerp(
                playerCamera.localPosition,
                targetPosition,
                Mathf.Clamp01(
                    Time.deltaTime * cameraPositionSpeed
                )
            );
    }

    // ==================================
    // KART STABILIZATION
    // ==================================

    void StabilizeKart()
    {
        float halfHeight = col != null
            ? col.bounds.extents.y
            : 1f;

        Vector3 rayStart = col != null
            ? col.bounds.center
            : transform.position;

        float rayLength = halfHeight + 0.2f;

        if (!Physics.Raycast(
            rayStart,
            Vector3.down,
            rayLength))
        {
            rb.AddForce(
                Vector3.down * groundForce,
                ForceMode.Acceleration
            );
        }
    }

    // ==================================
    // BOOST SYSTEM
    // ==================================

    IEnumerator BoostCoroutine()
    {
        isBoosting = true;

        if (boostParticles != null)
            boostParticles.Play();

        yield return new WaitForSeconds(boostDuration);

        isBoosting = false;

        if (boostParticles != null)
            boostParticles.Stop();
    }

    // ==================================
    // COLLISION HANDLING
    // ==================================

    void OnCollisionEnter(Collision collision)
    {
        if (!IsLocallyControlled)
            return;

        if (collision.collider.CompareTag("Road") ||
            collision.collider.CompareTag("Terrain"))
        {
            return;
        }

        float impactForce =
            collision.relativeVelocity.magnitude;

        if (impactForce > 2f)
        {
            if (collisionSound != null)
                collisionSound.Play();

            if (collisionParticles != null)
                collisionParticles.Play();

            rb.velocity *= collisionSlowdownFactor;

            if (!isShaking && playerCamera != null)
            {
                StartCoroutine(CameraShake());
            }
        }
    }

    // ==================================
    // CAMERA SHAKE
    // ==================================

    IEnumerator CameraShake()
    {
        isShaking = true;

        float time = 0f;

        while (time < collisionShakeDuration)
        {
            cameraShakeOffset = new Vector3(
                Random.Range(
                    -collisionShakeIntensity,
                    collisionShakeIntensity
                ),
                Random.Range(
                    -collisionShakeIntensity,
                    collisionShakeIntensity
                ),
                0f
            );

            time += Time.deltaTime;
            yield return null;
        }

        cameraShakeOffset = Vector3.zero;
        isShaking = false;
    }

    private void findHelmet()
    {
        if (HelmetOverlay != null)
            return;

        GameObject helmet = GameObject.FindGameObjectWithTag("HelmetOverlay");

        HelmetOverlay = helmet.GetComponent<RawImage>();
    }
} 