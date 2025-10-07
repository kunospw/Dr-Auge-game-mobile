// Located in: Scripts/DestructibleWall.cs

using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    [Tooltip("How many clones must hit this wall before it is destroyed.")]
    public int hitsRequired = 5;

    private int currentHits = 0;
    private bool isDestroyed = false; // Add a flag to prevent multiple triggers

    // This method will be called by each clone that hits the wall.
    public void TakeHit()
    {
        // If the wall is already breaking, don't count any more hits.
        if (isDestroyed)
        {
            return;
        }

        // Add one to our hit counter.
        currentHits++;

        // Check if the wall has taken enough damage.
        if (currentHits >= hitsRequired)
        {
            // ===== THIS IS THE FIX =====
            // Set the flag to true immediately.
            isDestroyed = true;

            // Disable the collider instantly so no more clones can hit it.
            GetComponent<Collider>().enabled = false;

            // Destroy the wall GameObject.
            Destroy(gameObject);
            // =========================
        }
    }
}