// Located in: Scripts/MutantCreature.cs

using UnityEngine;

public class MutantCreature : MonoBehaviour
{
    // This variable will hold a reference to the spawner that created this creature.
    public MutantHordeSpawner HordeSpawner { get; set; }

    private bool hasHit = false;

    // The Update() method is removed, so they remain stationary.

    void OnTriggerEnter(Collider other)
    {
        if (hasHit || !other.CompareTag("Player"))
        {
            return;
        }

        // We have hit the player.
        hasHit = true;

        // Tell our spawner that the horde has made contact.
        if (HordeSpawner != null)
        {
            HordeSpawner.OnPlayerContact(other.GetComponent<PlayerCounter>());
        }

        // The creature will be destroyed by the spawner, not by itself.
    }
}