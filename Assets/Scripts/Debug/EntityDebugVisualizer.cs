using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EntityStats))]
public class EntityDebugVisualizer : MonoBehaviour
{
    private const float PlayerPanelWidth = 320f;
    private const float PlayerPanelHeight = 110f;

    [Header("Display Toggles")]
    [SerializeField]
    private bool showKnockbackThreshold = true;

    [SerializeField]
    private bool showComboWindow;

    [SerializeField]
    private bool showNpcAttackWindup;

    [Header("World Space Display")]
    [SerializeField, Min(0f)]
    private float heightOffset = 2.25f;

    [SerializeField, Min(1f)]
    private float displayWidth = 140f;

    [SerializeField, Min(1f)]
    private float barHeight = 12f;

    [SerializeField, Min(0.001f)]
    private float worldScale = 0.01f;

    private EntityStats stats;
    private PlayerController playerController;
    private NpcController npcController;
    private bool isPlayerScreenDisplay;
    private Camera targetCamera;
    private Canvas displayCanvas;
    private RectTransform displayRect;
    private GameObject playerScreenPanel;
    private RectTransform thresholdBar;
    private RawImage thresholdFill;
    private Text thresholdLabel;
    private Text comboLabel;
    private Text npcAttackLabel;
    private RectTransform attackWindupBar;
    private RawImage attackWindupFill;

    private void Awake()
    {
        stats = GetComponent<EntityStats>();
        playerController = GetComponent<PlayerController>();
        npcController = GetComponent<NpcController>();
        isPlayerScreenDisplay = playerController != null;
        CreateDisplay();
    }

    private void OnDestroy()
    {
        if (isPlayerScreenDisplay && displayCanvas != null)
            Destroy(displayCanvas.gameObject);
    }

