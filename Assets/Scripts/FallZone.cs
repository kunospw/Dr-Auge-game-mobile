using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FallZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[DAMAGE] FallZone: Something entered {gameObject.name}");
        
        // Player main body falls => instant game over via controller
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("[DAMAGE] FallZone: Player fell, playing damage sound");
            // Play damage sound when player falls
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("[DAMAGE] FallZone: AudioManager.Instance is NULL!");
            }
            player.Die();
            return;
        }

        // Individual clone falls => only this clone dies
        var member = other.GetComponent<CrowdMember>();
        if (member != null)
        {
            Debug.Log("[DAMAGE] FallZone: Crowd member fell, playing damage sound");
            // Play damage sound when crowd member falls
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("[DAMAGE] FallZone: AudioManager.Instance is NULL!");
            }
            member.KillByHazard();
        }
    }
}
