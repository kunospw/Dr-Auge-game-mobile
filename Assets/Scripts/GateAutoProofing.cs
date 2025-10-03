using UnityEngine;

public class GateAutoProofing : MonoBehaviour
{
    [Header("Auto-Proofing Settings")]
    [Tooltip("How often to check and update gate values (seconds)")]
    public float updateInterval = 2f;
    
    [Tooltip("Minimum change in crowd count to trigger gate updates")]
    public int crowdChangeThreshold = 20;
    
    private int lastCrowdCount = 0;
    private float lastUpdateTime = 0f;

    void Start()
    {
        // Initialize with current crowd count
        CrowdManager crowdManager = FindObjectOfType<CrowdManager>();
        if (crowdManager != null)
        {
            lastCrowdCount = crowdManager.GetCrowdCount();
        }
        lastUpdateTime = Time.time;
    }

    void Update()
    {
        // Check if it's time to update
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            CheckAndUpdateGates();
            lastUpdateTime = Time.time;
        }
    }

    private void CheckAndUpdateGates()
    {
        // Get current crowd count
        CrowdManager crowdManager = FindObjectOfType<CrowdManager>();
        if (crowdManager == null) return;
        
        int currentCrowdCount = crowdManager.GetCrowdCount();
        int crowdChange = Mathf.Abs(currentCrowdCount - lastCrowdCount);
        
        // Only update if there's a significant change in crowd size
        if (crowdChange >= crowdChangeThreshold)
        {
            Debug.Log($"GateAutoProofing: Crowd count changed from {lastCrowdCount} to {currentCrowdCount}. Updating gates...");
            
            // Update all unused gates in the scene
            UpdateAllGates();
            
            lastCrowdCount = currentCrowdCount;
        }
    }

    private void UpdateAllGates()
    {
        // Find all gates in the scene
        Gate[] allGates = FindObjectsOfType<Gate>();
        
        foreach (Gate gate in allGates)
        {
            if (gate != null)
            {
                // Refresh the gate value based on current crowd count
                gate.RefreshGateValue();
            }
        }
        
        Debug.Log($"GateAutoProofing: Updated {allGates.Length} gates");
    }

    // Method to manually trigger gate updates (can be called from other scripts)
    public void ForceUpdateGates()
    {
        UpdateAllGates();
    }
}
