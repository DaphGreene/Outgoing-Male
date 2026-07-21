using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class StyledMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("Graphic Colors")]
    [SerializeField] private Color normalGraphicColor = Color.white;
    [SerializeField] private Color highlightedGraphicColor = Color.white;
    [SerializeField] private Color pressedGraphicColor = new(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color disabledGraphicColor = new(1f, 1f, 1f, 0.45f);

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = new(0.96f, 0.96f, 0.94f, 1f);
    [SerializeField] private Color highlightedTextColor = new(0.96f, 0.96f, 0.94f, 1f);
    [SerializeField] private Color pressedTextColor = new(0.90f, 0.90f, 0.88f, 1f);
    [SerializeField] private Color disabledTextColor = new(0.60f, 0.60f, 0.58f, 0.70f);

    [Header("Feedback")]
    [SerializeField] private Color highlightedOutlineColor = new(0.4627451f, 0.95686275f, 0.972549f, 0.95f);
    [SerializeField] private Color pressedOutlineColor = new(1f, 0.427451f, 0.882353f, 0.95f);
    [SerializeField] private Vector2 outlineDistance = new(2f, 2f);
    [SerializeField] private Vector2 pressedTextOffset = new(0f, -2f);

    private Button button;
    private Graphic targetGraphic;
    private Outline graphicOutline;
    private TMP_Text buttonText;
    private RectTransform buttonTextRect;
    private Vector2 originalTextAnchoredPosition;
    private bool hasCachedTextPosition;
    private bool isPointerOver;
    private bool isPressed;
    private bool isSelected;

    private void Awake()
    {
        CacheReferences();
        UpdateVisualState();
    }

    private void OnEnable()
    {
        CacheReferences();
        UpdateVisualState();
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isPressed = false;
        isSelected = false;
        UpdateVisualState();
    }

    public void RefreshVisualState()
    {
        UpdateVisualState();
    }

    public void SetDisabledColors(Color graphicColor, Color textColor)
    {
        disabledGraphicColor = graphicColor;
        disabledTextColor = textColor;
    }

    public void CaptureCurrentTextPosition()
    {
        CacheReferences();

        if (buttonTextRect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        originalTextAnchoredPosition = buttonTextRect.anchoredPosition;
        hasCachedTextPosition = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        UpdateVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        isPressed = false;
        UpdateVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.IsInteractable())
        {
            isPressed = true;
            UpdateVisualState();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateVisualState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        UpdateVisualState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        isPressed = false;
        UpdateVisualState();
    }

    private void CacheReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (targetGraphic == null && button != null)
            targetGraphic = button.targetGraphic;

        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
        {
            graphicOutline = targetGraphic.GetComponent<Outline>();
            if (graphicOutline == null)
                graphicOutline = targetGraphic.gameObject.AddComponent<Outline>();

            graphicOutline.enabled = false;
            graphicOutline.effectDistance = outlineDistance;
            graphicOutline.useGraphicAlpha = true;
        }

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null && buttonTextRect == null)
        {
            buttonTextRect = buttonText.rectTransform;
            originalTextAnchoredPosition = buttonTextRect.anchoredPosition;
            hasCachedTextPosition = true;
        }
    }

    private void UpdateVisualState()
    {
        CacheReferences();

        bool interactable = button != null && button.IsInteractable();
        bool shouldHighlight = interactable && (isPointerOver || isSelected);

        if (!interactable)
        {
            ApplyColors(disabledGraphicColor, disabledTextColor);
            ApplyOutline(false, highlightedOutlineColor);
            ApplyTextOffset(Vector2.zero);
            return;
        }

        if (isPressed)
        {
            ApplyColors(pressedGraphicColor, pressedTextColor);
            ApplyOutline(true, pressedOutlineColor);
            ApplyTextOffset(pressedTextOffset);
            return;
        }

        if (shouldHighlight)
        {
            ApplyColors(highlightedGraphicColor, highlightedTextColor);
            ApplyOutline(true, highlightedOutlineColor);
            ApplyTextOffset(Vector2.zero);
            return;
        }

        ApplyColors(normalGraphicColor, normalTextColor);
        ApplyOutline(false, highlightedOutlineColor);
        ApplyTextOffset(Vector2.zero);
    }

    private void ApplyColors(Color graphicColor, Color textColor)
    {
        if (targetGraphic != null)
            targetGraphic.color = graphicColor;

        if (buttonText != null)
            buttonText.color = textColor;
    }

    private void ApplyOutline(bool enabled, Color outlineColor)
    {
        if (graphicOutline == null)
            return;

        graphicOutline.effectColor = outlineColor;
        graphicOutline.effectDistance = outlineDistance;
        graphicOutline.enabled = enabled;
    }

    private void ApplyTextOffset(Vector2 offset)
    {
        if (!hasCachedTextPosition || buttonTextRect == null)
            return;

        buttonTextRect.anchoredPosition = originalTextAnchoredPosition + offset;
    }
}
