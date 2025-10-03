// Located in Scripts/MutantHorde.cs

using UnityEngine;
using TMPro; // Needed for the text counter
using System.Collections.Generic;

public class MutantHorde : MonoBehaviour
{
    [Header("Horde Settings")]
    [Tooltip("Minimum number of creatures in the horde.")]
    public int minAmount = 1;
    [Tooltip("Maximum number of creatures in the horde.")]
    public int maxAmount = 20;

    [Header("Visuals")]
    [Tooltip("The prefab for a single creature model.")]
    public GameObject creaturePrefab;
    [Tooltip("The 3D TextMeshPro object used for the counter.")]
    public TextMeshPro counterText;
    [Tooltip("An empty child object to hold the spawned creatures.")]
    public Transform hordeContainer;

    [Header("Formation")]
    public int columns = 5;
    public float spacingX = 0.4f;
    public float spacingZ = 0.4f;

    private int hordeAmount;
    private bool hasBeenTriggered = false;
    private bool creaturesSpawned = false;

    void Start()
    {
        // Only initialize the horde amount and text, don't spawn creatures yet
        InitializeHordeData();
    }

    void Update()
    {
        // Spawn creatures when player gets close (but only once)
        if (!creaturesSpawned && !hasBeenTriggered)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                if (distanceToPlayer < 15f) // Spawn when player is within 15 units
                {
                    SpawnCreatures();
                    creaturesSpawned = true;
                }
            }
        }
    }

    void InitializeHordeData()
    {
        // 1. Generate the random amount for the horde
        hordeAmount = Random.Range(minAmount, maxAmount + 1);

        // 2. Update the counter text to show how much damage it will do
        if (counterText != null)
        {
            counterText.text = $"-{hordeAmount}";
        }
    }

    void SpawnCreatures()
    {
        if (creaturePrefab == null || hordeContainer == null)
        {
            Debug.LogError("Assign the Creature Prefab and Horde Container in the Inspector!");
            return;
        }

        // Clear any existing creatures first
        foreach (Transform child in hordeContainer)
        {
            if (child != null && child != counterText?.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // 3. Spawn and arrange the creatures in a grid relative to the horde container
        float formationWidth = (columns - 1) * spacingX;
        for (int i = 0; i < hordeAmount; i++)
        {
            int row = i / columns;
            int col = i % columns;
            float xPos = (col * spacingX) - (formationWidth / 2f);
            float zPos = -row * spacingZ;
            Vector3 localSpawnPosition = new Vector3(xPos, 0, zPos);

            // Spawn relative to the horde container with proper local positioning
            GameObject creature = Instantiate(creaturePrefab, hordeContainer);
            creature.transform.localPosition = localSpawnPosition;
            creature.transform.localRotation = Quaternion.identity;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered || !other.CompareTag("Player")) return;

        PlayerCounter playerCounter = other.GetComponent<PlayerCounter>();
        if (playerCounter != null)
        {
            hasBeenTriggered = true;

            // 4. Damage the player by the horde's amount
            Debug.Log($"★★★ MUTANT HORDE HIT - Player hit a mutant horde of {hordeAmount} creatures! ★★★");
            
            // Play damage sound for mutant horde hit
            if (AudioManager.Instance != null)
            {
                Debug.Log("★★★ PLAYING DAMAGE SOUND FROM MUTANT HORDE ★★★");
                AudioManager.Instance.PlayDamageSound();
            }
            else
            {
                Debug.LogError("★★★ AUDIOMANAGER IS NULL IN MUTANT HORDE! ★★★");
            }
            
            playerCounter.HitObstacle(hordeAmount);

            // 5. Destroy this entire obstacle
            Destroy(gameObject);
        }
    }
}