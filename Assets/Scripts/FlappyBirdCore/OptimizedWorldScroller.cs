using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the player visually fixed while platforms and pooled obstacles move
/// across the screen. One manager updates every moving item, so obstacle
/// prefabs do not need their own Update methods.
/// </summary>
public sealed class OptimizedWorldScroller : MonoBehaviour
{
    [Header("Camera / Character")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform fixedCharacter;
    [SerializeField] private bool lockCharacterHorizontally = true;
    [SerializeField, Range(0f, 1f)] private float characterViewportX = 0.25f;
    [SerializeField, Min(0.01f)] private float worldPlaneDistance = 13f;

    [Header("Scrolling")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField, Min(0f)] private float scrollSpeed = 3.5f;

    [Header("Recycled Platform")]
    [SerializeField] private Transform platformRoot;
    [SerializeField] private Transform[] platformSegments = Array.Empty<Transform>();
    [SerializeField, Min(0.1f)] private float platformSegmentLength = 6f;

    [Header("Pooled Obstacles")]
    [SerializeField] private GameObject[] obstaclePrefabs = Array.Empty<GameObject>();
    [SerializeField] private Transform poolRoot;
    [SerializeField, Min(1)] private int prewarmPerPrefab = 4;
    [SerializeField] private bool allowPoolExpansion;
    [SerializeField, Min(0.05f)] private float spawnInterval = 1.75f;
    [SerializeField] private float spawnViewportX = 1.15f;
    [SerializeField] private float despawnViewportX = -0.15f;
    [SerializeField] private Vector2 spawnViewportYRange = new Vector2(0.25f, 0.78f);

    private readonly List<ObstaclePool> pools = new List<ObstaclePool>();
    private readonly List<ActiveObstacle> activeObstacles = new List<ActiveObstacle>(32);

    private Vector3 screenRight;
    private bool isRunning;
    private float spawnTimer;

    private sealed class ObstaclePool
    {
        public readonly GameObject prefab;
        public readonly Queue<GameObject> available;

        public ObstaclePool(GameObject source, int capacity)
        {
            prefab = source;
            available = new Queue<GameObject>(capacity);
        }
    }

    private struct ActiveObstacle
    {
        public Transform transform;
        public GameObject gameObject;
        public int poolIndex;

        public ActiveObstacle(GameObject instance, int sourcePool)
        {
            transform = instance.transform;
            gameObject = instance;
            poolIndex = sourcePool;
        }
    }

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            Debug.LogError("OptimizedWorldScroller needs a camera.", this);
            enabled = false;
            return;
        }

        screenRight = worldCamera.transform.right.normalized;
        ResolvePlatformSegments();
        AlignPlatformSegments();
        BuildObstaclePools();
        isRunning = playOnStart;
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        float distance = scrollSpeed * Time.deltaTime;
        Vector3 movement = -screenRight * distance;

