// Located in: Scripts/SimpleDamageHandler.cs

using UnityEngine;

[RequireComponent(typeof(Collider), typeof(CrowdMember))]
public class SimpleDamageHandler : MonoBehaviour
{
    private CrowdMember crowdMember;

    void Start()
    {
        // Get the CrowdMember script on this same character object.
        crowdMember = GetComponent<CrowdMember>();
    }

    void OnTriggerEnter(Collider other)
    {
        // If the character is already dead or the game is won, do nothing.
        if (crowdMember.IsDead || FinishLine.IsWinning)
        {
            return;
        }

        // Check the TAG of the object we hit.
        string tag = other.tag;

        if (tag == "Gap" || tag == "Water")
        {
            // If we hit a Gap or Water, start the falling sequence (same behavior)
            Debug.Log($"★★★★★ {name} hit {tag} - FALLING WITH PHYSICS ★★★★★");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            crowdMember.MakeFall();
        }
        else if (tag == "Obstacle" || tag == "Wall")
        {
            // If we hit obstacles or walls, kill the character instantly.
            Debug.Log($"★★★★★ {name} hit {tag} - INSTANT KILL ★★★★★");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDamageSound();
            }
            crowdMember.KillByHazard();

            // ===== THIS IS THE NEW LOGIC FOR THE WALL =====
            // If the object we hit was a wall, we tell it that it took a hit.
            if (tag == "Wall")
            {
                // Try to get the DestructibleWall script from the wall object.
                DestructibleWall wall = other.GetComponent<DestructibleWall>();
                if (wall != null)
                {
                    // Tell the wall to record the hit.
                    wall.TakeHit();
                }
            }
            // ===============================================
        }
    }
}