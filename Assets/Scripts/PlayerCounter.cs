// Located in Scripts/PlayerCounter.cs

using UnityEngine;
using TMPro;

public class PlayerCounter : MonoBehaviour
{
    public CrowdManager crowdManager;
    public TextMeshProUGUI counterText;
    private PlayerController playerController;

    private int totalDamageTaken = 0; // ✅ Track total deaths

    void Start()
    {
        // Reset winning flag when PlayerCounter starts
        FinishLine.IsWinning = false;
        Debug.Log("PlayerCounter: Reset IsWinning flag to false in Start()");
        
        crowdManager = GetComponent<CrowdManager>();
        playerController = GetComponent<PlayerController>();

        if (GetCurrentCount() == 0)
        {
            crowdManager.AddCharacters(1);
        }
        RefreshUI();
    }

    public void CheckForGameOver()
    {
        // Don't trigger game over if player is winning
        if (FinishLine.IsWinning)
        {
            Debug.Log("PlayerCounter: Skipping game over check - player is winning!");
            return;
        }
        
        int currentCount = GetCurrentCount();
        Debug.Log($"★★★ GAME OVER CHECK - Current Count: {currentCount}, Player Alive: {(playerController != null ? playerController.IsAlive() : false)}, IsWinning: {FinishLine.IsWinning} ★★★");
        
        if (currentCount <= 0 && playerController != null && playerController.IsAlive())
        {
            Debug.Log("★★★ TRIGGERING GAME OVER - Count is 0 and player is alive! ★★★");
            playerController.Die();
        }
    }

    public void RemoveSpecificMember(Transform memberTransform)
    {
        Debug.Log("A crowd member was hit and removed.");
        if (crowdManager != null)
        {
            crowdManager.RemoveSpecificCharacter(memberTransform);
        }

        totalDamageTaken++; // ✅ Increment here
        Debug.Log($"Total Damage Taken: {totalDamageTaken}");

        RefreshUI();
        CheckForGameOver();
    }

    public void ApplyDelta(int add)
    {
        int countBefore = GetCurrentCount();
        
        if (add > 0) 
        {
            crowdManager.AddCharacters(add);
            Debug.Log($"Added {add} characters. Count before: {countBefore}, after: {GetCurrentCount()}");
        }
        else if (add < 0)
        {
            crowdManager.RemoveCharacters(Mathf.Abs(add));
            totalDamageTaken += Mathf.Abs(add); // ✅ Track bulk damage from horde/gate
            Debug.Log($"Removed {Mathf.Abs(add)} characters. Count before: {countBefore}, after: {GetCurrentCount()}");
        }

        RefreshUI();
        CheckForGameOver();
    }

    public void ApplyMultiply(float mul)
    {
        int currentCount = GetCurrentCount();
        int newCount = Mathf.RoundToInt(currentCount * mul);
        int difference = newCount - currentCount;

        Debug.Log($"Characters multiplied! From {currentCount} to {newCount}.");

        ApplyDelta(difference);
        
        // Debug: Verify the count after multiplication
        Debug.Log($"After multiplication, actual count is: {GetCurrentCount()}");
    }

    public void HitObstacle(int damage)
    {
        Debug.Log($"★★★ PLAYER HIT OBSTACLE - Damage: {damage}. Playing damage sound ★★★");
        
        // Play damage sound when hitting obstacles
        if (AudioManager.Instance != null)
        {
            Debug.Log("★★★ PLAYING DAMAGE SOUND FROM PLAYER COUNTER HIT OBSTACLE ★★★");
            AudioManager.Instance.PlayDamageSound();
        }
        else
        {
            Debug.LogError("★★★ AUDIOMANAGER IS NULL IN PLAYER COUNTER! ★★★");
        }
        
        ApplyDelta(-damage);
        Debug.Log($"Player hit obstacle. Damage: {damage}. New count: {GetCurrentCount()}");
    }

    private int GetCurrentCount()
    {
        if (crowdManager == null) return 0;
        int count = crowdManager.GetCrowdCount();
        // Only log every 10th call to reduce spam
        if (Time.frameCount % 10 == 0)
        {
            Debug.Log($"GetCurrentCount returning: {count}");
        }
        return count;
    }

    void RefreshUI()
    {
        int currentCount = GetCurrentCount();
        if (counterText)
        {
            counterText.text = currentCount.ToString();
            Debug.Log($"UI Updated - Counter text set to: {currentCount}");
        }
        else
        {
            Debug.LogWarning("CounterText is not assigned! UI will not update.");
        }
    }
    
    void Update()
    {
        // Continuously sync UI with authoritative count and detect zero immediately
        RefreshUI();
        if (GetCurrentCount() <= 0)
        {
            CheckForGameOver();
        }
    }
    
    public void ForceRefresh()
    {
        if (crowdManager != null)
        {
            crowdManager.ForceCleanupCrowdList();
        }
        RefreshUI();
        Debug.Log($"Force refresh - Current count: {GetCurrentCount()}");
        
        // Force check for game over after cleanup
        CheckForGameOver();
    }
    
    [ContextMenu("Force Game Over")]
    public void ForceGameOver()
    {
        if (playerController != null && playerController.IsAlive())
        {
            playerController.Die();
        }
    }
    
    [ContextMenu("Set Count to 0")]
    public void SetCountToZero()
    {
        if (crowdManager != null)
        {
            int currentCount = GetCurrentCount();
            if (currentCount > 0)
            {
                crowdManager.RemoveCharacters(currentCount);
            }
        }
        RefreshUI();
        CheckForGameOver();
    }
}
