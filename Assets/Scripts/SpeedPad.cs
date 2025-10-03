using UnityEngine;

[DisallowMultipleComponent]
public class SpeedPad : MonoBehaviour
{
    [Tooltip("Kalikan base speed (mis. 1.5 = 150%)")]
    public float multiplier = 2f;

    [Tooltip("Berapa detik boost aktif")]
    public float duration = 1.5f;

    [Tooltip("Kalau true, pad hanya sekali pakai")]
    public bool oneShot = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        gameObject.tag = "Accelerate"; // opsional
    }
}
