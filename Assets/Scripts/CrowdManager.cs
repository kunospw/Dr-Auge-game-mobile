// Located in Scripts/CrowdManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrowdManager : MonoBehaviour
{
    [Header("Setup")]
    public PlayerCounter playerCounter;
    public GameObject characterPrefab;
    public Transform crowdContainer;

    [Header("Formation")]
    [Tooltip("Number of columns in the crowd formation.")]
    public int columns = 8;             // Increased for wider, more impressive crowd
    [Tooltip("Base horizontal (side-to-side) spacing between members.")]
    public float baseSpacingX = 0.15f;  // Tighter spacing for more compact look
    [Tooltip("Base vertical (front-to-back) spacing between members.")]
    public float baseSpacingZ = 0.2f;   // Tighter front-to-back spacing
    
    [Header("Crowd Deformation")]
    [Tooltip("Minimum spacing when crowd is very small (closest together).")]
    public float minSpacingX = 0.08f;   // Very tight when small
    [Tooltip("Minimum vertical spacing when crowd is very small.")]
    public float minSpacingZ = 0.12f;   // Very tight front-to-back when small
    [Tooltip("How quickly the crowd tightens when members are lost (0-1).")]
    [Range(0f, 1f)]
    public float deformationFactor = 0.7f;
    [Tooltip("Minimum number of members before deformation starts.")]
    public int deformationThreshold = 10;
    
    [Header("Dynamic Scaling")]
    [Tooltip("Base scale when crowd is small (1 member).")]
    public float baseScale = 0.2f;      // Slightly bigger base scale
    [Tooltip("Minimum scale when crowd is very large.")]
    public float minScale = 0.04f;      // Minimum scale for very large crowds
    [Tooltip("Number of members at which scaling starts to take effect.")]
    public int scalingThreshold = 5;    // Start scaling at 5 members
    [Tooltip("Maximum crowd size for scaling calculation (very large crowd).")]
    public int maxCrowdSize = 200;      // Allow much larger crowds with optimizations
    [Tooltip("How aggressively the scaling reduces size (0-1).")]
    [Range(0f, 1f)]
    public float scalingFactor = 0.8f;

    [Header("Smoothing")]
    public float followSmooth = 12f;
    public float memberSmooth = 16f;

    [Header("Performance Optimization")]
    [Tooltip("Maximum crowd size before using performance optimizations")]
    public int performanceThreshold = 50;
    [Tooltip("How many crowd members to update per frame (for large crowds)")]
    public int membersPerFrameUpdate = 10;
    [Tooltip("Distance from player beyond which crowd members are culled")]
    public float cullDistance = 20f;
    [Tooltip("Use simpler formation for very large crowds")]
    public bool useSimpleFormation = true;
    [Tooltip("Maximum crowd members to spawn per frame to prevent freezing")]
    public int maxSpawnPerFrame = 5;
    [Tooltip("Time between spawn batches in seconds")]
    public float spawnBatchDelay = 0.02f;

    private readonly List<Transform> crowdMemberTransforms = new List<Transform>();
    
    // Current dynamic spacing values
    private float currentSpacingX;
    private float currentSpacingZ;
    
    // Current dynamic scale value
    private float currentScale;
    
    // Performance optimization variables
    private int updateIndex = 0;
    private bool useOptimizations = false;
    private Transform playerTransform;
    
    // Spawn queue for smooth instantiation
    private System.Collections.Generic.Queue<int> spawnQueue = new System.Collections.Generic.Queue<int>();
    private bool isSpawning = false;
    
    // Object pooling for better performance
    private System.Collections.Generic.Queue<GameObject> crowdPool = new System.Collections.Generic.Queue<GameObject>();
    private int poolSize = 50; // Pre-create 50 objects

    public int GetCrowdCount()
    {
        // Keep list in sync with actual children to avoid stale counts
        RepairCrowdListIfNeeded();
        return crowdMemberTransforms.Count;
    }
    
    public float GetCurrentScale()
    {
        return currentScale;
    }

    private void RepairCrowdListIfNeeded()
    {
        // Remove nulls
        crowdMemberTransforms.RemoveAll(t => t == null);
        if (crowdContainer == null) return;

        // If list size differs from children, rebuild from children
        if (crowdMemberTransforms.Count != crowdContainer.childCount)
        {
            crowdMemberTransforms.Clear();
            for (int i = 0; i < crowdContainer.childCount; i++)
            {
                var child = crowdContainer.GetChild(i);
                if (child != null && child.GetComponent<CrowdMember>() != null)
                {
                    crowdMemberTransforms.Add(child);
                }
            }
        }
    }
    
    public void ForceCleanupCrowdList()
    {
        int beforeCount = crowdMemberTransforms.Count;
        RepairCrowdListIfNeeded();
        int afterCount = crowdMemberTransforms.Count;
        if (afterCount != beforeCount) OnCrowdSizeChanged();
        
        // If we have 0 members after cleanup, trigger game over check
        if (afterCount <= 0 && playerCounter != null)
        {
            // Don't trigger game over if player is winning
            if (FinishLine.IsWinning)
            {
                Debug.Log("CrowdManager: Skipping game over check - player is winning!");
            }
            else
            {
                Debug.Log($"★★★ CROWDMANAGER: Crowd is empty after cleanup - triggering game over check. IsWinning: {FinishLine.IsWinning} ★★★");
                playerCounter.CheckForGameOver();
            }
        }
    }
    
    private void CalculateDynamicSpacing()
    {
        int crowdCount = GetCrowdCount();
        
        // If we have fewer members than the threshold, start deforming
        if (crowdCount <= deformationThreshold)
        {
            // Calculate deformation ratio (0 when at threshold, 1 when at 1 member)
            float deformationRatio = 1f - ((float)crowdCount / deformationThreshold);
            deformationRatio = Mathf.Clamp01(deformationRatio);
            
            // Apply deformation factor to control how aggressive the tightening is
            deformationRatio *= deformationFactor;
            
            // Interpolate between base spacing and minimum spacing
            currentSpacingX = Mathf.Lerp(baseSpacingX, minSpacingX, deformationRatio);
            currentSpacingZ = Mathf.Lerp(baseSpacingZ, minSpacingZ, deformationRatio);
        }
        else
        {
            // Use base spacing when above threshold
            currentSpacingX = baseSpacingX;
            currentSpacingZ = baseSpacingZ;
        }
    }
    
    private void CalculateDynamicScale()
    {
        int crowdCount = GetCrowdCount();
        
        // Always scale based on crowd size, starting from 1 member
        if (crowdCount >= scalingThreshold)
        {
            // Use simple linear scaling for more predictable results
            // Scale from baseScale (at 1 member) to minScale (at maxCrowdSize)
            float scaleRatio = (float)(crowdCount - scalingThreshold) / (maxCrowdSize - scalingThreshold);
            scaleRatio = Mathf.Clamp01(scaleRatio);
            
            // Apply scaling factor to control how aggressive the scaling is
            scaleRatio *= scalingFactor;
            
            // Interpolate between base scale and minimum scale
            currentScale = Mathf.Lerp(baseScale, minScale, scaleRatio);
        }
        else
        {
            // Use base scale when below threshold
            currentScale = baseScale;
        }
    }

    void Awake()
    {
        if (!crowdContainer)
        {
            GameObject go = new GameObject("CrowdContainer");
            crowdContainer = go.transform;
            crowdContainer.SetParent(transform, worldPositionStays: false);
            crowdContainer.localPosition = Vector3.zero;
            crowdContainer.localRotation = Quaternion.identity;
        }
        
        // Initialize spacing and scale values
        currentSpacingX = baseSpacingX;
        currentSpacingZ = baseSpacingZ;
        currentScale = baseScale;
        
        // Initialize performance optimization
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Reset winning flag when CrowdManager awakens (safety reset)
        FinishLine.IsWinning = false;
        Debug.Log("CrowdManager: Reset IsWinning flag to false in Awake()");
        
        // Initialize object pool
        InitializePool();
    }
    
    private void InitializePool()
    {
        if (!characterPrefab) return;
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject pooledCharacter = Instantiate(characterPrefab);
            pooledCharacter.SetActive(false);
            
            // Set up the character components
            CrowdMember member = pooledCharacter.GetComponent<CrowdMember>();
            if (member == null)
            {
                member = pooledCharacter.AddComponent<CrowdMember>();
            }
            
            if (pooledCharacter.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
            if (pooledCharacter.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }
            
            crowdPool.Enqueue(pooledCharacter);
        }
    }

    void LateUpdate()
    {
        // Check if we need performance optimizations
        int crowdCount = GetCrowdCount();
        useOptimizations = crowdCount > performanceThreshold;
        
        // Calculate dynamic spacing and scaling based on current crowd size
        CalculateDynamicSpacing();
        CalculateDynamicScale();
        
        // Smoothly move the container to follow the main player
        crowdContainer.position = Vector3.Lerp(
            crowdContainer.position,
            transform.position,
            Time.deltaTime * followSmooth
        );
        crowdContainer.rotation = Quaternion.Slerp(
            crowdContainer.rotation,
            transform.rotation,
            Time.deltaTime * followSmooth
        );

        if (useOptimizations)
        {
            ArrangeCrowdOptimized();
        }
        else
        {
            ArrangeCrowd();
        }
        
        // Periodic repair to ensure list stays clean
        if (Time.frameCount % 60 == 0) ForceCleanupCrowdList();
    }

    public void AddCharacters(int amount)
    {
        if (!crowdContainer || !characterPrefab)
        {
            Debug.LogError("Crowd Container or Character Prefab is not assigned!");
            return;
        }

        // Don't spawn new characters if player reached finish line
        if (FinishLine.IsWinning)
        {
            Debug.Log("CrowdManager: Skipping AddCharacters - player reached finish line");
            return;
        }

        // For small amounts, spawn immediately
        if (amount <= maxSpawnPerFrame)
        {
            SpawnCharactersImmediate(amount);
        }
        else
        {
            // For large amounts, queue them for gradual spawning
            spawnQueue.Enqueue(amount);
            if (!isSpawning)
            {
                StartCoroutine(ProcessSpawnQueue());
            }
        }
    }
    
    private void SpawnCharactersImmediate(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            SpawnSingleCharacter();
        }
        
        // Trigger immediate deformation adjustment when adding characters
        OnCrowdSizeChanged();
    }
    
    private void SpawnSingleCharacter()
    {
        GameObject newCharacter = GetPooledCharacter();
        if (newCharacter == null)
        {
            // Pool exhausted, create new one
            Vector3 spawnPos = transform.position;
            newCharacter = Instantiate(characterPrefab, spawnPos, Quaternion.identity, crowdContainer);
            
            CrowdMember member = newCharacter.GetComponent<CrowdMember>();
            if (member == null)
            {
                member = newCharacter.AddComponent<CrowdMember>();
            }

            if (newCharacter.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
            if (newCharacter.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }
        }
        else
        {
            // Reuse pooled character
            newCharacter.transform.SetParent(crowdContainer);
            newCharacter.transform.position = transform.position;
            newCharacter.transform.rotation = Quaternion.identity;
            newCharacter.SetActive(true);
        }

        // Set initial scale based on current crowd size
        newCharacter.transform.localScale = Vector3.one * currentScale;
        crowdMemberTransforms.Add(newCharacter.transform);

        // Initialize the crowd member
        CrowdMember crowdMember = newCharacter.GetComponent<CrowdMember>();
        if (crowdMember != null)
        {
            crowdMember.Initialize(playerCounter);
        }
    }
    
    private GameObject GetPooledCharacter()
    {
        if (crowdPool.Count > 0)
        {
            return crowdPool.Dequeue();
        }
        return null;
    }
    
    private void ReturnToPool(GameObject character)
    {
        if (character != null)
        {
            character.SetActive(false);
            character.transform.SetParent(null);
            crowdPool.Enqueue(character);
        }
    }
    
    public void StopSpawning()
    {
        Debug.Log("CrowdManager: Stopping all spawning operations");
        spawnQueue.Clear();
        isSpawning = false;
        StopAllCoroutines(); // Stop any running spawn coroutines
    }
    
    // Special removal method for finish line that doesn't trigger game over
    public void RemoveCharactersForFinishLine(int amount)
    {
        int amountToRemove = Mathf.Min(amount, crowdMemberTransforms.Count);
        if (amountToRemove <= 0) return;

        int startIndex = crowdMemberTransforms.Count - amountToRemove;
        for (int i = crowdMemberTransforms.Count - 1; i >= startIndex; i--)
        {
            Transform memberToRemove = crowdMemberTransforms[i];
            crowdMemberTransforms.RemoveAt(i);
            if (memberToRemove != null)
            {
                // Try to return to pool instead of destroying
                ReturnToPool(memberToRemove.gameObject);
            }
        }
        
        // Clean up any null references that might have been left behind
        crowdMemberTransforms.RemoveAll(transform => transform == null);
        
        // Only update spacing/scaling, no game over check
        CalculateDynamicSpacing();
        CalculateDynamicScale();
        
        Debug.Log($"FinishLine removal: Crowd size now {GetCrowdCount()}. New spacing: X={currentSpacingX:F2}, Z={currentSpacingZ:F2}, Scale: {currentScale:F3}");
    }
    
    private System.Collections.IEnumerator ProcessSpawnQueue()
    {
        isSpawning = true;
        
        while (spawnQueue.Count > 0)
        {
            // Check if player reached finish line - stop spawning if so
            if (FinishLine.IsWinning)
            {
                Debug.Log("CrowdManager: Player reached finish line - stopping spawn queue");
                spawnQueue.Clear(); // Clear remaining spawn requests
                break;
            }
            
            int totalToSpawn = spawnQueue.Dequeue();
            int spawned = 0;
            
            Debug.Log($"CrowdManager: Starting to spawn {totalToSpawn} characters in batches of {maxSpawnPerFrame}");
            
            while (spawned < totalToSpawn)
            {
                // Check finish line state before each batch
                if (FinishLine.IsWinning)
                {
                    Debug.Log("CrowdManager: Player reached finish line during spawning - stopping immediately");
                    isSpawning = false;
                    yield break;
                }
                
                int spawnThisBatch = Mathf.Min(maxSpawnPerFrame, totalToSpawn - spawned);
                
                for (int i = 0; i < spawnThisBatch; i++)
                {
                    SpawnSingleCharacter();
                    spawned++;
                }
                
                // Update spacing and scaling after each batch
                CalculateDynamicSpacing();
                CalculateDynamicScale();
                
                // Wait before next batch to prevent frame drops
                yield return new UnityEngine.WaitForSeconds(spawnBatchDelay);
            }
            
            Debug.Log($"CrowdManager: Finished spawning {totalToSpawn} characters. Total crowd size: {GetCrowdCount()}");
        }
        
        isSpawning = false;
        
        // Final adjustment after all spawning is complete (only if not at finish line)
        if (!FinishLine.IsWinning)
        {
            OnCrowdSizeChanged();
        }
    }

    public void RemoveCharacters(int amount)
    {
        int amountToRemove = Mathf.Min(amount, crowdMemberTransforms.Count);
        if (amountToRemove <= 0) return;

        int startIndex = crowdMemberTransforms.Count - amountToRemove;
        for (int i = crowdMemberTransforms.Count - 1; i >= startIndex; i--)
        {
            Transform memberToRemove = crowdMemberTransforms[i];
            crowdMemberTransforms.RemoveAt(i);
            if (memberToRemove != null)
            {
                // Try to return to pool instead of destroying
                ReturnToPool(memberToRemove.gameObject);
            }
        }
        
        // Clean up any null references that might have been left behind
        crowdMemberTransforms.RemoveAll(transform => transform == null);
        
        // Trigger immediate deformation adjustment (but avoid game over check during finish line)
        OnCrowdSizeChanged();
    }

    public void RemoveSpecificCharacter(Transform memberTransform)
    {
        if (memberTransform == null)
        {
            // Silent return
            return;
        }

        if (crowdMemberTransforms.Contains(memberTransform))
        {
            crowdMemberTransforms.Remove(memberTransform);
            
            // Try to return to pool instead of destroying
            ReturnToPool(memberTransform.gameObject);
            
            // Trigger immediate deformation adjustment
            OnCrowdSizeChanged();
        }
        
        // Clean up any null references that might have been left behind
        crowdMemberTransforms.RemoveAll(transform => transform == null);
    }
    
    private void OnCrowdSizeChanged()
    {
        // Force immediate recalculation of spacing and scaling for smoother transitions
        CalculateDynamicSpacing();
        CalculateDynamicScale();
        
        // Optional: Add some debug logging
        Debug.Log($"Crowd size changed to {GetCrowdCount()}. New spacing: X={currentSpacingX:F2}, Z={currentSpacingZ:F2}, Scale: {currentScale:F3}");
        
        // Check for game over if crowd is empty
        if (GetCrowdCount() <= 0 && playerCounter != null)
        {
            // Don't trigger game over if player is winning
            if (FinishLine.IsWinning)
            {
                Debug.Log("CrowdManager: Skipping game over check - player is winning!");
            }
            else
            {
                Debug.Log("Crowd is empty - checking for game over");
                playerCounter.CheckForGameOver();
            }
        }
    }

    // --- CROWD FORMATION LOGIC WITH DYNAMIC DEFORMATION ---
    private void ArrangeCrowd()
    {
        int count = crowdMemberTransforms.Count;
        if (count <= 0) return;

        // Calculate the actual number of columns needed for current crowd size
        int actualColumns = Mathf.Min(columns, count);
        
        // Total width of the formation, used for centering
        float formationWidth = (actualColumns - 1) * currentSpacingX;

        for (int i = 0; i < count; i++)
        {
            // Calculate the member's position in the grid
            int row = i / actualColumns;
            int col = i % actualColumns;

            // Determine the target local position using dynamic spacing
            float xPos = (col * currentSpacingX) - (formationWidth / 2f);
            float zPos = -row * currentSpacingZ; // Position them behind the leader

            Vector3 localOffset = new Vector3(xPos, 0, zPos);

            // Convert local offset to world position based on the leader's position and rotation
            Vector3 targetPos = transform.position + (transform.rotation * localOffset);

            // Apply position, rotation, and scale with smoothing
            Transform follower = crowdMemberTransforms[i];
            if (follower != null)
            {
                follower.position = Vector3.Lerp(follower.position, targetPos, Time.deltaTime * memberSmooth);
                follower.rotation = Quaternion.Slerp(follower.rotation, transform.rotation, Time.deltaTime * memberSmooth);
                
                // Apply dynamic scaling
                Vector3 targetScale = Vector3.one * currentScale;
                follower.localScale = Vector3.Lerp(follower.localScale, targetScale, Time.deltaTime * memberSmooth);
            }
        }
    }
    
    private void ArrangeCrowdOptimized()
    {
        int count = crowdMemberTransforms.Count;
        if (count <= 0) return;

        // Use simpler formation for very large crowds
        int actualColumns = useSimpleFormation && count > performanceThreshold ? 
            Mathf.Min(columns * 2, count) : Mathf.Min(columns, count);
        
        float formationWidth = (actualColumns - 1) * currentSpacingX;
        
        // Only update a subset of crowd members per frame
        int membersToUpdate = Mathf.Min(membersPerFrameUpdate, count);
        
        for (int frame = 0; frame < membersToUpdate; frame++)
        {
            if (updateIndex >= count) updateIndex = 0;
            
            int i = updateIndex;
            Transform follower = crowdMemberTransforms[i];
            
            if (follower != null)
            {
                // Cull distant crowd members for performance
                if (playerTransform != null)
                {
                    float distanceToPlayer = Vector3.Distance(follower.position, playerTransform.position);
                    if (distanceToPlayer > cullDistance)
                    {
                        follower.gameObject.SetActive(false);
                        updateIndex++;
                        continue;
                    }
                    else
                    {
                        if (!follower.gameObject.activeInHierarchy)
                            follower.gameObject.SetActive(true);
                    }
                }
                
                // Calculate position (same as original but with potential simpler formation)
                int row = i / actualColumns;
                int col = i % actualColumns;

                float xPos = (col * currentSpacingX) - (formationWidth / 2f);
                float zPos = -row * currentSpacingZ;

                Vector3 localOffset = new Vector3(xPos, 0, zPos);
                Vector3 targetPos = transform.position + (transform.rotation * localOffset);

                // Use faster lerping for large crowds
                float optimizedSmooth = memberSmooth * 0.7f; // Slightly less smooth but faster
                
                follower.position = Vector3.Lerp(follower.position, targetPos, Time.deltaTime * optimizedSmooth);
                follower.rotation = Quaternion.Slerp(follower.rotation, transform.rotation, Time.deltaTime * optimizedSmooth);
                
                Vector3 targetScale = Vector3.one * currentScale;
                follower.localScale = Vector3.Lerp(follower.localScale, targetScale, Time.deltaTime * optimizedSmooth);
            }
            
            updateIndex++;
        }
    }
}