// Located in Scripts/ObstacleDamage.cs

using UnityEngine;

public enum DamageMode { OnEnter, PerSecond }

[RequireComponent(typeof(Collider))]
public class ObstacleDamage : MonoBehaviour
{
    public DamageMode mode = DamageMode.OnEnter;
    public float dps = 5f;               // damage per second (for PerSecond)
    public float cooldown = 0.1f;        // minimal re-hit delay to prevent double-count
    
    [Header("Destruction Settings")]
    [Tooltip("Should this object be destroyed when hit?")]
    public bool destroyOnHit = false;
    [Tooltip("Delay before destroying the object (for animation/effects)")]
    public float destroyDelay = 0f;
    [Tooltip("Number of hits required to destroy (0 = destroy on first hit)")]
    public int hitsToDestroy = 0;
    
    private float lastHitTime = -999f;
    private float lastSoundTime = -999f;  // prevent sound spam
    private int currentHits = 0;

    void Start()
    {
        Debug.Log($"★★★ OBSTACLE DAMAGE INITIALIZED on {gameObject.name}, Tag: {gameObject.tag}, Mode: {mode} ★★★");
        var col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"★★★ OBSTACLE DAMAGE Collider found: isTrigger={col.isTrigger}, enabled={col.enabled} ★★★");
            if (!col.isTrigger)
            {
                Debug.LogError($"★★★ FIXING COLLIDER - Setting {gameObject.name} collider to isTrigger=true ★★★");
                col.isTrigger = true;
            }
        }
        else
        {
            Debug.LogError($"★★★ OBSTACLE DAMAGE NO COLLIDER found on {gameObject.name}! ★★★");
        }
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // we use triggers
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"★★★ OBSTACLE DAMAGE TRIGGER ENTER - Object: {other.name}, Tag: {other.tag}, Mode: {mode} ★★★");
        
        if (mode == DamageMode.OnEnter)
        {
            Debug.Log($"★★★ CALLING TryKillSingleClone for {other.name} ★★★");
            TryKillSingleClone(other);
        }
        else
        {
            Debug.Log($"★★★ MODE IS {mode}, NOT OnEnter - SKIPPING ★★★");
        }
    }

    void OnTriggerStay(Collider other)
    {
        Debug.Log($"[DEBUG] ObstacleDamage: OnTriggerStay - Object: {other.name}, Tag: {other.tag}, Mode: {mode}");
        
        if (mode == DamageMode.PerSecond)
        {
            if (Time.time - lastHitTime >= 1f / Mathf.Max(0.0001f, dps))
            {
                lastHitTime = Time.time;
                TryKillSingleClone(other);
            }
        }
    }

    private void TryKillSingleClone(Collider other)
    {
        // If a crowd member hits this, only that member dies
        var member = other.GetComponent<CrowdMember>();
        if (member != null)
        {
            Debug.Log($"DAMAGE DETECTED - Member hit {gameObject.name}, Tag: {gameObject.tag}");
            
            // Play damage sound with cooldown to prevent spam
            if (AudioManager.Instance != null && Time.time - lastSoundTime >= 0.3f)
            {
                Debug.Log("PLAYING DAMAGE SOUND NOW");
                AudioManager.Instance.PlayDamageSound();
                lastSoundTime = Time.time;
            }
            else if (AudioManager.Instance == null)
            {
                Debug.LogError("AUDIOMANAGER IS NULL!");
            }
            else
            {
                Debug.Log("DAMAGE SOUND ON COOLDOWN");
            }
            
            // Handle destruction if enabled
            if (destroyOnHit)
            {
                currentHits++;
                Debug.Log($"★★★ {gameObject.name} HIT! Hits: {currentHits}/{hitsToDestroy + 1} ★★★");
                
                if (currentHits > hitsToDestroy)
                {
                    Debug.Log($"★★★ DESTROYING {gameObject.name} after {currentHits} hits! ★★★");
                    DestroyObstacle();
                }
            }
            
            // Wall special-case: kill 5 members when hitting a wall
            if (CompareTag("Wall"))
            {
                KillMultipleMembers(5);
                if (!destroyOnHit) // Only use old behavior if destruction is not enabled
                    gameObject.SetActive(false);
            }
            else
            {
                member.KillByHazard();
            }
        }
        else
        {
            // Handle player hitting the obstacle
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null && destroyOnHit)
            {
                Debug.Log($"★★★ PLAYER HIT {gameObject.name} - DESTROYING! ★★★");
                DestroyObstacle();
            }
            else
            {
                Debug.Log($"NO CROWDMEMBER COMPONENT found on {other.name}");
            }
        }
    }

    private void KillMultipleMembers(int amount)
    {
        // Find the CrowdManager to remove multiple members
        var crowdManager = FindObjectOfType<CrowdManager>();
        if (crowdManager != null)
        {
            crowdManager.RemoveCharacters(amount);
        }
    }
    
    private void DestroyObstacle()
    {
        // Disable the collider first to prevent multiple triggers
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
            
        // Add destruction effects here if needed (particles, animation, etc.)
        
        if (destroyDelay > 0f)
        {
            // Destroy after delay
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            // Destroy immediately
            Destroy(gameObject);
        }
    }
}