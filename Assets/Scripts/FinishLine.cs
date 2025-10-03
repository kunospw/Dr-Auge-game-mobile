using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [Header("Finish Settings")]
    [Tooltip("How fast to reduce the crowd count to 0 (members per second)")]
    public float reductionSpeed = 10f;
    
    // Static flag to prevent game over during win sequence
    public static bool IsWinning = false;
    
    private bool hasBeenTriggered = false;
    private PlayerCounter playerCounter;
    private CrowdManager crowdManager;
    private UIManager uiManager;
    private PlayerController playerController;
    private bool isReducing = false;

    void Reset()
    {
        // Ensure the collider is set as a trigger
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        // Reset the winning flag when a new level starts
        IsWinning = false;
        Debug.Log("FinishLine: Reset IsWinning flag to false for new level");
        
        // Find the UI Manager in the scene
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("FinishLine: No UIManager found in the scene!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered) return;

        // Check if the player hit the finish line
        if (other.CompareTag("Player"))
        {
            playerCounter = other.GetComponent<PlayerCounter>();
            crowdManager = other.GetComponent<CrowdManager>();
            playerController = other.GetComponent<PlayerController>();
            
            if (crowdManager == null)
            {
                crowdManager = FindObjectOfType<CrowdManager>();
            }
            
            if (playerCounter != null && crowdManager != null && playerController != null)
            {
                hasBeenTriggered = true;
                IsWinning = true; // Set winning flag to prevent game over
                Debug.Log("FinishLine: Player reached the finish line! Stopping player movement and spawning.");
                
                // Stop any ongoing spawning operations immediately
                crowdManager.StopSpawning();
                
                // Stop the player's movement immediately
                StopPlayerMovement();
                
                // Start reducing the crowd count to 0
                StartCoroutine(ReduceCrowdToZero());
            }
            else
            {
                Debug.LogError("FinishLine: Could not find PlayerCounter, CrowdManager, or PlayerController!");
            }
        }
    }

    private System.Collections.IEnumerator ReduceCrowdToZero()
    {
        isReducing = true;
        
        while (crowdManager.GetCrowdCount() > 0 && isReducing)
        {
            // Calculate how many to remove this frame
            int currentCount = crowdManager.GetCrowdCount();
            int toRemove = Mathf.CeilToInt(reductionSpeed * Time.deltaTime);
            toRemove = Mathf.Min(toRemove, currentCount); // Don't go below 0
            
            if (toRemove > 0)
            {
                // Remove crowd members using finish line method (no game over check)
                crowdManager.RemoveCharactersForFinishLine(toRemove);
            }
            
            yield return null; // Wait for next frame
        }
        
        // Wait a moment after all crowd is gone
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Show win panel
        if (uiManager != null)
        {
            Debug.Log("FinishLine: Showing win panel");
            uiManager.ShowWinPanel();
        }
        else
        {
            Debug.LogError("FinishLine: UIManager not found, cannot show win panel");
        }
        
        isReducing = false;
        // Keep IsWinning = true to prevent any future game over checks
    }
    
    private void StopPlayerMovement()
    {
        if (playerController != null)
        {
            // Stop the player at the finish line
            playerController.StopAtFinishLine();
            Debug.Log("FinishLine: Player movement stopped at finish line");
        }
    }
}
