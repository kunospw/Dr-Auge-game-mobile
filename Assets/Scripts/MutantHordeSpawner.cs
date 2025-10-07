// Located in: Scripts/MutantHordeSpawner.cs

using UnityEngine;
using TMPro;
using System.Collections.Generic; // Required for the list of creatures

public class MutantHordeSpawner : MonoBehaviour
{
    [Header("Horde Settings")]
    public GameObject mutantCreaturePrefab;
    public TextMeshPro counterText;

    [Header("Horde Size")]
    public int minAmount = 5;
    public int maxAmount = 20;

    [Header("Formation")]
    public int columns = 5;
    public float spacing = 0.5f;

    private int hordeSize;
    private bool hasAttacked = false;
    private List<GameObject> spawnedCreatures = new List<GameObject>(); // Keep track of our children

    void Start()
    {
        hordeSize = Random.Range(minAmount, maxAmount + 1);

        if (counterText != null)
        {
            counterText.text = hordeSize.ToString();
        }

        if (mutantCreaturePrefab == null)
        {
            Debug.LogError("MutantHordeSpawner: Assign the Mutant Creature Prefab!");
            return;
        }

        float formationWidth = (Mathf.Min(hordeSize, columns) - 1) * spacing;
        for (int i = 0; i < hordeSize; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float xPos = (col * spacing) - (formationWidth / 2f);
            float zPos = row * spacing;

            Vector3 spawnPosition = transform.position + new Vector3(xPos, 0, zPos);

            // Spawn the creature as a child of this spawner
            GameObject newCreatureObject = Instantiate(mutantCreaturePrefab, spawnPosition, transform.rotation, transform);

            // Get the script and give it a reference back to this spawner
            MutantCreature creatureScript = newCreatureObject.GetComponent<MutantCreature>();
            if (creatureScript != null)
            {
                creatureScript.HordeSpawner = this;
            }

            // Add the new creature to our list for cleanup later
            spawnedCreatures.Add(newCreatureObject);
        }
    }

    // This method is called by a MutantCreature when it hits the player
    public void OnPlayerContact(PlayerCounter playerCounter)
    {
        // If we've already attacked, do nothing.
        if (hasAttacked)
        {
            return;
        }

        hasAttacked = true;

        if (playerCounter != null)
        {
            Debug.Log($"<color=red>MUTANT HORDE ATTACK! Dealing {hordeSize} damage.</color>");

            // Apply damage equal to the total size of the horde.
            playerCounter.HitObstacle(hordeSize);
        }

        // Destroy the entire horde and this spawner.
        Destroy(gameObject);
    }
}