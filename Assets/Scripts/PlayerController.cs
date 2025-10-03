using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float baseForwardSpeed = 8f;   // kecepatan dasar
    public float laneOffset = 2.5f;
    public float laneLerpSpeed = 12f;

    [Header("UI")]
    public UIManager uiManager; // Reference to UI Manager

    [Header("Boost")]
    public float accelRate = 20f;         // seberapa cepat naik
    public float decelRate = 10f;         // seberapa cepat turun
    private float currentSpeed;           // dipakai untuk move.z
    private float targetSpeed;            // tujuan (base / boosted)
    private float boostTimer;             // sisa durasi boost

    [Header("Jump")]
    public float gravity = 20f;

    private int currentLane = 0;
    private float verticalVelocity;
    private bool alive = true;
    private bool finishLineReached = false;

    [Header("Touch Controls")]
    [Tooltip("Sensitivity for touch drag movement (higher = more sensitive)")]
    public float touchSensitivity = 0.01f;
    [Tooltip("Speed for snapping back to lanes")]
    public float laneSnapSpeed = 8f;
    
    // Touch input variables
    private Vector2 initialTouchPosition;
    private float targetLanePosition = 0f;
    private bool isDragging = false;

    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        currentSpeed = baseForwardSpeed;
        targetSpeed = baseForwardSpeed;
        
        // Reset winning flag when PlayerController awakens (safety reset)
        FinishLine.IsWinning = false;
        Debug.Log("PlayerController: Reset IsWinning flag to false in Awake()");
    }

    void Update()
    {
        if (!alive || finishLineReached) return;

        HandleInput();

        // ===== speed smoothing =====
        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f) targetSpeed = baseForwardSpeed; // habis, balik normal
        }
        float rate = (currentSpeed < targetSpeed) ? accelRate : decelRate;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

        // ===== lane steer =====
        float targetX;
        
        if (isDragging)
        {
            // Use continuous drag position while touching
            targetX = targetLanePosition;
        }
        else
        {
            // Use discrete lane position when not dragging
            targetX = currentLane * laneOffset;
        }
        
        Vector3 move = Vector3.zero;
        float lerpSpeed = isDragging ? laneLerpSpeed * 1.5f : laneLerpSpeed; // Faster response during drag
        move.x = (targetX - transform.position.x) * lerpSpeed;

        // forward auto-run
        move.z = currentSpeed;

        // gravity
        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        else verticalVelocity -= gravity * Time.deltaTime;
        move.y = verticalVelocity;

        cc.Move(move * Time.deltaTime);
    }

    void HandleInput()
    {
        HandleTouchInput();
        HandleMouseInput(); // Mouse simulation for editor testing
        HandleKeyboardInput(); // Keep keyboard for testing in editor
    }
    
    void HandleTouchInput()
    {
        // Handle mobile touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                // Store the initial touch position when touch begins
                initialTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                // Calculate the horizontal drag distance
                float deltaX = touch.position.x - initialTouchPosition.x;
                
                // Convert screen space to world space movement
                float targetX = deltaX * touchSensitivity;
                
                // Clamp the target position to lane boundaries
                targetX = Mathf.Clamp(targetX, -laneOffset, laneOffset);
                
                // Set the target lane position directly based on touch drag
                targetLanePosition = targetX;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                // Snap to nearest lane when touch ends
                SnapToNearestLane();
                isDragging = false;
            }
        }
    }
    
    void HandleMouseInput()
    {
        // Simulate touch with mouse for Unity Editor testing
        if (Input.GetMouseButtonDown(0))
        {
            initialTouchPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePos = Input.mousePosition;
            float deltaX = currentMousePos.x - initialTouchPosition.x;
            
            float targetX = deltaX * touchSensitivity;
            targetX = Mathf.Clamp(targetX, -laneOffset, laneOffset);
            
            targetLanePosition = targetX;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            SnapToNearestLane();
            isDragging = false;
        }
    }
    
    void HandleKeyboardInput()
    {
        // Keep keyboard input for testing in Unity Editor (only if not dragging)
        if (!isDragging)
        {
            if (Input.GetKeyDown(KeyCode.A))
                currentLane = Mathf.Clamp(currentLane - 1, -1, 1);
            if (Input.GetKeyDown(KeyCode.D))
                currentLane = Mathf.Clamp(currentLane + 1, -1, 1);
        }
    }
    
    void SnapToNearestLane()
    {
        // Determine which lane is closest to current position
        float currentX = targetLanePosition;
        
        if (currentX < -laneOffset * 0.5f)
            currentLane = -1; // Left lane
        else if (currentX > laneOffset * 0.5f)
            currentLane = 1;  // Right lane
        else
            currentLane = 0;  // Center lane
            
        Debug.Log($"Snapped to lane: {currentLane}");
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Obstacle"))
        {
            Debug.Log($"[DAMAGE] PlayerController: Hit obstacle {hit.collider.name}, playing damage sound");
            // Play damage sound when player hits obstacle
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("[DAMAGE] PlayerController: AudioManager.Instance is NULL!");
            }
            Die();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!alive) return;

        // Launchpad lompat (kalau ada)
        var pad = other.GetComponent<Launchpad>();
        if (pad)
        {
            verticalVelocity = pad.force;
            // Play jump sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("Jump");
            }
        }

        // Speed pad (ACCELERATE)
        var sp = other.GetComponent<SpeedPad>();
        if (sp)
        {
            targetSpeed = baseForwardSpeed * sp.multiplier; // mis. 2x
            boostTimer = Mathf.Max(boostTimer, sp.duration);

            if (sp.oneShot) other.gameObject.SetActive(false);
        }
    }

    public bool IsAlive()
    {
        return alive;
    }

    public void Die()
    {
        if (!alive) return;

        alive = false;
        currentSpeed = 0f;
        targetSpeed = 0f;
        Debug.Log("Game Over!");
        
        // Try to find and call UI Manager
        if (uiManager != null)
        {
            Debug.Log("PlayerController: Calling UIManager.ShowGameOver()");
            uiManager.ShowGameOver();
        }
        else
        {
            // Try to find UIManager in the scene if not assigned
            UIManager foundUIManager = FindObjectOfType<UIManager>();
            if (foundUIManager != null)
            {
                Debug.Log("PlayerController: Found UIManager in scene, calling ShowGameOver()");
                foundUIManager.ShowGameOver();
            }
            else
            {
                Debug.LogError("PlayerController: No UIManager found! Falling back to time stop.");
                Time.timeScale = 0f;
            }
        }
    }
    
    public void StopAtFinishLine()
    {
        Debug.Log("PlayerController: Stopping at finish line - disabling movement and input");
        
        // Set the finish line flag to disable all movement and input
        finishLineReached = true;
        
        // Set both current and target speed to 0 to stop forward movement
        currentSpeed = 0f;
        targetSpeed = 0f;
        boostTimer = 0f; // Clear any boost effects
        
        Debug.Log("PlayerController: Player stopped at finish line successfully");
    }
}
