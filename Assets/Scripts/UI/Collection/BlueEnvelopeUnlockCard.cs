using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BlueEnvelopeUnlockCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Skin")]
    [SerializeField] private string skinId = PlayerSkinState.BlueEnvelopeSkinId;
    [SerializeField] private string skinTitle = "Blue Envelope";
    [SerializeField] private int unlockCost = 10;
    [SerializeField] private Sprite[] skinSprites;

    [Header("Scene References")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image costIconImage;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private StyledMenuButton styledActionButton;

    [Header("Behavior")]
    [SerializeField] private float lockedPreviewAlpha = 0.55f;
    [SerializeField] private float hoverFrameRate = 8f;
    [SerializeField] private int idlePreviewFrameIndex;
    [SerializeField] private Color disabledButtonGraphicColor = new(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color disabledButtonTextColor = new(0.72f, 0.72f, 0.7f, 1f);
    [SerializeField] private HorizontalLayoutGroup actionButtonLayoutGroup;

    private static BlueEnvelopeUnlockCard hoveredCard;
    private bool isHovered;
    private int lastPreviewFrame = -1;

    private void Awake()
    {
        BindMissingReferences();
        RefreshVisualState();
    }

    private void OnEnable()
    {
        BindMissingReferences();

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionPressed);
            actionButton.onClick.AddListener(HandleActionPressed);
        }

        StampBank.OnStampCountChanged += HandleStampCountChanged;
        PlayerSkinState.OnSkinStateChanged += RefreshVisualState;
        RefreshVisualState();
    }

    private void OnDisable()
    {
        StampBank.OnStampCountChanged -= HandleStampCountChanged;
        PlayerSkinState.OnSkinStateChanged -= RefreshVisualState;

        if (hoveredCard == this)
            hoveredCard = null;

        if (actionButton != null)
            actionButton.onClick.RemoveListener(HandleActionPressed);
    }

    private void OnValidate()
    {
        BindMissingReferences();
    }

    private void Update()
    {
        if (previewImage == null || skinSprites == null || skinSprites.Length == 0)
            return;

        bool isSelected = PlayerSkinState.SelectedSkinId == skinId;
        bool shouldAnimate = hoveredCard != null ? hoveredCard == this : isSelected;

        int targetFrame = 0;
        if (shouldAnimate)
            targetFrame = Mathf.FloorToInt(Time.unscaledTime * hoverFrameRate) % skinSprites.Length;
        else
            targetFrame = GetClampedIdlePreviewFrameIndex();

        if (targetFrame == lastPreviewFrame)
            return;

        lastPreviewFrame = targetFrame;
        previewImage.sprite = skinSprites[targetFrame];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoveredCard = this;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoveredCard == this)
            hoveredCard = null;
        isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleActionPressed();
    }

    public void HandleActionPressed()
    {
        if (!PlayerSkinState.IsUnlocked(skinId))
        {
            if (!PlayerSkinState.TryUnlock(skinId, unlockCost))
                return;
        }
        else
        {
            PlayerSkinState.SelectSkin(skinId);
        }

        RefreshVisualState();
    }

    private void HandleStampCountChanged(int stampCount)
    {
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        BindMissingReferences();

        bool isUnlocked = PlayerSkinState.IsUnlocked(skinId);
        bool isSelected = PlayerSkinState.SelectedSkinId == skinId;
        bool canAfford = unlockCost <= 0 || StampBank.Count >= unlockCost;

        if (previewImage != null)
        {
            Color color = previewImage.color;
            color.a = isUnlocked ? 1f : lockedPreviewAlpha;
            previewImage.color = color;

            if (skinSprites != null && skinSprites.Length > 0)
                previewImage.sprite = skinSprites[GetClampedIdlePreviewFrameIndex()];
        }

        if (titleText != null)
            titleText.text = skinTitle;

        if (costText != null)
            costText.gameObject.SetActive(false);

        if (costIconImage != null)
        {
            bool showCostIcon = !isUnlocked && unlockCost > 0;
            costIconImage.gameObject.SetActive(showCostIcon);

            Color color = costIconImage.color;
            color.a = showCostIcon ? 1f : 0f;
            costIconImage.color = color;
        }

        if (actionButtonText != null)
        {
            if (!isUnlocked)
                actionButtonText.text = unlockCost > 0 ? $"{unlockCost} x" : "Unlock";
            else if (isSelected)
                actionButtonText.text = "Equipped";
            else
                actionButtonText.text = "Equip";
        }

        if (actionButton != null)
            actionButton.interactable = isUnlocked || canAfford;

        if (actionButtonLayoutGroup != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(actionButtonLayoutGroup.transform as RectTransform);
        else if (actionButton != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(actionButton.transform as RectTransform);

        if (styledActionButton != null)
        {
            styledActionButton.SetDisabledColors(disabledButtonGraphicColor, disabledButtonTextColor);
            styledActionButton.CaptureCurrentTextPosition();
            styledActionButton.RefreshVisualState();
        }
    }

    private void BindMissingReferences()
    {
        if (previewImage == null)
            previewImage = FindImageChild("SkinPreview");

        if (titleText == null)
            titleText = FindTextChild("SkinTitle");

        if (costText == null)
            costText = FindTextChild("SkinCost");

        if (costIconImage == null)
            costIconImage = FindImageChild("SkinCostIcon");

        if (actionButton == null)
            actionButton = FindButtonInSiblingRow();

        if (styledActionButton == null && actionButton != null)
            styledActionButton = actionButton.GetComponent<StyledMenuButton>();

        if (actionButtonLayoutGroup == null && actionButton != null)
            actionButtonLayoutGroup = actionButton.GetComponent<HorizontalLayoutGroup>();

        if (actionButtonText == null && actionButton != null)
            actionButtonText = actionButton.GetComponentInChildren<TMP_Text>(true);
    }

    private Image FindImageChild(string childName)
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == childName)
                return images[i];
        }

        return null;
    }

    private TMP_Text FindTextChild(string childName)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == childName)
                return texts[i];
        }

        return null;
    }

    private Button FindButtonInSiblingRow()
    {
        Transform frameRow = transform.parent;
        Transform buttonRow = frameRow != null ? frameRow.parent?.Find("ButtonRow") : null;
        if (buttonRow == null)
            return null;

        int siblingIndex = transform.GetSiblingIndex();
        if (siblingIndex < 0 || siblingIndex >= buttonRow.childCount)
            return null;

        return buttonRow.GetChild(siblingIndex).GetComponent<Button>();
    }

    private int GetClampedIdlePreviewFrameIndex()
    {
        if (skinSprites == null || skinSprites.Length == 0)
            return 0;

        return Mathf.Clamp(idlePreviewFrameIndex, 0, skinSprites.Length - 1);
    }
}
