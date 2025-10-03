using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 0, 180); // degrees per second

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