    private void LateUpdate()
    {
        if (!isPlayerScreenDisplay)
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null && displayRect != null)
            {
                if (displayCanvas != null &&
                    displayCanvas.worldCamera != targetCamera)
                {
                    displayCanvas.worldCamera = targetCamera;
                }

                displayRect.rotation = targetCamera.transform.rotation;
            }
        }

        UpdateKnockbackDisplay();
        UpdateComboDisplay();
        UpdateNpcAttackWindupDisplay();

        if (isPlayerScreenDisplay && playerScreenPanel != null)
        {
            bool hasVisiblePlayerInfo =
                thresholdBar.gameObject.activeSelf ||
                comboLabel.gameObject.activeSelf;

            playerScreenPanel.SetActive(hasVisiblePlayerInfo);
        }
    }

    private void UpdateKnockbackDisplay()
    {
        bool thresholdIsEnabled =
            showKnockbackThreshold &&
            stats != null &&
            stats.MaxKnockbackThreshold > 0f;

        if (thresholdBar == null || thresholdFill == null)
            return;

        thresholdBar.gameObject.SetActive(thresholdIsEnabled);

        if (thresholdLabel != null)
            thresholdLabel.gameObject.SetActive(thresholdIsEnabled);

        if (!thresholdIsEnabled)
            return;

        float fill = stats.KnockbackThresholdFill;
        float barWidth = thresholdBar.sizeDelta.x;
        thresholdFill.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Max(0f, barWidth - 4f) * fill);
        thresholdFill.color = Color.Lerp(Color.red, Color.green, fill);

        if (thresholdLabel != null)
        {
            thresholdLabel.text =
                $"KNOCKBACK THRESHOLD  " +
                $"{stats.CurrentKnockbackThreshold:0.0} / " +
                $"{stats.MaxKnockbackThreshold:0.0}";
        }
    }

    private void UpdateComboDisplay()
    {
        if (comboLabel == null || playerController == null)
            return;

        bool comboWindowOpen = playerController.IsComboWindowOpen;
        bool comboInputBuffered = playerController.IsComboInputBuffered;
        bool shouldShow =
            showComboWindow &&
            (comboWindowOpen || comboInputBuffered);

        comboLabel.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        if (comboInputBuffered)
        {
            comboLabel.text = "COMBO BUFFERED";
            comboLabel.color = Color.green;
        }
        else
        {
            comboLabel.text = "COMBO WINDOW";
            comboLabel.color = Color.cyan;
        }
    }

    private void UpdateNpcAttackWindupDisplay()
    {
        if (npcController == null ||
            npcAttackLabel == null ||
            attackWindupBar == null ||
            attackWindupFill == null)
        {
            return;
        }

        bool isInStartup = false;
        float startupProgress = 0f;
        string attackLabel = "";

        NpcAction currentAction =
            npcController.Actions != null
                ? npcController.Actions.CurrentAction
                : null;

        if (currentAction is NpcMeleeAttackAction meleeAttack &&
            meleeAttack.IsInStartup)
        {
            isInStartup = true;
            startupProgress = meleeAttack.StartupProgress;
            attackLabel = "MELEE WINDUP";
        }
        else if (currentAction is NpcRangedAttackAction rangedAttack &&
                 rangedAttack.IsInStartup)
        {
            isInStartup = true;
            startupProgress = rangedAttack.StartupProgress;
            attackLabel = "RANGED WINDUP";
        }

        bool shouldShow = showNpcAttackWindup && isInStartup;
        npcAttackLabel.gameObject.SetActive(shouldShow);
        attackWindupBar.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        npcAttackLabel.text = attackLabel;

        float progress = Mathf.Clamp01(startupProgress);
        float remainingStartup = 1f - progress;

        attackWindupFill.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Max(0f, displayWidth - 4f) * remainingStartup);
        attackWindupFill.color = Color.Lerp(
            Color.yellow,
            Color.red,
            progress);
    }

    private void CreateDisplay()
    {
        GameObject canvasObject = new GameObject(
            "Entity Debug Visuals",
            typeof(RectTransform),
            typeof(Canvas));

        displayRect = canvasObject.GetComponent<RectTransform>();
        displayCanvas = canvasObject.GetComponent<Canvas>();

        if (isPlayerScreenDisplay)
        {
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            displayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            displayRect.anchorMin = Vector2.zero;
            displayRect.anchorMax = Vector2.one;
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;
            displayRect.localScale = Vector3.one;

            CreatePlayerScreenPanel();
            CreateComboLabel();
            CreateKnockbackBar();
            return;
        }

        canvasObject.transform.SetParent(transform, false);
        displayRect.localPosition = Vector3.up * heightOffset;
        displayRect.localRotation = Quaternion.identity;
        displayRect.localScale = Vector3.one * worldScale;
        displayRect.sizeDelta = new Vector2(displayWidth, 64f);

        displayCanvas.renderMode = RenderMode.WorldSpace;
        displayCanvas.worldCamera = Camera.main;
        displayCanvas.overrideSorting = true;
        displayCanvas.sortingOrder = 100;

        CreateComboLabel();
        CreateNpcAttackWindupDisplay();
        CreateKnockbackBar();
    }

    private void CreatePlayerScreenPanel()
    {
        GameObject panelObject = new GameObject(
            "Player Debug Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        panelObject.transform.SetParent(displayRect, false);
        playerScreenPanel = panelObject;

        RawImage background = panelObject.GetComponent<RawImage>();
        background.texture = Texture2D.whiteTexture;
        background.color = new Color(0f, 0f, 0f, 0.72f);
        background.raycastTarget = false;

        RectTransform panelRect = background.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta =
            new Vector2(PlayerPanelWidth, PlayerPanelHeight);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
    }

    private void CreateComboLabel()
    {
        GameObject labelObject = new GameObject(
            "Combo Debug Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        Transform labelParent = isPlayerScreenDisplay
            ? playerScreenPanel.transform
            : displayRect;
        labelObject.transform.SetParent(labelParent, false);

        comboLabel = labelObject.GetComponent<Text>();
        comboLabel.font =
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        comboLabel.fontSize = 16;
        comboLabel.alignment = TextAnchor.MiddleCenter;
        comboLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        comboLabel.verticalOverflow = VerticalWrapMode.Overflow;
        comboLabel.raycastTarget = false;
        comboLabel.text = "";
        comboLabel.gameObject.SetActive(false);

        RectTransform labelRect = comboLabel.rectTransform;
        if (isPlayerScreenDisplay)
        {
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = new Vector2(PlayerPanelWidth - 24f, 24f);
            labelRect.anchoredPosition = new Vector2(12f, -72f);
        }
        else
        {
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(displayWidth, 18f);
            labelRect.anchoredPosition = new Vector2(0f, 22f);
        }
    }

    private void CreateNpcAttackWindupDisplay()
    {
        GameObject labelObject = new GameObject(
            "NPC Attack Windup Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        labelObject.transform.SetParent(displayRect, false);

        npcAttackLabel = labelObject.GetComponent<Text>();
        npcAttackLabel.font =
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        npcAttackLabel.fontSize = 16;
        npcAttackLabel.alignment = TextAnchor.MiddleCenter;
        npcAttackLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        npcAttackLabel.verticalOverflow = VerticalWrapMode.Overflow;
        npcAttackLabel.color = Color.yellow;
        npcAttackLabel.raycastTarget = false;
        npcAttackLabel.text = "";
        npcAttackLabel.gameObject.SetActive(false);

        RectTransform labelRect = npcAttackLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(displayWidth, 18f);
        labelRect.anchoredPosition = new Vector2(0f, 5f);

        GameObject backgroundObject = new GameObject(
            "NPC Attack Windup Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        backgroundObject.transform.SetParent(displayRect, false);

        RawImage background = backgroundObject.GetComponent<RawImage>();
        background.texture = Texture2D.whiteTexture;
        background.color = Color.black;
        background.raycastTarget = false;

        attackWindupBar = background.rectTransform;
        attackWindupBar.anchorMin = new Vector2(0.5f, 0.5f);
        attackWindupBar.anchorMax = new Vector2(0.5f, 0.5f);
        attackWindupBar.pivot = new Vector2(0.5f, 0.5f);
        attackWindupBar.sizeDelta = new Vector2(displayWidth, 8f);
        attackWindupBar.anchoredPosition = new Vector2(0f, -8f);

        GameObject fillObject = new GameObject(
            "NPC Attack Windup Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        fillObject.transform.SetParent(attackWindupBar, false);

        attackWindupFill = fillObject.GetComponent<RawImage>();
        attackWindupFill.texture = Texture2D.whiteTexture;
        attackWindupFill.color = Color.yellow;
        attackWindupFill.raycastTarget = false;

        RectTransform fillRect = attackWindupFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(displayWidth - 4f, -4f);
        fillRect.anchoredPosition = new Vector2(2f, 0f);
    }

    private void CreateKnockbackBar()
    {
        Transform barParent = isPlayerScreenDisplay
            ? playerScreenPanel.transform
            : displayRect;

        GameObject backgroundObject = new GameObject(
            "Knockback Threshold Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        backgroundObject.transform.SetParent(barParent, false);

        RawImage background = backgroundObject.GetComponent<RawImage>();
        background.texture = Texture2D.whiteTexture;
        background.color = Color.black;
        background.raycastTarget = false;

        thresholdBar = background.rectTransform;
        float barWidth = isPlayerScreenDisplay
            ? PlayerPanelWidth - 24f
            : displayWidth;

        if (isPlayerScreenDisplay)
        {
            thresholdLabel = CreatePlayerText(
                "Knockback Threshold Label",
                "KNOCKBACK THRESHOLD",
                playerScreenPanel.transform,
                new Vector2(12f, -12f),
                new Vector2(barWidth, 20f));
        }

        thresholdBar.anchorMin = isPlayerScreenDisplay
            ? new Vector2(0f, 1f)
            : new Vector2(0.5f, 0.5f);
        thresholdBar.anchorMax = isPlayerScreenDisplay
            ? new Vector2(0f, 1f)
            : new Vector2(0.5f, 0.5f);
        thresholdBar.pivot = isPlayerScreenDisplay
            ? new Vector2(0f, 1f)
            : new Vector2(0.5f, 0.5f);
        thresholdBar.sizeDelta = new Vector2(barWidth, barHeight);
        thresholdBar.anchoredPosition = isPlayerScreenDisplay
            ? new Vector2(12f, -38f)
            : new Vector2(0f, -23f);

        GameObject fillObject = new GameObject(
            "Knockback Threshold Fill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        fillObject.transform.SetParent(thresholdBar, false);

        thresholdFill = fillObject.GetComponent<RawImage>();
        thresholdFill.texture = Texture2D.whiteTexture;
        thresholdFill.raycastTarget = false;

        RectTransform fillRect = thresholdFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(
            Mathf.Max(0f, barWidth - 4f) * stats.KnockbackThresholdFill,
            -4f);
        fillRect.anchoredPosition = new Vector2(2f, 0f);
    }

    private Text CreatePlayerText(
        string objectName,
        string initialText,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = initialText;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.sizeDelta = size;
        labelRect.anchoredPosition = anchoredPosition;

        return label;
    }
}
