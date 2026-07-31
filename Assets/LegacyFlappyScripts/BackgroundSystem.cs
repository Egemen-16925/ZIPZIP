namespace Legacy2D
{
using System.Collections.Generic;
using UnityEngine;

public class BackgroundSystem : MonoBehaviour
{
    [Header("Legacy scene references")]
    [SerializeField] private SpriteRenderer _firstSprite;
    [SerializeField] private SpriteRenderer _secondSprite;
    [SerializeField] private SpriteRenderer _thirdSprite;
    [SerializeField] private float _speed;
    [SerializeField] private float _reduceAmount;
    [SerializeField] private List<Sprite> _sprites;
    [SerializeField] private List<int> _obstacleChangeIndexes;

    [Header("Theme synchronization")]
    [SerializeField] private Spawner _spawner;
    [SerializeField] private List<Sprite> _baseThemes = new List<Sprite>();
    [SerializeField] private List<Sprite> _foregroundThemes = new List<Sprite>();
    [SerializeField, Min(1f)] private float _themeDuration = 28f;
    [SerializeField, Min(0.25f)] private float _crossfadeDuration = 5f;

    [Header("Parallax")]
    [SerializeField, HideInInspector, Min(0f)] private float _foregroundSpeed = 0.45f;
    [SerializeField] private int _baseSortingOrder = -20;
    [SerializeField] private int _foregroundSortingOrder = -7;

    private readonly SpriteRenderer[] currentForeground = new SpriteRenderer[2];
    private readonly SpriteRenderer[] nextForeground = new SpriteRenderer[2];

    private Camera targetCamera;
    private SpriteRenderer currentBase;
    private SpriteRenderer nextBase;
    private bool initialized;
    private bool isMoving;
    private bool isTransitioning;
    private bool obstacleThemeChanged;
    private int currentTheme;
    private int pendingTheme;
    private float themeElapsed;
    private float transitionElapsed;
    private float viewportWidth;
    private float viewportHeight;
    private float lastCameraX;

    private void Awake()
    {
        InitializeParallax();
    }

    private void Start()
    {
        ResetBG();
    }

    private void Update()
    {
        if (!initialized)
            return;

        FollowCamera();

        if (!isMoving)
            return;

        MoveForeground(_foregroundSpeed * Time.deltaTime);

        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        if (currentTheme >= ThemeCount - 1)
            return;

        themeElapsed += Time.deltaTime;
        if (themeElapsed >= _themeDuration)
            BeginTransition(currentTheme + 1);
    }

    private int ThemeCount => Mathf.Min(_baseThemes.Count, _foregroundThemes.Count);

    private void InitializeParallax()
    {
        targetCamera = Camera.main;
        if (targetCamera == null || ThemeCount == 0)
            return;

        DisableLegacyRenderers();

        currentBase = CreateRenderer("Parallax Base Current", _baseSortingOrder);
        nextBase = CreateRenderer("Parallax Base Next", _baseSortingOrder + 1);

        for (int i = 0; i < 2; i++)
        {
            currentForeground[i] = CreateRenderer("Parallax Foreground Current " + i, _foregroundSortingOrder);
            nextForeground[i] = CreateRenderer("Parallax Foreground Next " + i, _foregroundSortingOrder + 1);
        }

        initialized = true;
    }

    private SpriteRenderer CreateRenderer(string objectName, int sortingOrder)
    {
        GameObject layerObject = new GameObject(objectName);
        layerObject.transform.SetParent(transform, false);

        SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;

        SpriteRenderer template = _firstSprite != null ? _firstSprite : GetComponentInChildren<SpriteRenderer>();
        if (template != null)
        {
            renderer.sortingLayerID = template.sortingLayerID;
            renderer.sharedMaterial = template.sharedMaterial;
        }

        return renderer;
    }

    private void DisableLegacyRenderers()
    {
        if (_firstSprite != null)
            _firstSprite.enabled = false;
        if (_secondSprite != null)
            _secondSprite.enabled = false;
        if (_thirdSprite != null)
            _thirdSprite.enabled = false;
    }

    private void RefreshViewport()
    {
        viewportHeight = targetCamera.orthographicSize * 2f;
        viewportWidth = viewportHeight * targetCamera.aspect;
    }

    private void SetThemeInstantly(int themeIndex)
    {
        currentTheme = Mathf.Clamp(themeIndex, 0, ThemeCount - 1);
        pendingTheme = currentTheme;
        isTransitioning = false;
        obstacleThemeChanged = false;
        themeElapsed = 0f;
        transitionElapsed = 0f;

        currentBase.sprite = _baseThemes[currentTheme];
        nextBase.sprite = _baseThemes[currentTheme];
        SetAlpha(currentBase, 1f);
        SetAlpha(nextBase, 0f);

        for (int i = 0; i < 2; i++)
        {
            currentForeground[i].sprite = _foregroundThemes[currentTheme];
            currentForeground[i].flipX = i == 1;
            nextForeground[i].sprite = _foregroundThemes[currentTheme];
            nextForeground[i].flipX = i == 1;
            SetAlpha(currentForeground[i], 1f);
            SetAlpha(nextForeground[i], 0f);
        }

        LayoutLayers(true);
        ApplyObstacleTheme(currentTheme);
    }

    private void LayoutLayers(bool resetForegroundPositions)
    {
        RefreshViewport();
        Vector3 cameraPosition = targetCamera.transform.position;
        lastCameraX = cameraPosition.x;

        LayoutBase(currentBase, cameraPosition);
        LayoutBase(nextBase, cameraPosition);

        for (int i = 0; i < 2; i++)
        {
            LayoutForeground(currentForeground[i]);
            LayoutForeground(nextForeground[i]);

            if (resetForegroundPositions)
            {
                Vector3 position = new Vector3(cameraPosition.x + (i * viewportWidth), cameraPosition.y, 0f);
                currentForeground[i].transform.position = position;
                nextForeground[i].transform.position = position;
            }
        }
    }

    private void LayoutBase(SpriteRenderer renderer, Vector3 cameraPosition)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = Mathf.Max(viewportWidth / spriteSize.x, viewportHeight / spriteSize.y);
        SetWorldScale(renderer.transform, scale);
        renderer.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
    }

    private void LayoutForeground(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = viewportWidth / spriteSize.x;
        SetWorldScale(renderer.transform, scale);
    }

    private static void SetWorldScale(Transform layerTransform, float uniformScale)
    {
        Vector3 parentScale = layerTransform.parent != null
            ? layerTransform.parent.lossyScale
            : Vector3.one;

        layerTransform.localScale = new Vector3(
            uniformScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
            uniformScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
            1f / Mathf.Max(Mathf.Abs(parentScale.z), 0.0001f));
    }

    private void FollowCamera()
    {
        Vector3 cameraPosition = targetCamera.transform.position;
        float cameraDeltaX = cameraPosition.x - lastCameraX;
        lastCameraX = cameraPosition.x;

        LayoutBase(currentBase, cameraPosition);
        LayoutBase(nextBase, cameraPosition);

        if (!Mathf.Approximately(cameraDeltaX, 0f))
        {
            for (int i = 0; i < 2; i++)
            {
                currentForeground[i].transform.position += Vector3.right * cameraDeltaX;
                nextForeground[i].transform.position += Vector3.right * cameraDeltaX;
            }
        }
    }

    private void MoveForeground(float distance)
    {
        float cameraLeft = targetCamera.transform.position.x - (viewportWidth * 0.5f);

        for (int i = 0; i < 2; i++)
            currentForeground[i].transform.position += Vector3.left * distance;

        for (int i = 0; i < 2; i++)
        {
            SpriteRenderer renderer = currentForeground[i];
            if (renderer.transform.position.x + (viewportWidth * 0.5f) >= cameraLeft)
                continue;

            SpriteRenderer other = currentForeground[1 - i];
            renderer.transform.position = new Vector3(
                other.transform.position.x + viewportWidth - 0.01f,
                targetCamera.transform.position.y,
                renderer.transform.position.z);
            renderer.flipX = !other.flipX;
        }

        for (int i = 0; i < 2; i++)
        {
            nextForeground[i].transform.position = currentForeground[i].transform.position;
            nextForeground[i].transform.localScale = currentForeground[i].transform.localScale;
            nextForeground[i].flipX = currentForeground[i].flipX;
        }
    }

    private void BeginTransition(int nextThemeIndex)
    {
        pendingTheme = Mathf.Clamp(nextThemeIndex, 0, ThemeCount - 1);
        transitionElapsed = 0f;
        isTransitioning = true;
        obstacleThemeChanged = false;

        nextBase.sprite = _baseThemes[pendingTheme];
        LayoutBase(nextBase, targetCamera.transform.position);
        SetAlpha(nextBase, 0f);

        for (int i = 0; i < 2; i++)
        {
            nextForeground[i].sprite = _foregroundThemes[pendingTheme];
            LayoutForeground(nextForeground[i]);
            nextForeground[i].transform.position = currentForeground[i].transform.position;
            nextForeground[i].flipX = currentForeground[i].flipX;
            SetAlpha(nextForeground[i], 0f);
        }
    }

    private void UpdateTransition()
    {
        transitionElapsed += Time.deltaTime;
        float blend = Mathf.Clamp01(transitionElapsed / _crossfadeDuration);
        float smoothBlend = blend * blend * (3f - (2f * blend));

        // Keep the outgoing opaque image underneath the incoming one. Fading both
        // exposes the camera clear colour and creates a pale flash at mid-blend.
        SetAlpha(currentBase, 1f);
        SetAlpha(nextBase, smoothBlend);

        for (int i = 0; i < 2; i++)
        {
            SetAlpha(currentForeground[i], 1f - smoothBlend);
            SetAlpha(nextForeground[i], smoothBlend);
        }

        if (!obstacleThemeChanged && blend >= 0.5f)
        {
            ApplyObstacleTheme(pendingTheme);
            obstacleThemeChanged = true;
        }

        if (blend >= 1f)
            CompleteTransition();
    }

    private void CompleteTransition()
    {
        currentTheme = pendingTheme;
        themeElapsed = 0f;
        transitionElapsed = 0f;
        isTransitioning = false;

        currentBase.sprite = nextBase.sprite;
        SetAlpha(currentBase, 1f);
        SetAlpha(nextBase, 0f);

        for (int i = 0; i < 2; i++)
        {
            currentForeground[i].sprite = nextForeground[i].sprite;
            SetAlpha(currentForeground[i], 1f);
            SetAlpha(nextForeground[i], 0f);
        }
    }

    private void ApplyObstacleTheme(int themeIndex)
    {
        if (_spawner == null)
            return;

        _spawner.SetNewObstacles((ObstacleTypes)Mathf.Clamp(themeIndex, 0, 4));
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
            return;

        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    public void StartMoving()
    {
        if (!initialized)
            InitializeParallax();

        isMoving = initialized;
    }

    public void SetForegroundSpeed(float speed)
    {
        _foregroundSpeed = Mathf.Max(0f, speed);
    }

    public float ForegroundSpeed => _foregroundSpeed;

    public void StopMoving()
    {
        isMoving = false;
    }

    public void ResetBG()
    {
        if (!initialized)
            InitializeParallax();

        isMoving = false;
        if (initialized)
            SetThemeInstantly(0);
    }
}
}
