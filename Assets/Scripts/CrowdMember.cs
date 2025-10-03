using UnityEngine;

public class CrowdMember : MonoBehaviour
{
    private PlayerCounter playerCounter;
    private bool isDead = false;

    public void Initialize(PlayerCounter counter)
    {
        playerCounter = counter;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"CROWDMEMBER HIT SOMETHING - Object: {other.name}, Tag: {other.tag}, isDead: {isDead}");
        
        if (isDead) return;
        
        // If it hits an obstacle hazard (handled by ObstacleDamage), play sound immediately and let it handle
        if (other.GetComponent<ObstacleDamage>() != null)
        {
            Debug.Log($"FOUND OBSTACLEDAMAGE ON {other.name} - PLAYING DAMAGE SOUND AND letting it handle");
            
            // Play damage sound immediately since we found an obstacle
            if (AudioManager.Instance != null)
            {
                Debug.Log("★★★ PLAYING DAMAGE SOUND FROM CROWDMEMBER OBSTACLEDAMAGE DETECTION ★★★");
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("★★★ AUDIOMANAGER IS NULL IN CROWDMEMBER OBSTACLEDAMAGE! ★★★");
            }
            return;
        }

        // Hard hazards by tag
        if (other.CompareTag("Water") || other.CompareTag("Wall"))
        {
            Debug.Log($"CROWDMEMBER HIT WATER/WALL - {other.tag} ({other.name}), PLAYING DAMAGE SOUND");
            // Play damage sound for water and wall hits
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("AUDIOMANAGER IS NULL IN CROWDMEMBER!");
            }
            KillByHazard();
            if (other.CompareTag("Wall")) other.gameObject.SetActive(false);
        }
        else if (other.CompareTag("Gap"))
        {
            Debug.Log($"CROWDMEMBER HIT GAP - ({other.name}), PLAYING DAMAGE SOUND");
            // Play damage sound for falling into gaps
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("AUDIOMANAGER IS NULL IN CROWDMEMBER!");
            }
            // Make the crowd member fall instead of instant death
            MakeFall();
        }
        else
        {
            Debug.Log($"CROWDMEMBER HIT SOMETHING ELSE - Tag: {other.tag}, Name: {other.name}");
        }
    }

    private void MakeFall()
    {
        if (isDead) return;
        
        // Enable physics so the member falls
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Remove from crowd formation immediately
        if (playerCounter != null)
        {
            playerCounter.RemoveSpecificMember(transform);
        }
        
        // Destroy after falling for a bit (to clean up)
        Destroy(gameObject, 3f);
        isDead = true;
    }

    public void KillByHazard()
    {
        if (isDead) return;

        isDead = true;

        // Remove from counter BEFORE destroying the object
        if (playerCounter != null)
        {
            playerCounter.RemoveSpecificMember(transform);
        }

        // Use Destroy instead of DestroyImmediate to avoid physics trigger errors
        Destroy(gameObject);
    }
}
