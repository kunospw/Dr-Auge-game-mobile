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

        if (tag == "Gap")
        {
            // If we hit a Gap, start the falling sequence.
            crowdMember.MakeFall();
        }
        else if (tag == "Obstacle" || tag == "Wall" || tag == "Water")
        {
            // If we hit any other hazard, kill the character instantly.
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