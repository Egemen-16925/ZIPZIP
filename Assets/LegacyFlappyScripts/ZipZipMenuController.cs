namespace Legacy2D
{
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(10)]
public sealed class ZipZipMenuController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Player player;

    [Header("Generated UI Art")]
    [SerializeField] private Sprite panelFrame;
    [SerializeField] private Sprite panelBorder;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite logoSprite;

    private static readonly string[] CharacterNames =
    {
        "TONTON",
        "TABLO",
        "PATIRCIK",
        "KEDİŞ"
    };

    // Avatar asset indices stay unchanged so existing saves remain valid.
    // Only their order in the character selection screen is rearranged.
    private static readonly int[] CharacterDisplayOrder = { 0, 2, 3, 1 };
    private const string AutoStartKey = "ZipZip.AutoStartSelectedAvatar";

    private static readonly Color Navy = new Color(0.015f, 0.075f, 0.19f, 1f);
    private static readonly Color Sky = new Color(0.12f, 0.72f, 0.95f, 1f);
    private static readonly Color Gold = new Color(1f, 0.72f, 0.15f, 1f);
    private static readonly Color Orange = new Color(1f, 0.32f, 0.055f, 1f);
    private static readonly Color Cream = new Color(1f, 0.96f, 0.78f, 1f);

    private Sprite roundedSprite;
    private RectTransform menuRoot;
    private GameObject screenDim;
    private GameObject mainPanel;
    private GameObject characterPanel;
    private GameObject settingsPanel;
    private GameObject pausePanel;
    private GameObject gameOverPanel;
    private GameObject retryButton;
    private GameObject pauseHudButton;
    private Image brightnessOverlay;
    private TMP_Text selectedNameText;
    private TMP_Text gameOverScoreText;
    private RawImage mainPreviewImage;
    private RawImage characterPreviewImage;
    private Outline[] characterCardOutlines;
    private GameObject previewRoot;
    private GameObject[] previewCharacters;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private GameObject scoreObject;
    private GameObject healthRoot;
    private int selectedIndex;
    private int lastShownGameOverScore = -1;
    private bool gameStarted;
    private bool autoStartRequested;

    private void Awake()
    {
        ResolveReferences();
        if (rootCanvas == null || gameManager == null || player == null)
        {
            Debug.LogError("ZipZip menu could not resolve Canvas, GameManager, or Player.");
            enabled = false;
            return;
        }

        roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        selectedIndex = Mathf.Clamp(PlayerPrefs.GetInt("ZipZip.SelectedAvatar", 0), 0, 3);
        autoStartRequested = PlayerPrefs.GetInt(AutoStartKey, 0) == 1;
        if (autoStartRequested)
        {
            PlayerPrefs.DeleteKey(AutoStartKey);
            PlayerPrefs.Save();
        }
        AudioListener.volume = Mathf.Clamp01(PlayBoxLauncherDataManager.GetSound);

        DisableLegacyInterface();
        StyleScore();
        StyleHealthBar();
        BuildPreviewCharacters();
        BuildInterface();

        player.SetAvatarIndex(selectedIndex);
        player.birds.gameObject.SetActive(false);
        ShowMainMenu();

        gameManager.ConfigureMenuUi(mainPanel, retryButton, gameOverPanel);
    }

    private void Start()
    {
        if (autoStartRequested)
        {
            StartGame();
        }
    }

    private void Update()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf && gameOverScoreText != null && lastShownGameOverScore != gameManager.score)
        {
            lastShownGameOverScore = gameManager.score;
            gameOverScoreText.text = gameManager.score.ToString();
            screenDim.SetActive(true);
            pauseHudButton.SetActive(false);
        }

        if (gameStarted && Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausePanel.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                OpenPauseMenu();
            }
        }
    }

    private void OnDestroy()
    {
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }

    private void ResolveReferences()
    {
        if (rootCanvas == null)
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject != null)
            {
                rootCanvas = canvasObject.GetComponent<Canvas>();
            }
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
        }
    }

    private void DisableLegacyInterface()
    {
        SetLegacyObjectActive("AvatarSelectScren", false);
        SetLegacyObjectActive("PlayButton", false);
        SetLegacyObjectActive("RestartButton", false);
        SetLegacyObjectActive("GameOver", false);
        SetLegacyObjectActive("PausePanel", false);
        SetLegacyObjectActive("MenuButton", false);

        GameObject score = FindCanvasObject("Score");
        scoreObject = score;
        if (scoreObject != null)
        {
            scoreObject.SetActive(false);
        }

        if (player.healthBar != null)
        {
            healthRoot = player.healthBar.transform.parent.gameObject;
            healthRoot.SetActive(false);
        }
    }

    private GameObject FindCanvasObject(string relativePath)
    {
        Transform target = rootCanvas.transform.Find(relativePath);
        return target != null ? target.gameObject : null;
    }

    private void SetLegacyObjectActive(string relativePath, bool state)
    {
        GameObject target = FindCanvasObject(relativePath);
        if (target != null)
        {
            target.SetActive(state);
        }
    }

    private void StyleScore()
    {
        GameObject score = FindCanvasObject("Score");
        if (score == null)
        {
            return;
        }

        RectTransform rect = score.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -34f);
        rect.sizeDelta = new Vector2(420f, 150f);
        rect.localScale = Vector3.one;

        Text text = score.GetComponent<Text>();
        if (text != null)
        {
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 92;
            text.fontStyle = FontStyle.Bold;
            text.color = Cream;
        }

        Outline outline = score.GetComponent<Outline>();
        if (outline == null)
        {
            outline = score.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.01f, 0.05f, 0.14f, 0.95f);
        outline.effectDistance = new Vector2(5f, -5f);
    }

    private void StyleHealthBar()
    {
        if (player.healthBar == null)
        {
            return;
        }

        Canvas playerCanvas = player.healthBar.GetComponentInParent<Canvas>(true);
        if (playerCanvas == null)
        {
            return;
        }

        RectTransform playerCanvasRect = playerCanvas.GetComponent<RectTransform>();
        playerCanvasRect.localPosition = new Vector3(0f, 1.28f, 2f);

        Image fill = player.healthBar;
        RectTransform frameRect = fill.transform.parent.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(172f, 30f);
        frameRect.localScale = Vector3.one;

        Image frame = frameRect.GetComponent<Image>();
        frame.sprite = roundedSprite;
        frame.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        frame.color = new Color(0.015f, 0.06f, 0.15f, 0.96f);
        frame.raycastTarget = false;

        Outline outline = frame.GetComponent<Outline>();
        if (outline == null)
        {
            outline = frame.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(6f, 6f);
        fillRect.offsetMax = new Vector2(-6f, -6f);
        fillRect.localScale = Vector3.one;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.color = new Color(1f, 0.24f, 0.06f, 1f);
        fill.raycastTarget = false;
    }

    private void BuildPreviewCharacters()
    {
        const int previewLayer = 30;
        previewRoot = new GameObject("MenuCharacterPreviews");
        previewCharacters = new GameObject[4];

        previewTexture = new RenderTexture(1600, 600, 24, RenderTextureFormat.ARGB32)
        {
            name = "ZipZip_MenuCharacterPreview",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 2
        };
        previewTexture.Create();

        GameObject cameraObject = new GameObject("MenuPreviewCamera");
        cameraObject.transform.SetParent(previewRoot.transform, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -20f);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 3.15f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 50f;
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.targetTexture = previewTexture;
        previewCamera.allowHDR = false;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.cullingMask &= ~(1 << previewLayer);
        }

        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (i >= player.Avatars.Count || player.Avatars[i] == null)
            {
                continue;
            }

            GameObject source = player.Avatars[i];
            GameObject clone = Instantiate(source, previewRoot.transform);
            clone.name = "Preview_" + CharacterNames[i];
            clone.transform.localScale = source.transform.lossyScale;
            clone.transform.rotation = Quaternion.Euler(-90f, 90f, 0f);
            SetLayerRecursively(clone, previewLayer);
            clone.SetActive(true);

            foreach (Collider collider3D in clone.GetComponentsInChildren<Collider>(true))
            {
                collider3D.enabled = false;
            }

            foreach (Collider2D collider2D in clone.GetComponentsInChildren<Collider2D>(true))
            {
                collider2D.enabled = false;
            }

            previewCharacters[i] = clone;
        }
    }

    private void BuildInterface()
    {
        menuRoot = CreateRect("ModernMenuRoot", rootCanvas.transform, Vector2.zero, Vector2.zero);
        Stretch(menuRoot);

        brightnessOverlay = CreateImage("BrightnessOverlay", menuRoot, null, Color.clear);
        Stretch(brightnessOverlay.rectTransform);
        brightnessOverlay.raycastTarget = false;

        Image dim = CreateImage("ScreenDim", menuRoot, null, Color.clear);
        Stretch(dim.rectTransform);
        dim.raycastTarget = false;
        screenDim = dim.gameObject;

        BuildMainPanel();
        BuildCharacterPanel();
        BuildSettingsPanel();
        BuildPausePanel();
        BuildGameOverPanel();
        BuildPauseHudButton();

        ApplyBrightness(PlayerPrefs.GetFloat("ZipZip.Brightness", 1f));
    }

    private void BuildMainPanel()
    {
        mainPanel = CreatePanel("MainPanel", menuRoot, new Vector2(1760f, 920f), true);

        Image logo = CreateImage("ZipZipLogo", mainPanel.transform, logoSprite, Color.white);
        logo.rectTransform.anchoredPosition = new Vector2(0f, 305f);
        logo.rectTransform.sizeDelta = new Vector2(760f, 205f);
        logo.preserveAspect = true;
        logo.raycastTarget = false;

        mainPreviewImage = CreateRawImage("SelectedCharacterPreview", mainPanel.transform, new Vector2(0f, -8f), new Vector2(1600f, 570f));
        selectedNameText = CreateNamePlate("SelectedName", mainPanel.transform, CharacterNames[selectedIndex], new Vector2(-355f, -222f), new Vector2(410f, 84f));

        CreateButton("Play", mainPanel.transform, "BAŞLA", new Vector2(390f, 112f), new Vector2(430f, 104f), Orange, StartGame);
        CreateButton("Characters", mainPanel.transform, "KARAKTERLER", new Vector2(390f, -18f), new Vector2(430f, 104f), new Color(0.05f, 0.55f, 0.88f, 1f), OpenCharacterPanel);
        CreateButton("Settings", mainPanel.transform, "AYARLAR", new Vector2(390f, -148f), new Vector2(430f, 104f), new Color(0.10f, 0.28f, 0.55f, 1f), OpenSettingsPanel);
    }

    private void BuildCharacterPanel()
    {
        characterPanel = CreatePanel("CharacterPanel", menuRoot, new Vector2(2240f, 1080f), true);

        characterPreviewImage = CreateRawImage("AllCharacterPreviews", characterPanel.transform, new Vector2(0f, 20f), new Vector2(1900f, 610f));

        characterCardOutlines = new Outline[4];
        float[] xPositions = { -660f, -220f, 220f, 660f };

        for (int slot = 0; slot < CharacterDisplayOrder.Length; slot++)
        {
            int avatarIndex = CharacterDisplayOrder[slot];
            int capturedIndex = avatarIndex;
            Button card = CreateButton(
                "CharacterCard_" + CharacterNames[avatarIndex],
                characterPanel.transform,
                string.Empty,
                new Vector2(xPositions[slot], -8f),
                new Vector2(380f, 520f),
                Color.clear,
                () => SetSelectedCharacter(capturedIndex));

            card.image.color = Color.clear;
            characterCardOutlines[avatarIndex] = card.GetComponent<Outline>();
            CreateNamePlate("Name", card.transform, CharacterNames[avatarIndex], new Vector2(0f, -197f), new Vector2(330f, 72f));
        }

        CreateButton("Back", characterPanel.transform, "GERİ", new Vector2(0f, -382f), new Vector2(340f, 86f), new Color(0.10f, 0.28f, 0.55f, 1f), ShowMainMenu);
        UpdateCharacterSelectionVisuals();
    }

    private void BuildSettingsPanel()
    {
        settingsPanel = CreatePanel("SettingsPanel", menuRoot, new Vector2(1220f, 780f), true);
        CreateText("Title", settingsPanel.transform, "AYARLAR", 74f, Gold, new Vector2(0f, 245f), new Vector2(700f, 96f));
        CreateText("SoundLabel", settingsPanel.transform, "SES", 31f, Cream, new Vector2(-330f, 100f), new Vector2(240f, 55f));
        CreateSlider("Sound", settingsPanel.transform, new Vector2(125f, 100f), AudioListener.volume, ApplySound);
        CreateText("BrightnessLabel", settingsPanel.transform, "PARLAKLIK", 31f, Cream, new Vector2(-330f, -20f), new Vector2(260f, 55f));
        CreateSlider("Brightness", settingsPanel.transform, new Vector2(125f, -20f), PlayerPrefs.GetFloat("ZipZip.Brightness", 1f), ApplyBrightness);
        CreateButton("Back", settingsPanel.transform, "GERİ", new Vector2(0f, -205f), new Vector2(360f, 92f), Orange, ShowMainMenu);
    }

    private void BuildPausePanel()
    {
        pausePanel = CreatePanel("PausePanelModern", menuRoot, new Vector2(1320f, 880f), true);
        CreateText("Title", pausePanel.transform, "OYUN DURAKLATILDI", 65f, Gold, new Vector2(0f, 290f), new Vector2(900f, 92f));
        CreateText("SoundLabel", pausePanel.transform, "SES", 28f, Cream, new Vector2(-360f, 145f), new Vector2(220f, 50f));
        CreateSlider("Sound", pausePanel.transform, new Vector2(130f, 145f), AudioListener.volume, ApplySound);
        CreateText("BrightnessLabel", pausePanel.transform, "PARLAKLIK", 28f, Cream, new Vector2(-360f, 42f), new Vector2(250f, 50f));
        CreateSlider("Brightness", pausePanel.transform, new Vector2(130f, 42f), PlayerPrefs.GetFloat("ZipZip.Brightness", 1f), ApplyBrightness);
        CreateButton("Resume", pausePanel.transform, "DEVAM ET", new Vector2(0f, -92f), new Vector2(430f, 88f), Orange, ResumeGame);
        CreateButton("Restart", pausePanel.transform, "YENİDEN BAŞLAT", new Vector2(0f, -197f), new Vector2(430f, 88f), new Color(0.05f, 0.55f, 0.88f, 1f), ReloadCurrentScene);
        CreateButton("MainMenu", pausePanel.transform, "ANA MENÜ", new Vector2(0f, -302f), new Vector2(430f, 88f), new Color(0.10f, 0.28f, 0.55f, 1f), ReloadCurrentScene);
    }

    private void BuildGameOverPanel()
    {
        gameOverPanel = CreatePanel("GameOverPanelModern", menuRoot, new Vector2(1040f, 690f), true);
        gameOverScoreText = CreateScoreDisplay(gameOverPanel.transform, new Vector2(0f, 145f));
        retryButton = CreateButton("Retry", gameOverPanel.transform, "TEKRAR DENE", new Vector2(0f, 12f), new Vector2(430f, 94f), Orange, RetrySelectedCharacter).gameObject;
        CreateButton("MainMenu", gameOverPanel.transform, "ANA MENÜ", new Vector2(0f, -108f), new Vector2(430f, 88f), new Color(0.10f, 0.28f, 0.55f, 1f), ReloadCurrentScene);
        gameOverPanel.SetActive(false);
    }

    private void BuildPauseHudButton()
    {
        Button button = CreateButton("PauseHudButton", menuRoot, "II", new Vector2(-132f, -64f), new Vector2(230f, 82f), Orange, OpenPauseMenu);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-32f, -28f);
        pauseHudButton = button.gameObject;
        pauseHudButton.SetActive(false);
    }

    private GameObject CreatePanel(string objectName, Transform parent, Vector2 size, bool useFilledFrame)
    {
        RectTransform panel = CreateRect(objectName, parent, Vector2.zero, size);

        Sprite frameSprite = panelFrame != null ? panelFrame : panelBorder;
        Image frame = CreateImage("GeneratedFrame", panel, frameSprite, Color.white);
        Stretch(frame.rectTransform);
        frame.preserveAspect = false;
        frame.raycastTarget = false;
        return panel.gameObject;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size, Color normalColor, Action action)
    {
        RectTransform rect = CreateRect(objectName, parent, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        bool useDecorativeButton = buttonSprite != null && size.x / Mathf.Max(1f, size.y) >= 2.5f;
        image.sprite = useDecorativeButton ? buttonSprite : roundedSprite;
        image.type = useDecorativeButton || roundedSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        image.color = useDecorativeButton ? Color.white : normalColor;

        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.02f, 0.08f, 0.72f);
        shadow.effectDistance = new Vector2(0f, -7f);

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.78f, 0.25f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.86f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => action());

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText("Label", button.transform, label, 36f, Cream, Vector2.zero, size - new Vector2(24f, 12f));
            text.raycastTarget = false;
        }

        return button;
    }

    private Slider CreateSlider(string objectName, Transform parent, Vector2 position, float value, Action<float> callback)
    {
        RectTransform root = CreateRect(objectName, parent, position, new Vector2(650f, 54f));
        Slider slider = root.gameObject.AddComponent<Slider>();

        Image background = CreateImage("Background", root, roundedSprite, new Color(0.01f, 0.06f, 0.16f, 0.96f));
        Stretch(background.rectTransform, 0f, 8f, 0f, 8f);
        background.type = Image.Type.Sliced;

        RectTransform fillArea = CreateRect("Fill Area", root, Vector2.zero, Vector2.zero);
        Stretch(fillArea, 11f, 11f, 11f, 11f);
        Image fill = CreateImage("Fill", fillArea, roundedSprite, Sky);
        Stretch(fill.rectTransform);
        fill.type = Image.Type.Sliced;

        RectTransform handleArea = CreateRect("Handle Slide Area", root, Vector2.zero, Vector2.zero);
        Stretch(handleArea, 13f, 13f, 0f, 0f);
        Image handle = CreateImage("Handle", handleArea, roundedSprite, Gold);
        handle.rectTransform.sizeDelta = new Vector2(44f, 62f);
        handle.type = Image.Type.Sliced;

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        slider.onValueChanged.AddListener(v => callback(v));
        return slider;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string content, float fontSize, Color color, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.outlineColor = new Color32(5, 18, 48, 230);
        text.outlineWidth = 0.16f;
        return text;
    }

    private Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent, Vector2.zero, new Vector2(100f, 100f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : roundedSprite;
        image.color = color;
        if (image.sprite != null && image.sprite == roundedSprite)
        {
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    private RawImage CreateRawImage(string objectName, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent, position, size);
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.texture = previewTexture;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateNamePlate(string objectName, Transform parent, string content, Vector2 position, Vector2 size)
    {
        RectTransform plate = CreateRect(objectName, parent, position, size);
        Image background = plate.gameObject.AddComponent<Image>();
        background.sprite = buttonSprite != null ? buttonSprite : roundedSprite;
        background.type = buttonSprite != null || roundedSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        background.color = Color.white;
        background.raycastTarget = false;

        Shadow shadow = plate.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.02f, 0.08f, 0.72f);
        shadow.effectDistance = new Vector2(0f, -5f);

        TMP_Text text = CreateText("Label", plate, content, 34f, Cream, Vector2.zero, size - new Vector2(30f, 12f));
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text CreateScoreDisplay(Transform parent, Vector2 position)
    {
        Vector2 size = new Vector2(560f, 164f);
        RectTransform plate = CreateRect("ScoreDisplay", parent, position, size);
        Image background = plate.gameObject.AddComponent<Image>();
        background.sprite = buttonSprite != null ? buttonSprite : roundedSprite;
        background.type = buttonSprite != null || roundedSprite == null ? Image.Type.Simple : Image.Type.Sliced;
        background.color = Color.white;
        background.raycastTarget = false;

        Shadow shadow = plate.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.02f, 0.08f, 0.78f);
        shadow.effectDistance = new Vector2(0f, -7f);

        CreateText("Caption", plate, "SKOR", 27f, Navy, new Vector2(0f, 34f), new Vector2(300f, 44f)).raycastTarget = false;
        TMP_Text value = CreateText("Value", plate, "0", 72f, Cream, new Vector2(0f, -23f), new Vector2(350f, 84f));
        value.raycastTarget = false;
        return value;
    }

    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private void ShowMainMenu()
    {
        Time.timeScale = 1f;
        gameStarted = false;
        screenDim.SetActive(true);
        mainPanel.SetActive(true);
        characterPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pauseHudButton.SetActive(false);
        if (scoreObject != null) scoreObject.SetActive(false);
        if (healthRoot != null) healthRoot.SetActive(false);
        player.birds.gameObject.SetActive(false);
        ShowSinglePreview(selectedIndex);
        selectedNameText.text = CharacterNames[selectedIndex];
    }

    private void OpenCharacterPanel()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        characterPanel.SetActive(true);
        ShowAllPreviews();
        UpdateCharacterSelectionVisuals();
    }

    private void OpenSettingsPanel()
    {
        mainPanel.SetActive(false);
        characterPanel.SetActive(false);
        settingsPanel.SetActive(true);
        HideAllPreviews();
    }

    private void SetSelectedCharacter(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, 3);
        UpdateCharacterSelectionVisuals();
        PlayerPrefs.SetInt("ZipZip.SelectedAvatar", selectedIndex);
        PlayerPrefs.Save();
        player.SetAvatarIndex(selectedIndex);
        ShowMainMenu();
    }

    private void UpdateCharacterSelectionVisuals()
    {
        if (characterCardOutlines == null)
        {
            return;
        }

        for (int i = 0; i < characterCardOutlines.Length; i++)
        {
            bool selected = i == selectedIndex;
            characterCardOutlines[i].effectColor = selected ? Gold : new Color(0.17f, 0.55f, 0.78f, 0.60f);
            characterCardOutlines[i].effectDistance = selected ? new Vector2(5f, -5f) : new Vector2(1.5f, -1.5f);
        }
    }

    private void StartGame()
    {
        PlayerPrefs.SetInt("ZipZip.SelectedAvatar", selectedIndex);
        PlayerPrefs.Save();

        HideAllPreviews();
        previewRoot.SetActive(false);
        screenDim.SetActive(false);
        mainPanel.SetActive(false);
        characterPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pauseHudButton.SetActive(true);
        if (scoreObject != null) scoreObject.SetActive(true);
        if (healthRoot != null) healthRoot.SetActive(true);

        player.birds.gameObject.SetActive(true);
        player.SetAvatarIndex(selectedIndex);
        gameStarted = true;
        gameManager.Play();
    }

    private void OpenPauseMenu()
    {
        if (!gameStarted || gameOverPanel.activeSelf)
        {
            return;
        }

        Time.timeScale = 0f;
        screenDim.SetActive(true);
        pausePanel.SetActive(true);
        pauseHudButton.SetActive(false);
    }

    private void ResumeGame()
    {
        pausePanel.SetActive(false);
        screenDim.SetActive(false);
        pauseHudButton.SetActive(true);
        Time.timeScale = 1f;
    }

    private void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void RetrySelectedCharacter()
    {
        PlayerPrefs.SetInt("ZipZip.SelectedAvatar", selectedIndex);
        PlayerPrefs.SetInt(AutoStartKey, 1);
        PlayerPrefs.Save();
        ReloadCurrentScene();
    }

    private void ApplySound(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;
        PlayBoxLauncherDataManager.SetSound(value);
    }

    private void ApplyBrightness(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("ZipZip.Brightness", value);
        if (brightnessOverlay != null)
        {
            brightnessOverlay.color = new Color(0f, 0f, 0f, (1f - value) * 0.72f);
        }
    }

    private void ShowSinglePreview(int index)
    {
        previewRoot.SetActive(true);
        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] == null) continue;
            bool active = i == index;
            previewCharacters[i].SetActive(active);
            if (active)
            {
                previewCharacters[i].transform.position = new Vector3(-3.45f, -0.05f, -7f);
                previewCharacters[i].transform.rotation = GetPreviewRotation(i);
            }
        }
    }

    private void ShowAllPreviews()
    {
        previewRoot.SetActive(true);
        float[] xPositions = { -6.35f, -2.12f, 2.12f, 6.35f };
        float[] yPositions = { 0.05f, 0.05f, 0.05f, 0.00f };

        for (int slot = 0; slot < CharacterDisplayOrder.Length; slot++)
        {
            int avatarIndex = CharacterDisplayOrder[slot];
            if (previewCharacters[avatarIndex] == null) continue;
            previewCharacters[avatarIndex].SetActive(true);
            previewCharacters[avatarIndex].transform.position = new Vector3(xPositions[slot], yPositions[slot], -7f);
            previewCharacters[avatarIndex].transform.rotation = GetPreviewRotation(avatarIndex);
        }
    }

    private static Quaternion GetPreviewRotation(int avatarIndex)
    {
        if (avatarIndex == 3)
        {
            return Quaternion.Euler(-180f, -90f, 180f);
        }

        if (avatarIndex == 1)
        {
            return Quaternion.Euler(-30f, 90f, 0f);
        }

        return Quaternion.Euler(-90f, 90f, 0f);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void HideAllPreviews()
    {
        if (previewCharacters == null)
        {
            return;
        }

        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] != null)
            {
                previewCharacters[i].SetActive(false);
            }
        }
    }
}
}
