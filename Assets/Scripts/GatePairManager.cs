using UnityEngine;

public class GatePairManager : MonoBehaviour
{
    public Gate leftGate;
    public Gate rightGate;

    private bool isPairTriggered = false;

    void Start()
    {
        // Memberi tahu setiap gerbang siapa manager mereka
        if (leftGate != null) leftGate.SetManager(this);
        if (rightGate != null) rightGate.SetManager(this);
    }

    // Metode ini dipanggil oleh gerbang yang ditabrak pemain
    public void NotifyGateHit(Gate hitGate, PlayerCounter counter)
    {
        // Jika pasangan ini sudah pernah terpicu, jangan lakukan apa-apa
        if (isPairTriggered) return;

        isPairTriggered = true;

        // Terapkan efek dari gerbang yang ditabrak
        hitGate.ApplyEffect(counter);

        // PERUBAHAN: Daripada menghilangkan gerbang kedua, kita hanya
        // menonaktifkan collider-nya agar tidak bisa ditabrak lagi.
        Gate otherGate = (hitGate == leftGate) ? rightGate : leftGate;
        if (otherGate != null)
        {
            // Ambil komponen Collider dan matikan.
            Collider otherGateCollider = otherGate.GetComponent<Collider>();
            if (otherGateCollider != null)
            {
                otherGateCollider.enabled = false;
            }
        }
    }
}