
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Collections;
using GONet;

[RequireComponent(typeof(Rigidbody))]
public class OpponentKartAI : MonoBehaviour
{
    [Header("Spline Track")]
    public SplineContainer trackSpline;

    [Header("Look-Ahead Settings")]
    [Tooltip("Base distance ahead on the spline.")]
    public float lookAheadDistance = 15f;

    [Tooltip("Extra look-ahead multiplier at full speed.")]
    public float speedLookAheadScale = 0.5f;

    [Header("AI Movement Settings")]
    public float acceleration = 20f;
    public float maxSpeed = 15f;

    [Header("Boost")]
    public float boostMultiplier = 1.5f;
    public float boostDuration = 3f;
    public float boostRandomCooldown = 9f;

    [Header("Curvature Braking")]
    public float curvatureBrakeStrength = 5f;
    public float minCurveSpeed = 6f;

    [Header("Slope Settings (match player)")]
    public float slopeFactor = 1f;
    public float groundForce = 50f;

    [Header("AI Weight")]
    public float AIWeight;

    [Header("Kart Visuals")]
    public MeshRenderer bathtubRenderer;

    [Header("Race Control")]
    public bool canDrive = false;

    [Header("Runtime Set Props")]
    public bool IsBoosting = false;
    public float RemainingBoostCooldown = 0f;

    // ==================================
    // AI KART RECOVERY SETTINGS
    // ==================================

    [Header("AI Kart Recovery")]
    [Tooltip("Seconds the kart must remain slow before recovery.")]
    public float stuckTimeLimit = 1f;

    [Tooltip("Speed below which the kart is considered stuck.")]
    public float stuckSpeedThreshold = 0.5f;

    [Tooltip("Height above the spline when repositioned.")]
    public float recoveryHeight = 1.5f;

    [Tooltip("Delay before checking for another recovery.")]
    public float recoveryCooldown = 2f;

    private float stuckTimer = 0f;
    private float recoveryCooldownTimer = 0f;

    public float ScaledMaxSpeed =>
        maxSpeed * (IsBoosting ? boostMultiplier : 1f);

    GONetParticipant participant;
    Rigidbody rb;
    float currentT;

    // ==================================
    // INITIALIZATION
    // ==================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        participant = GetComponent<GONetParticipant>();

        AIWeight = Random.Range(0.8f, 1.3f);

        RandomizeBodyColor();

        if (RaceManager.Instance != null)
        {
            SetSplineTrack(RaceManager.Instance.TrackSpline);
        }
        else
        {
            Debug.LogWarning(
                "OpponentKartAI: RaceManager not found."
            );
        }

        RemainingBoostCooldown =
            Random.Range(0f, boostRandomCooldown);

