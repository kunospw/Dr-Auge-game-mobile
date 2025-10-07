// Located in: Scripts/CrowdManager.cs

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
    public int columns = 8;
    [Tooltip("Base horizontal (side-to-side) spacing between members.")]
    public float baseSpacingX = 0.15f;
    [Tooltip("Base vertical (front-to-back) spacing between members.")]
    public float baseSpacingZ = 0.2f;
    
    [Header("Crowd Deformation")]
    [Tooltip("Minimum spacing when crowd is very small (closest together).")]
    public float minSpacingX = 0.08f;
    [Tooltip("Minimum vertical spacing when crowd is very small.")]
    public float minSpacingZ = 0.12f;
    [Tooltip("How quickly the crowd tightens when members are lost (0-1).")]
    [Range(0f, 1f)]
    public float deformationFactor = 0.7f;
    [Tooltip("Minimum number of members before deformation starts.")]
    public int deformationThreshold = 10;
    
    [Header("Dynamic Scaling")]
    [Tooltip("Base scale when crowd is small (1 member).")]
    public float baseScale = 0.2f;
    [Tooltip("Minimum scale when crowd is very large.")]
    public float minScale = 0.04f;
    [Tooltip("Number of members at which scaling starts to take effect.")]
    public int scalingThreshold = 5;
    [Tooltip("Maximum crowd size for scaling calculation (very large crowd).")]
    public int maxCrowdSize = 200;
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
    
    private float currentSpacingX;
    private float currentSpacingZ;
    private float currentScale;
    
    private int updateIndex = 0;
    private bool useOptimizations = false;
    private Transform playerTransform;
    
    private System.Collections.Generic.Queue<int> spawnQueue = new System.Collections.Generic.Queue<int>();
    private bool isSpawning = false;
    
    private System.Collections.Generic.Queue<GameObject> crowdPool = new System.Collections.Generic.Queue<GameObject>();
    private int poolSize = 50;

    public int GetCrowdCount()
    {
        RepairCrowdListIfNeeded();
        return crowdMemberTransforms.Count;
    }
    
    public float GetCurrentScale()
    {
        return currentScale;
    }

    private void RepairCrowdListIfNeeded()
    {
        crowdMemberTransforms.RemoveAll(t => t == null);
        if (crowdContainer == null) return;

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
        
        if (afterCount <= 0 && playerCounter != null)
        {
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
        if (crowdCount <= deformationThreshold)
        {
            float deformationRatio = 1f - ((float)crowdCount / deformationThreshold);
            deformationRatio = Mathf.Clamp01(deformationRatio);
            deformationRatio *= deformationFactor;
            currentSpacingX = Mathf.Lerp(baseSpacingX, minSpacingX, deformationRatio);
            currentSpacingZ = Mathf.Lerp(baseSpacingZ, minSpacingZ, deformationRatio);
        }
        else
        {
            currentSpacingX = baseSpacingX;
            currentSpacingZ = baseSpacingZ;
        }
    }
    
    private void CalculateDynamicScale()
    {
        int crowdCount = GetCrowdCount();
        if (crowdCount >= scalingThreshold)
        {
            float scaleRatio = (float)(crowdCount - scalingThreshold) / (maxCrowdSize - scalingThreshold);
            scaleRatio = Mathf.Clamp01(scaleRatio);
            scaleRatio *= scalingFactor;
            currentScale = Mathf.Lerp(baseScale, minScale, scaleRatio);
        }
        else
        {
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
        
        currentSpacingX = baseSpacingX;
        currentSpacingZ = baseSpacingZ;
        currentScale = baseScale;
        
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        FinishLine.IsWinning = false;
        Debug.Log("CrowdManager: Reset IsWinning flag to false in Awake()");
        
        InitializePool();
    }
    
    private void InitializePool()
    {
        if (!characterPrefab) return;
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject pooledCharacter = Instantiate(characterPrefab);
            pooledCharacter.SetActive(false);
            
            CrowdMember member = pooledCharacter.GetComponent<CrowdMember>() ?? pooledCharacter.AddComponent<CrowdMember>();
            
            if (pooledCharacter.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
            if (pooledCharacter.TryGetComponent<Collider>(out var col)) col.isTrigger = true;
            
            crowdPool.Enqueue(pooledCharacter);
        }
    }

    void Update()
    {
        // Debug key to force all crowd members to ground (for testing)
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Debug: Forcing all crowd members to ground (G key pressed)");
            ForceAllToGround();
        }
        
        // Debug key to check for stuck members (H key)
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Debug: Checking for stuck crowd members (H key pressed)");
            CheckForStuckMembers();
        }
    }

    void LateUpdate()
    {
        int crowdCount = GetCrowdCount();
        useOptimizations = crowdCount > performanceThreshold;
        
        CalculateDynamicSpacing();
        CalculateDynamicScale();
        
        crowdContainer.position = Vector3.Lerp(crowdContainer.position, transform.position, Time.deltaTime * followSmooth);
        crowdContainer.rotation = Quaternion.Slerp(crowdContainer.rotation, transform.rotation, Time.deltaTime * followSmooth);

        if (useOptimizations)
        {
            ArrangeCrowdOptimized();
        }
        else
        {
            ArrangeCrowd();
        }
        
        if (Time.frameCount % 60 == 0) ForceCleanupCrowdList();
    }

    public void AddCharacters(int amount)
    {
        if (!crowdContainer || !characterPrefab) return;
        if (FinishLine.IsWinning) return;

        if (amount <= maxSpawnPerFrame)
        {
            SpawnCharactersImmediate(amount);
        }
        else
        {
            spawnQueue.Enqueue(amount);
            if (!isSpawning) StartCoroutine(ProcessSpawnQueue());
        }
    }
    
    private void SpawnCharactersImmediate(int amount)
    {
        for (int i = 0; i < amount; i++) SpawnSingleCharacter();
        OnCrowdSizeChanged();
    }
    
    private void SpawnSingleCharacter()
    {
        GameObject newCharacter = GetPooledCharacter();
        if (newCharacter == null)
        {
            newCharacter = Instantiate(characterPrefab, transform.position, Quaternion.identity, crowdContainer);
            CrowdMember member = newCharacter.GetComponent<CrowdMember>() ?? newCharacter.AddComponent<CrowdMember>();
            if (newCharacter.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
            if (newCharacter.TryGetComponent<Collider>(out var col)) col.isTrigger = true;
        }
        else
        {
            newCharacter.transform.SetParent(crowdContainer);
            newCharacter.transform.position = transform.position;
            newCharacter.transform.rotation = Quaternion.identity;
            newCharacter.SetActive(true);
        }

        newCharacter.transform.localScale = Vector3.one * currentScale;
        crowdMemberTransforms.Add(newCharacter.transform);

        CrowdMember crowdMember = newCharacter.GetComponent<CrowdMember>();
        if (crowdMember != null)
        {
            crowdMember.Initialize(playerCounter);
            crowdMember.UpdateGroundReference(transform.position);
        }
    }
    
    private GameObject GetPooledCharacter()
    {
        if (crowdPool.Count > 0) return crowdPool.Dequeue();
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
        spawnQueue.Clear();
        isSpawning = false;
        StopAllCoroutines();
    }
    
    public void RemoveCharactersForFinishLine(int amount)
    {
        int amountToRemove = Mathf.Min(amount, crowdMemberTransforms.Count);
        if (amountToRemove <= 0) return;

        int startIndex = crowdMemberTransforms.Count - amountToRemove;
        for (int i = crowdMemberTransforms.Count - 1; i >= startIndex; i--)
        {
            Transform memberToRemove = crowdMemberTransforms[i];
            crowdMemberTransforms.RemoveAt(i);
            if (memberToRemove != null) ReturnToPool(memberToRemove.gameObject);
        }
        
        crowdMemberTransforms.RemoveAll(transform => transform == null);
        
        CalculateDynamicSpacing();
        CalculateDynamicScale();
    }
    
    private System.Collections.IEnumerator ProcessSpawnQueue()
    {
        isSpawning = true;
        
        while (spawnQueue.Count > 0)
        {
            if (FinishLine.IsWinning)
            {
                spawnQueue.Clear();
                break;
            }
            
            int totalToSpawn = spawnQueue.Dequeue();
            int spawned = 0;
            
            while (spawned < totalToSpawn)
            {
                if (FinishLine.IsWinning)
                {
                    isSpawning = false;
                    yield break;
                }
                
                int spawnThisBatch = Mathf.Min(maxSpawnPerFrame, totalToSpawn - spawned);
                for (int i = 0; i < spawnThisBatch; i++)
                {
                    SpawnSingleCharacter();
                    spawned++;
                }
                
                CalculateDynamicSpacing();
                CalculateDynamicScale();
                
                yield return new UnityEngine.WaitForSeconds(spawnBatchDelay);
            }
        }
        
        isSpawning = false;
        if (!FinishLine.IsWinning) OnCrowdSizeChanged();
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
            if (memberToRemove != null) ReturnToPool(memberToRemove.gameObject);
        }
        
        crowdMemberTransforms.RemoveAll(transform => transform == null);
        OnCrowdSizeChanged();
    }

    public void RemoveSpecificCharacter(Transform memberTransform)
    {
        if (memberTransform == null) return;

        if (crowdMemberTransforms.Contains(memberTransform))
        {
            crowdMemberTransforms.Remove(memberTransform);
            ReturnToPool(memberTransform.gameObject);
            OnCrowdSizeChanged();
        }
        crowdMemberTransforms.RemoveAll(transform => transform == null);
    }
    
    private void OnCrowdSizeChanged()
    {
        CalculateDynamicSpacing();
        CalculateDynamicScale();
        
        if (GetCrowdCount() <= 0 && playerCounter != null)
        {
            if (FinishLine.IsWinning)
            {
                Debug.Log("CrowdManager: Skipping game over check - player is winning!");
            }
            else
            {
                playerCounter.CheckForGameOver();
            }
        }
    }

    private void ArrangeCrowd()
    {
        int count = crowdMemberTransforms.Count;
        if (count <= 0) return;

        int actualColumns = Mathf.Min(columns, count);
        float formationWidth = (actualColumns - 1) * currentSpacingX;

        for (int i = 0; i < count; i++)
    {
        Transform follower = crowdMemberTransforms[i];
        if (follower == null) continue;

        // ===== ADD THIS CHECK HERE =====
        // If the member is jumping, skip all positioning logic for it.
        CrowdMember crowdMember = follower.GetComponent<CrowdMember>();
        if (crowdMember != null && crowdMember.IsCurrentlyJumping())
        {
            continue; // Go to the next crowd member
        }
            // ===== END OF FIX =====

            int row = i / actualColumns;
            int col = i % actualColumns;

            float xPos = (col * currentSpacingX) - (formationWidth / 2f);
            float zPos = -row * currentSpacingZ;

            Vector3 localOffset = new Vector3(xPos, 0, zPos);
            Vector3 targetPos = transform.position + (transform.rotation * localOffset);

            follower.position = Vector3.Lerp(follower.position, targetPos, Time.deltaTime * memberSmooth);
            follower.rotation = Quaternion.Slerp(follower.rotation, transform.rotation, Time.deltaTime * memberSmooth);
            
            Vector3 targetScale = Vector3.one * currentScale;
            follower.localScale = Vector3.Lerp(follower.localScale, targetScale, Time.deltaTime * memberSmooth);
        }
    }
    
    private void ArrangeCrowdOptimized()
    {
        int count = crowdMemberTransforms.Count;
        if (count <= 0) return;

        int actualColumns = useSimpleFormation && count > performanceThreshold ? 
            Mathf.Min(columns * 2, count) : Mathf.Min(columns, count);
        
        float formationWidth = (actualColumns - 1) * currentSpacingX;
        
        int membersToUpdate = Mathf.Min(membersPerFrameUpdate, count);
        
        for (int frame = 0; frame < membersToUpdate; frame++)
        {
            if (updateIndex >= count) updateIndex = 0;
            
            int i = updateIndex;
            Transform follower = crowdMemberTransforms[i];
            
            if (follower != null)
            {
                // ===== ADD THIS CHECK HERE =====
                CrowdMember crowdMember = follower.GetComponent<CrowdMember>();
                if (crowdMember != null && crowdMember.IsCurrentlyJumping())
                {
                    updateIndex++;
                    continue; // Skip to the next member in the update batch
                }
                // ===== END OF FIX =====

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
                        if (!follower.gameObject.activeInHierarchy) follower.gameObject.SetActive(true);
                    }
                }
                
                int row = i / actualColumns;
                int col = i % actualColumns;

                float xPos = (col * currentSpacingX) - (formationWidth / 2f);
                float zPos = -row * currentSpacingZ;

                Vector3 localOffset = new Vector3(xPos, 0, zPos);
                Vector3 targetPos = transform.position + (transform.rotation * localOffset);

                float optimizedSmooth = memberSmooth * 0.7f;
                
                follower.position = Vector3.Lerp(follower.position, targetPos, Time.deltaTime * optimizedSmooth);
                follower.rotation = Quaternion.Slerp(follower.rotation, transform.rotation, Time.deltaTime * optimizedSmooth);
                
                Vector3 targetScale = Vector3.one * currentScale;
                follower.localScale = Vector3.Lerp(follower.localScale, targetScale, Time.deltaTime * optimizedSmooth);
            }
            
            updateIndex++;
        }
    }
    
    // Debug method to force all crowd members to ground
    [System.Obsolete("Debug method - remove in production")]
    public void ForceAllToGround()
    {
        Debug.Log("CrowdManager: Forcing all crowd members to ground");
        int forcedCount = 0;
        foreach (Transform memberTransform in crowdMemberTransforms)
        {
            if (memberTransform != null)
            {
                CrowdMember member = memberTransform.GetComponent<CrowdMember>();
                if (member != null)
                {
                    member.ForceToGround();
                    forcedCount++;
                }
            }
        }
        Debug.Log($"CrowdManager: Forced {forcedCount} crowd members to ground");
    }
    
    // Debug method to check for stuck members
    [System.Obsolete("Debug method - remove in production")]
    public void CheckForStuckMembers()
    {
        int stuckCount = 0;
        int totalCount = 0;
        
        foreach (Transform memberTransform in crowdMemberTransforms)
        {
            if (memberTransform != null)
            {
                totalCount++;
                float playerY = transform.position.y;
                float memberY = memberTransform.position.y;
                float heightDiff = memberY - playerY;
                
                if (heightDiff > 2f) // Consider stuck if more than 2 units above player
                {
                    stuckCount++;
                    Debug.Log($"STUCK MEMBER: {memberTransform.name} at Y={memberY:F2} (player Y={playerY:F2}, diff={heightDiff:F2})");
                }
            }
        }
        
        Debug.Log($"CrowdManager: Found {stuckCount} stuck members out of {totalCount} total members");
        
        if (stuckCount > 0)
        {
            Debug.Log("Press G to force all members to ground, or they should land automatically");
        }
    }
}