        MoveAndRecyclePlatform(movement);
        MoveAndRecycleObstacles(movement);
        TickSpawner();
    }

    private void LateUpdate()
    {
        if (!lockCharacterHorizontally || fixedCharacter == null || worldCamera == null)
        {
            return;
        }

        Vector3 anchor = worldCamera.ViewportToWorldPoint(
            new Vector3(characterViewportX, 0.5f, worldPlaneDistance));
        Vector3 position = fixedCharacter.position;
        float horizontalOffset =
            Vector3.Dot(anchor, screenRight) - Vector3.Dot(position, screenRight);
        fixedCharacter.position = position + screenRight * horizontalOffset;
    }

    public void StartScrolling()
    {
        isRunning = true;
    }

    public void StopScrolling()
    {
        isRunning = false;
    }

    public void SetRunning(bool value)
    {
        isRunning = value;
    }

    public void ResetWorld()
    {
        for (int index = activeObstacles.Count - 1; index >= 0; index--)
        {
            ReleaseObstacle(index);
        }

        AlignPlatformSegments();
        spawnTimer = spawnInterval;
        isRunning = playOnStart;
    }

    private void ResolvePlatformSegments()
    {
        if (platformRoot == null)
        {
            platformRoot = transform.Find("PlatformSegments");
        }

        if (platformSegments != null && platformSegments.Length > 0)
        {
            return;
        }

        if (platformRoot == null)
        {
            platformSegments = Array.Empty<Transform>();
            return;
        }

        platformSegments = new Transform[platformRoot.childCount];
        for (int index = 0; index < platformRoot.childCount; index++)
        {
            platformSegments[index] = platformRoot.GetChild(index);
        }
    }

    private void AlignPlatformSegments()
    {
        if (platformSegments == null || platformSegments.Length == 0 || worldCamera == null)
        {
            return;
        }

        Vector3 viewportCenter = worldCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, worldPlaneDistance));
        float centerScalar = Vector3.Dot(viewportCenter, screenRight);
        float firstScalar =
            centerScalar - (platformSegments.Length - 1) * platformSegmentLength * 0.5f;

        for (int index = 0; index < platformSegments.Length; index++)
        {
            Transform segment = platformSegments[index];
            if (segment == null)
            {
                continue;
            }

            Vector3 position = segment.position;
            float currentScalar = Vector3.Dot(position, screenRight);
            float targetScalar = firstScalar + index * platformSegmentLength;
            segment.position = position + screenRight * (targetScalar - currentScalar);
        }
    }

    private void MoveAndRecyclePlatform(Vector3 movement)
    {
        if (platformSegments == null || platformSegments.Length == 0)
        {
            return;
        }

        float leftBoundary = Vector3.Dot(
            worldCamera.ViewportToWorldPoint(
                new Vector3(despawnViewportX, 0.5f, worldPlaneDistance)),
            screenRight);
        float furthestRight = float.NegativeInfinity;

        for (int index = 0; index < platformSegments.Length; index++)
        {
            Transform segment = platformSegments[index];
            if (segment == null)
            {
                continue;
            }

            segment.position += movement;
            furthestRight = Mathf.Max(furthestRight, Vector3.Dot(segment.position, screenRight));
        }

        float halfLength = platformSegmentLength * 0.5f;
        for (int index = 0; index < platformSegments.Length; index++)
        {
            Transform segment = platformSegments[index];
            if (segment == null)
            {
                continue;
            }

            float scalar = Vector3.Dot(segment.position, screenRight);
            if (scalar + halfLength >= leftBoundary)
            {
                continue;
            }

            float targetScalar = furthestRight + platformSegmentLength;
            segment.position += screenRight * (targetScalar - scalar);
            furthestRight = targetScalar;
        }
    }

    private void BuildObstaclePools()
    {
        pools.Clear();
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        if (poolRoot == null)
        {
            GameObject root = new GameObject("ObstaclePool");
            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
        }

        activeObstacles.Capacity = Mathf.Max(
            activeObstacles.Capacity,
            obstaclePrefabs.Length * prewarmPerPrefab);

        for (int poolIndex = 0; poolIndex < obstaclePrefabs.Length; poolIndex++)
        {
            GameObject prefab = obstaclePrefabs[poolIndex];
            ObstaclePool pool = new ObstaclePool(prefab, prewarmPerPrefab);
            pools.Add(pool);

            if (prefab == null)
            {
                continue;
            }

            for (int instanceIndex = 0; instanceIndex < prewarmPerPrefab; instanceIndex++)
            {
                pool.available.Enqueue(CreatePooledInstance(poolIndex));
            }
        }
    }

    private GameObject CreatePooledInstance(int poolIndex)
    {
        ObstaclePool pool = pools[poolIndex];
        GameObject instance = Instantiate(pool.prefab, poolRoot);
        instance.name = $"{pool.prefab.name}_Pooled";
        instance.SetActive(false);
        return instance;
    }

    private void TickSpawner()
    {
        if (pools.Count == 0)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer += spawnInterval;
        SpawnObstacle();
    }

    private void SpawnObstacle()
    {
        int startIndex = UnityEngine.Random.Range(0, pools.Count);
        int selectedPool = -1;

        for (int offset = 0; offset < pools.Count; offset++)
        {
            int candidate = (startIndex + offset) % pools.Count;
            ObstaclePool pool = pools[candidate];
            if (pool.prefab != null && (pool.available.Count > 0 || allowPoolExpansion))
            {
                selectedPool = candidate;
                break;
            }
        }

        if (selectedPool < 0)
        {
            return;
        }

        ObstaclePool selected = pools[selectedPool];
        GameObject instance = selected.available.Count > 0
            ? selected.available.Dequeue()
            : CreatePooledInstance(selectedPool);

        float viewportY = UnityEngine.Random.Range(
            Mathf.Min(spawnViewportYRange.x, spawnViewportYRange.y),
            Mathf.Max(spawnViewportYRange.x, spawnViewportYRange.y));
        Vector3 spawnPosition = worldCamera.ViewportToWorldPoint(
            new Vector3(spawnViewportX, viewportY, worldPlaneDistance));

        instance.transform.SetPositionAndRotation(
            spawnPosition,
            selected.prefab.transform.rotation);
        instance.SetActive(true);
        activeObstacles.Add(new ActiveObstacle(instance, selectedPool));
    }

    private void MoveAndRecycleObstacles(Vector3 movement)
    {
        float despawnScalar = Vector3.Dot(
            worldCamera.ViewportToWorldPoint(
                new Vector3(despawnViewportX, 0.5f, worldPlaneDistance)),
            screenRight);

        for (int index = activeObstacles.Count - 1; index >= 0; index--)
        {
            ActiveObstacle active = activeObstacles[index];
            active.transform.position += movement;

            if (Vector3.Dot(active.transform.position, screenRight) < despawnScalar)
            {
                ReleaseObstacle(index);
            }
        }
    }

    private void ReleaseObstacle(int activeIndex)
    {
        ActiveObstacle active = activeObstacles[activeIndex];
        active.gameObject.SetActive(false);
        pools[active.poolIndex].available.Enqueue(active.gameObject);

        int lastIndex = activeObstacles.Count - 1;
        activeObstacles[activeIndex] = activeObstacles[lastIndex];
        activeObstacles.RemoveAt(lastIndex);
    }

    private void OnValidate()
    {
        platformSegmentLength = Mathf.Max(0.1f, platformSegmentLength);
        spawnInterval = Mathf.Max(0.05f, spawnInterval);
        prewarmPerPrefab = Mathf.Max(1, prewarmPerPrefab);
        worldPlaneDistance = Mathf.Max(0.01f, worldPlaneDistance);
        characterViewportX = Mathf.Clamp01(characterViewportX);
    }
}