        stuckTimer = 0f;
        recoveryCooldownTimer = 0f;
    }

    void OnEnable()
    {
        Debug.Log($"Enabling AI Kart: {gameObject.name}");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.AddKartAI(this, true);
        }
    }

    void OnDisable()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.AddKartAI(this, false);
        }

        stuckTimer = 0f;
        recoveryCooldownTimer = 0f;
    }

    // ==================================
    // UPDATE
    // ==================================

    void Update()
    {
        if (!canDrive || participant == null ||
            !participant.IsMine)
        {
            return;
        }

        RemainingBoostCooldown = Mathf.Max(
            0f,
            RemainingBoostCooldown - Time.deltaTime
        );

        if (!IsBoosting &&
            RemainingBoostCooldown <= 0f)
        {
            Boost();
        }
    }

    // ==================================
    // PHYSICS UPDATE
    // ==================================

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (!canDrive)
        {
            rb.velocity = Vector3.Lerp(
                rb.velocity,
                Vector3.zero,
                Time.fixedDeltaTime
            );

            stuckTimer = 0f;
            return;
        }

        if (trackSpline != null)
        {
            HandleSplineMovement();

            // Check whether the AI kart is stuck.
            CheckIfStuck();
        }
        else
        {
            HandleSlopeMovement();

            stuckTimer = 0f;
        }
    }

    // ==================================
    // RANDOM KART COLOR
    // ==================================

    void RandomizeBodyColor()
    {
        if (bathtubRenderer != null)
        {
            Color randomColor = new Color(
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f)
            );

            bathtubRenderer.material =
                new Material(bathtubRenderer.material);

            bathtubRenderer.material.color = randomColor;
        }
    }

    // ==================================
    // SET SPLINE TRACK
    // ==================================

    public void SetSplineTrack(SplineContainer container)
    {
        trackSpline = container;
    }

    // ==================================
    // SPLINE MOVEMENT
    // ==================================

    void HandleSplineMovement()
    {
        var spline = trackSpline.Spline;

        float3 localPos =
            trackSpline.transform.InverseTransformPoint(
                transform.position
            );

        SplineUtility.GetNearestPoint(
            spline,
            localPos,
            out float3 nearestLocal,
            out float t
        );

        currentT = t;

        float splineLength = spline.GetLength();

        if (splineLength < 0.01f)
            return;

        float speedFraction =
            rb.velocity.magnitude /
            Mathf.Max(ScaledMaxSpeed, 0.01f);

        float totalLookAhead =
            lookAheadDistance +
            speedFraction *
            speedLookAheadScale *
            lookAheadDistance;

        float tOffset = totalLookAhead / splineLength;

        float lookAheadT =
            (currentT + tOffset) % 1f;

        float3 lookAheadLocal =
            spline.EvaluatePosition(lookAheadT);

        Vector3 targetPos =
            trackSpline.transform.TransformPoint(
                lookAheadLocal
            );

        // Curvature-based speed control.
        float curvature =
            spline.EvaluateCurvature(lookAheadT);

        float curveSpeedFactor =
            1f / (1f +
            curvature * curvatureBrakeStrength);

        float targetSpeed = Mathf.Lerp(
            minCurveSpeed,
            ScaledMaxSpeed,
            curveSpeedFactor
        );

        // Ground detection.
        RaycastHit hit;

        bool grounded = Physics.Raycast(
            transform.position + Vector3.up * 0.2f,
            Vector3.down,
            out hit,
            1.5f
        );

        Vector3 desiredDirection;

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
                    Time.fixedDeltaTime * 8f
                )
            );

            Vector3 slopeForward =
                Vector3.ProjectOnPlane(
                    (targetPos - transform.position).normalized,
                    hit.normal
                ).normalized;

            desiredDirection = slopeForward;
        }
        else
        {
            desiredDirection =
                (targetPos - transform.position).normalized;
        }

        if (desiredDirection.sqrMagnitude > 0.01f &&
            grounded)
        {
            Quaternion directionalRotation =
                Quaternion.LookRotation(
                    desiredDirection,
                    hit.normal
                );

            Quaternion finalRotation =
                Quaternion.Slerp(
                    rb.rotation,
                    directionalRotation,
                    Time.fixedDeltaTime * 4f
                );

            rb.MoveRotation(finalRotation);
        }

        // Acceleration.
        float currentAcceleration =
            acceleration * (1f / AIWeight);

        rb.AddForce(
            desiredDirection * currentAcceleration,
            ForceMode.Acceleration
        );

        // Speed limiting.
        if (rb.velocity.magnitude > targetSpeed)
        {
            rb.velocity =
                rb.velocity.normalized * targetSpeed;
        }
    }

    // ==================================
    // STUCK KART DETECTION
    // ==================================

    void CheckIfStuck()
    {
        if (trackSpline == null)
        {
            stuckTimer = 0f;
            return;
        }

        // Wait after recovery before checking again.
        if (recoveryCooldownTimer > 0f)
        {
            recoveryCooldownTimer -=
                Time.fixedDeltaTime;

            stuckTimer = 0f;
            return;
        }

        // Check the kart's current speed.
        float currentSpeed = rb.velocity.magnitude;

        // If the kart is moving too slowly,
        // begin counting how long it remains stuck.
        if (currentSpeed < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            // Kart is moving normally.
            stuckTimer = 0f;
        }

        // Recover after the configured time limit.
        if (stuckTimer >= stuckTimeLimit)
        {
            Debug.LogWarning(
                $"AI Kart {gameObject.name} is stuck! " +
                "Returning to track..."
            );

            RecoverKart();

            stuckTimer = 0f;

            recoveryCooldownTimer =
                recoveryCooldown;
        }
    }

    // ==================================
    // AI KART RECOVERY SYSTEM
    // ==================================

    void RecoverKart()
    {
        if (trackSpline == null || rb == null)
            return;

        var spline = trackSpline.Spline;

        if (spline.Count == 0)
        {
            Debug.LogWarning(
                "AI Kart recovery failed: spline is empty."
            );
            return;
        }

        // Get current kart position in spline space.
        float3 localPosition =
            trackSpline.transform.InverseTransformPoint(
                rb.position
            );

        // Find the closest point on the track.
        SplineUtility.GetNearestPoint(
            spline,
            localPosition,
            out float3 nearestPoint,
            out float nearestT
        );

        currentT = nearestT;

        // Convert nearest spline point to world space.
        Vector3 recoveryPosition =
            trackSpline.transform.TransformPoint(
                nearestPoint
            );

        // Lift the kart above the track.
        recoveryPosition +=
            Vector3.up * recoveryHeight;

        // Get the forward direction of the track.
        float3 splineTangent =
            spline.EvaluateTangent(nearestT);

        Vector3 trackDirection =
            trackSpline.transform.TransformVector(
                (Vector3)splineTangent
            ).normalized;

        // Fallback if tangent is too small.
        if (trackDirection.sqrMagnitude < 0.001f)
        {
            trackDirection = transform.forward;
        }

        // Keep the kart facing horizontally
        // along the track.
        trackDirection.y = 0f;

        if (trackDirection.sqrMagnitude < 0.001f)
        {
            trackDirection = Vector3.forward;
        }

        Quaternion recoveryRotation =
            Quaternion.LookRotation(
                trackDirection.normalized,
                Vector3.up
            );

        // Stop any movement before teleporting.
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reposition the kart.
        rb.position = recoveryPosition;
        rb.rotation = recoveryRotation;

        // Reset any remaining motion.
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log(
            $"AI Kart {gameObject.name} recovered " +
            "successfully and returned to the spline."
        );
    }

    // ==================================
    // BOOST SYSTEM
    // ==================================

    public void Boost()
    {
        if (IsBoosting || RemainingBoostCooldown > 0f)
            return;

        RemainingBoostCooldown = Random.Range(
            boostRandomCooldown * 0.5f,
            boostRandomCooldown
        );

        StartCoroutine(BoostCoroutine());
    }

    IEnumerator BoostCoroutine()
    {
        IsBoosting = true;

        yield return new WaitForSeconds(boostDuration);

        IsBoosting = false;
    }

    // ==================================
    // SLOPE MOVEMENT
    // ==================================

    void HandleSlopeMovement()
    {
        RaycastHit hit;

        bool grounded = Physics.Raycast(
            transform.position + Vector3.up * 0.2f,
            Vector3.down,
            out hit,
            1.5f
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
                    Time.fixedDeltaTime * 8f
                )
            );

            Vector3 slopeForward =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    hit.normal
                ).normalized;

            float slopeAngle =
                Vector3.Angle(hit.normal, Vector3.up);

            float adjustment = 1f;

            if (slopeAngle > 0.1f)
            {
                if (Vector3.Dot(
                    slopeForward,
                    Vector3.up) < 0)
                {
                    adjustment = 1f / slopeFactor;
                }
                else
                {
                    adjustment = slopeFactor;
                }
            }

            float currentAcceleration =
                acceleration * (1f / AIWeight);

            Vector3 force =
                slopeForward *
                currentAcceleration *
                adjustment;

            rb.AddForce(
                force,
                ForceMode.Acceleration
            );

            if (rb.velocity.magnitude > ScaledMaxSpeed)
            {
                rb.velocity =
                    rb.velocity.normalized * ScaledMaxSpeed;
            }
        }
        else
        {
            rb.AddForce(
                Vector3.down * groundForce * 0.5f,
                ForceMode.Acceleration
            );
        }
    }
}
