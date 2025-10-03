// Located in Scripts/Gate.cs

using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Required for List

public enum GateOp { Add, Subtract, Multiply, Divide } // SetValue removed

public class Gate : MonoBehaviour
{
    public GateOp operation = GateOp.Add;
    public float value = 5f;

    [Header("UI di pintu")]
    public TextMeshPro text3D;
    public string prefix = "";
    public string suffix = "";

    [Header("One-shot")]
    public bool consumeOnHit = true;
    private bool consumed = false;

    [Header("Visuals")]
    public float fadeDuration = 0.5f;

    private GatePairManager pairManager;

    void Start()
    {
        // Define a list of allowed operations.
        List<GateOp> allowedOperations = new List<GateOp>
        {
            GateOp.Add,
            GateOp.Multiply,
        };

        // Randomly select one operation from the allowed list.
        operation = allowedOperations[Random.Range(0, allowedOperations.Count)];

        // Assign values with auto-proofing consideration
        AssignGateValue();
        
        UpdateGateLabel();
    }

    private void AssignGateValue()
    {
        // Get current crowd count for auto-proofing
        int currentCrowdCount = GetCurrentCrowdCount();
        
        if (operation == GateOp.Multiply)
        {
            // Auto-proofing: limit multiplication values when crowd > 100
            if (currentCrowdCount > 100)
            {
                // For large crowds, limit multiplication to 2-3
                value = Random.Range(2, 4); // 2 or 3
                Debug.Log($"Gate: Auto-proofing activated! Crowd count: {currentCrowdCount}, limited multiply value to: {value}");
            }
            else
            {
                // For smaller crowds, allow normal range 2-6
                value = Random.Range(2, 7);
            }
        }
        else // This applies to Add
        {
            // Addition values remain the same regardless of crowd size
            value = Random.Range(1, 11);
        }
    }

    private int GetCurrentCrowdCount()
    {
        // Try to find CrowdManager in the scene to get current count
        CrowdManager crowdManager = FindObjectOfType<CrowdManager>();
        if (crowdManager != null)
        {
            return crowdManager.GetCrowdCount();
        }
        
        // Fallback: try to find PlayerCounter
        PlayerCounter playerCounter = FindObjectOfType<PlayerCounter>();
        if (playerCounter != null)
        {
            // Since GetCurrentCount is private, we'll use CrowdManager as primary method
            return 0; // Default to 0 if we can't access it
        }
        
        return 0; // Default value if no components found
    }

    void UpdateGateLabel()
    {
        if (!text3D) return;
        string label = operation switch
        {
            GateOp.Add => $"+{Mathf.RoundToInt(value)}",
            GateOp.Multiply => $"x{value}",
            // Kept other cases for safety, though they shouldn't be selected.
            GateOp.Subtract => $"-{Mathf.RoundToInt(value)}",
            GateOp.Divide => $"�{value}",
            _ => value.ToString()
        };
        text3D.text = $"{prefix}{label}{suffix}";
    }

    public void SetManager(GatePairManager manager)
    {
        pairManager = manager;
    }

    // Method to refresh gate value based on current crowd count (for dynamic adjustment)
    public void RefreshGateValue()
    {
        if (!consumed) // Only refresh if gate hasn't been used
        {
            AssignGateValue();
            UpdateGateLabel();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed || !other.CompareTag("Player")) return;

        var counter = other.GetComponent<PlayerCounter>();
        if (!counter) return;

        if (pairManager != null)
        {
            pairManager.NotifyGateHit(this, counter);
        }
        else
        {
            ApplyEffect(counter);
        }

        if (consumeOnHit)
        {
            Consume();
        }
    }

    public void ApplyEffect(PlayerCounter c)
    {
        Debug.Log($"Gate passed! Operation: {operation}, Value: {value}");

        // Play multiply door sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Multiply Door");
        }

        switch (operation)
        {
            case GateOp.Add:
                c.ApplyDelta(Mathf.RoundToInt(value));
                break;
            case GateOp.Multiply:
                c.ApplyMultiply(value);
                break;
            // The cases below are no longer used but are kept to avoid errors
            // if you ever decide to re-enable them.
            case GateOp.Subtract:
                c.ApplyDelta(-Mathf.RoundToInt(value));
                break;
            case GateOp.Divide:
                if (Mathf.Approximately(value, 0f)) return;
                c.ApplyMultiply(1f / value);
                break;
        }
    }

    public void Consume()
    {
        if (consumed) return;
        consumed = true;
        GetComponent<Collider>().enabled = false;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        if (text3D == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        Color originalTextColor = text3D.color;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(1.0f - (elapsedTime / fadeDuration));
            text3D.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void DeactivateImmediately()
    {
        consumed = true;
        gameObject.SetActive(false);
    }
}