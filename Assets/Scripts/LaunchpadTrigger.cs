using UnityEngine;

/// <summary>
/// Place this script on individual crowd member prefabs to enable individual launchpad jumping.
/// This script should be attached to each Dr. Auge clone, not the player.
/// </summary>
public class LaunchpadTrigger : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    
    private CrowdMember crowdMember;
    private bool canJump = true;
    private float jumpCooldown = 1f; // Prevent multiple jumps in quick succession
    private float lastJumpTime = 0f;
    
    void Start()
    {
        // Get the CrowdMember component on this object
        crowdMember = GetComponent<CrowdMember>();
        if (crowdMember == null)
        {
            Debug.LogError($"LaunchpadTrigger on {name}: No CrowdMember component found! This script should be on crowd member prefabs.");
        }
        
        // Verify we have a collider set as trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"LaunchpadTrigger on {name}: No Collider found! Adding one.");
            col = gameObject.AddComponent<CapsuleCollider>();
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning($"LaunchpadTrigger on {name}: Collider is not set as trigger! Setting it now.");
            col.isTrigger = true;
        }
        
        Debug.Log($"LaunchpadTrigger on {name}: Setup complete. CrowdMember: {crowdMember != null}, Trigger: {col.isTrigger}");
    }
    
    void Update()
    {
        // Reset jump cooldown
        if (!canJump && Time.time - lastJumpTime > jumpCooldown)
        {
            canJump = true;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Only respond to launchpads
        Launchpad launchpad = other.GetComponent<Launchpad>();
        if (launchpad == null) return;
        
        // Check if we can jump (cooldown and valid state)
        if (!canJump || crowdMember == null || crowdMember.IsDead) return;
        
        // Use the launchpad's force or our default
        float forceToUse = launchpad.force > 0 ? launchpad.force : jumpForce;
        
        Debug.Log($"★★★ LaunchpadTrigger: {name} hit launchpad {other.name}, jumping with force {forceToUse} ★★★");
        
        // Make this individual crowd member jump
        crowdMember.Jump(forceToUse);
        
        Debug.Log($"★★★ After jump call - Position: {transform.position}, HasOriginalPos: {crowdMember != null} ★★★");
        
        // Set cooldown
        canJump = false;
        lastJumpTime = Time.time;
    }
    
    /// <summary>
    /// Call this to enable/disable jumping for this crowd member
    /// </summary>
    public void SetJumpEnabled(bool enabled)
    {
        canJump = enabled;
    }
    
    /// <summary>
    /// Reset the jump cooldown immediately
    /// </summary>
    public void ResetJumpCooldown()
    {
        canJump = true;
        lastJumpTime = 0f;
    }
    
    /// <summary>
    /// Test method to manually trigger a jump (for debugging)
    /// </summary>
    [System.Obsolete("Debug method - remove in production")]
    public void TestJump()
    {
        if (crowdMember != null)
        {
            Debug.Log($"★★★ TEST JUMP on {name} ★★★");
            crowdMember.Jump(jumpForce);
        }
        else
        {
            Debug.LogError($"★★★ TEST JUMP FAILED on {name} - no CrowdMember! ★★★");
        }
    }
}