using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    // Panjang chunk (sepanjang sumbu Z)
    public float Length => Vector3.Distance(startPoint.position, endPoint.position);
}
