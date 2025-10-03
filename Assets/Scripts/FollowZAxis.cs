using UnityEngine;

public class FollowZAxis : MonoBehaviour
{
    public Transform targetToFollow;

    void LateUpdate()
    {
        if (targetToFollow == null) return;

        // Get the current position of this object
        Vector3 newPosition = transform.position;

        // Only update the Z-axis component from the target's position
        newPosition.z = targetToFollow.position.z;

        // Apply the new position
        transform.position = newPosition;
    }
}