// Located in: Scripts/CrowdMember.cs

using UnityEngine;

public class CrowdMember : MonoBehaviour
{
    private PlayerCounter playerCounter;
    private bool isDead = false;

    public bool IsDead => isDead;

    [Header("Individual Jump Settings")]
    public float jumpForce = 12f; // Adjusted for a better feel with new gravity
    private float gravity = -15f;   // A strong gravity for responsive jumps

    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    public bool IsFalling { get; private set; } = false;

    // A reference to the main player transform for a stable ground check.
    private Transform groundReference;

    public void Initialize(PlayerCounter counter)
    {
        playerCounter = counter;
    }

    void Start()
    {
        // Get the stable ground reference from the parent of our parent (the main player object).
        if (transform.parent != null && transform.parent.parent != null)
        {
            groundReference = transform.parent.parent;
        }
    }

    public void MakeFall()
    {
        if (isDead) return;
        IsFalling = true;
        isDead = true;

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        if (playerCounter != null)
        {
            playerCounter.RemoveSpecificMember(transform);
        }
        Destroy(gameObject, 1f);
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

    void Update()
    {
        if (isDead) return;
        HandleJumpPhysics();
    }

    // ===== THIS IS THE FINAL, SMOOTH PHYSICS HANDLER =====
    private void HandleJumpPhysics()
    {
        // If we are grounded, let the CrowdManager handle our position.
        if (isGrounded)
        {
            return;
        }

        // Apply gravity to our upward speed.
        verticalVelocity += gravity * Time.deltaTime;

        // Move the character up or down.
        transform.position += new Vector3(0, verticalVelocity * Time.deltaTime, 0);

        // Check for landing against our stable ground reference.
        if (groundReference != null && transform.position.y <= groundReference.position.y && verticalVelocity < 0)
        {
            // Snap to the exact ground position.
            Vector3 finalPos = transform.position;
            finalPos.y = groundReference.position.y;
            transform.position = finalPos;

            // Reset our state to "grounded."
            isGrounded = true;
            verticalVelocity = 0;
        }
    }
    // =======================================================

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

    public bool IsCurrentlyJumping()
    {
        return !isGrounded;
    }
}