// Located in: Scripts/CrowdMember.cs

using UnityEngine;

public class CrowdMember : MonoBehaviour
{
    private PlayerCounter playerCounter;
    private bool isDead = false;

    public bool IsDead => isDead;

    [Header("Individual Jump Settings")]
    public float jumpForce = 10f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayerMask = 1;

    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private float gravity = -20f;
    private Vector3 originalPosition;
    private bool hasOriginalPosition = false;
    public bool IsFalling { get; private set; } = false;

    public void Initialize(PlayerCounter counter)
    {
        playerCounter = counter;
    }

    // ===== ONTRIGGERENTER REMOVED =====
    // This entire method has been deleted. All collision and damage detection
    // is now correctly handled by the SimpleDamageHandler.cs script.
    // This fixes the "ObstacleDamage could not be found" error.

    public void MakeFall()
    {
        if (isDead) return;

        IsFalling = true;
        isDead = true;

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;

        if (playerCounter != null)
        {
            playerCounter.RemoveSpecificMember(transform);
        }

        Destroy(gameObject, 2f); // Set to 2 seconds as requested
    }

    public void KillByHazard()
    {
        if (isDead) return;
        isDead = true;

        if (playerCounter != null)
        {
            playerCounter.RemoveSpecificMember(transform);
        }

        Destroy(gameObject);
    }

    void Start()
    {
        originalPosition = transform.position;
        hasOriginalPosition = true;
    }

    void Update()
    {
        if (isDead) return;
        HandleJumpPhysics();
    }

    private void HandleJumpPhysics()
    {
        if (!hasOriginalPosition) return;

        float currentY = transform.position.y;
        float groundY = originalPosition.y;

        if (currentY <= groundY + 0.01f && verticalVelocity <= 0)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
            verticalVelocity = 0f;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            verticalVelocity += gravity * Time.deltaTime;
            transform.position += new Vector3(0, verticalVelocity * Time.deltaTime, 0);
        }
    }

    public void Jump(float force)
    {
        if (isDead || !isGrounded) return;

        verticalVelocity = force;
        isGrounded = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Jump");
        }
    }

    public void UpdateGroundReference(Vector3 newGroundPosition)
    {
        originalPosition = newGroundPosition;
        hasOriginalPosition = true;
    }

    public void ForceToGround()
    {
        if (hasOriginalPosition)
        {
            Vector3 pos = transform.position;
            pos.y = originalPosition.y;
            transform.position = pos;
            verticalVelocity = 0f;
            isGrounded = true;
        }
    }

    public bool IsCurrentlyJumping()
    {
        return !isGrounded;
    }
}