using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CollectionGridScroller : UIBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Layout")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform placeholderTemplate;
    [SerializeField, Min(1)] private int columns = 3;
    [SerializeField, Min(0)] private int minimumCardCount = 0;
    [SerializeField] private Vector2 cardSpacing = new(40f, 36f);
    [SerializeField] private Vector2 contentPadding = new(24f, 24f);

    [Header("Scrolling")]
    [SerializeField] private Scrollbar verticalScrollbar;
    [SerializeField, Min(10f)] private float mouseWheelStep = 90f;
    [SerializeField, Min(10f)] private float keyboardStep = 120f;
    [SerializeField] private bool createScrollbarIfMissing = false;
    [SerializeField] private Vector2 scrollbarSize = new(20f, 0f);
    [SerializeField] private Vector2 scrollbarPadding = new(8f, 8f);
    [SerializeField] private bool autoSizeScrollbarHandle = true;
    [SerializeField, Range(0.05f, 1f)] private float fixedScrollbarHandleSize = 0.25f;
    [SerializeField, Min(24f)] private float generatedHandleHeight = 72f;
    [SerializeField] private Color generatedHandleColor = new(0.4627451f, 0.95686275f, 0.972549f, 0.95f);

    [Header("Placeholder Copy")]
    [SerializeField] private string placeholderTitle = "Coming Soon";
    [SerializeField] private string placeholderButtonText = "Soon";
    [SerializeField] private float placeholderPreviewAlpha = 0.4f;

    private readonly List<RectTransform> cards = new();
    private readonly List<Selectable> cardSelectables = new();
    private readonly List<GameObject> generatedCards = new();
    private float currentScrollY;
    private float maxScrollY;
    private bool ignoreScrollbarCallback;
    private bool isDragging;
    private Vector2 previousDragLocalPoint;

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureViewportComponents();
        EnsureScrollbar();
        WireScrollbar();
        RefreshLayout();
    }

    protected override void OnDisable()
    {
        UnwireScrollbar();
        base.OnDisable();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();

        if (!isActiveAndEnabled)
            return;

        RebuildGrid();
        EnsureSelectionVisible(immediate: true);
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        HandleKeyboardNavigation();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (maxScrollY <= 0f)
            return;

        SetScrollPosition(currentScrollY - eventData.scrollDelta.y * mouseWheelStep);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (maxScrollY <= 0f || viewport == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out previousDragLocalPoint))
            return;

        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || viewport == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        float deltaY = localPoint.y - previousDragLocalPoint.y;
        previousDragLocalPoint = localPoint;
        SetScrollPosition(currentScrollY - deltaY);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void WireScrollbar()
    {
        if (verticalScrollbar == null)
            return;

        verticalScrollbar.onValueChanged.RemoveListener(HandleScrollbarChanged);
        verticalScrollbar.onValueChanged.AddListener(HandleScrollbarChanged);
    }

    private void UnwireScrollbar()
    {
        if (verticalScrollbar == null)
            return;

        verticalScrollbar.onValueChanged.RemoveListener(HandleScrollbarChanged);
    }

    private void HandleScrollbarChanged(float value)
    {
        if (ignoreScrollbarCallback)
            return;

        SetScrollPosition(Mathf.Lerp(maxScrollY, 0f, value));
    }

    private void HandleKeyboardNavigation()
    {
        if (cards.Count == 0)
            return;

        int direction = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow))
            direction = -columns;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            direction = columns;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            direction = -1;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            direction = 1;

        if (direction == 0)
            return;

        int currentIndex = GetCurrentSelectedIndex();
        if (currentIndex < 0)
            currentIndex = 0;

        int targetIndex = Mathf.Clamp(currentIndex + direction, 0, cardSelectables.Count - 1);
        if (targetIndex == currentIndex && direction != 0)
            return;

        Selectable selectable = cardSelectables[targetIndex];
        if (selectable != null)
            EventSystem.current?.SetSelectedGameObject(selectable.gameObject);

        EnsureCardVisible(targetIndex);
    }

    private int GetCurrentSelectedIndex()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null)
            return -1;

        for (int i = 0; i < cardSelectables.Count; i++)
        {
            if (cardSelectables[i] != null && cardSelectables[i].gameObject == selected)
                return i;
        }

        return -1;
    }

    private void RebuildGrid()
    {
        if (viewport == null || content == null || placeholderTemplate == null)
            return;

        ConfigureContentRoot();
        DisableConflictingContentLayout();
        EnsureMinimumCards();
        CollectCards();
        LayoutCards();
        UpdateScrollbar();
    }

    public void RefreshLayout()
    {
        RebuildGrid();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SetScrollPosition(0f);
        EnsureSelectionVisible(immediate: true);
    }

    private void EnsureMinimumCards()
    {
        int existingCards = CountCardChildren();
        while (existingCards < minimumCardCount)
        {
            RectTransform clone = Instantiate(placeholderTemplate, content);
            clone.name = $"PlaceholderFrameStack_{existingCards + 1}";
            clone.SetSiblingIndex(0);
            PreparePlaceholderCard(clone);
            generatedCards.Add(clone.gameObject);
            existingCards++;
        }
    }

    private int CountCardChildren()
    {
        int count = 0;
        for (int i = 0; i < content.childCount; i++)
        {
            if (content.GetChild(i) is RectTransform rect && rect.gameObject.activeSelf)
                count++;
        }

        return count;
    }

    private void PreparePlaceholderCard(RectTransform card)
    {
        BlueEnvelopeUnlockCard unlockCard = card.GetComponent<BlueEnvelopeUnlockCard>();
        if (unlockCard != null)
            Destroy(unlockCard);

        TMP_Text title = FindText(card, "SkinTitle");
        if (title != null)
            title.text = placeholderTitle;

        TMP_Text cost = FindText(card, "SkinCost");
        if (cost != null)
            cost.gameObject.SetActive(false);

        Transform costRow = card.Find("SkinCostRow");
        if (costRow != null)
            costRow.gameObject.SetActive(false);

        Image icon = FindImage(card, "SkinCostIcon");
        if (icon != null)
            icon.gameObject.SetActive(false);

        Image preview = FindImage(card, "SkinPreview");
        if (preview != null)
        {
            Color color = preview.color;
            color.a = placeholderPreviewAlpha;
            preview.color = color;
        }

        Button button = card.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = true;

            TMP_Text buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
                buttonLabel.text = placeholderButtonText;
        }
    }

    private void CollectCards()
    {
        cards.Clear();
        cardSelectables.Clear();

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
                continue;

            cards.Add(child);

            Button button = child.GetComponentInChildren<Button>(true);
            if (button != null)
                cardSelectables.Add(button);
        }
    }

    private void LayoutCards()
    {
        if (cards.Count == 0)
            return;

        float maxCardWidth = 0f;
        float maxCardHeight = 0f;
        Vector2[] cardSizes = new Vector2[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            Vector2 cardSize = GetCardSize(cards[i]);
            cardSizes[i] = cardSize;
            maxCardWidth = Mathf.Max(maxCardWidth, cardSize.x);
            maxCardHeight = Mathf.Max(maxCardHeight, cardSize.y);
        }

        float stepX = maxCardWidth + cardSpacing.x;
        float stepY = maxCardHeight + cardSpacing.y;
        int rows = Mathf.CeilToInt(cards.Count / (float)columns);

        float contentWidth = contentPadding.x * 2f + columns * maxCardWidth + (columns - 1) * cardSpacing.x;
        float contentHeight = contentPadding.y * 2f + rows * maxCardHeight + Mathf.Max(0, rows - 1) * cardSpacing.y;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        for (int i = 0; i < cards.Count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            RectTransform card = cards[i];
            Vector2 cardSize = cardSizes[i];
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(0f, 1f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSize.x);
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSize.y);
            card.anchoredPosition = new Vector2(
                contentPadding.x + column * stepX + cardSize.x * 0.5f,
                -(contentPadding.y + row * stepY + cardSize.y * 0.5f));
        }

        maxScrollY = Mathf.Max(0f, contentHeight - viewport.rect.height);
        currentScrollY = Mathf.Clamp(currentScrollY, 0f, maxScrollY);
        ApplyScrollPosition();
    }

    private void SetScrollPosition(float value)
    {
        currentScrollY = Mathf.Clamp(value, 0f, maxScrollY);
        ApplyScrollPosition();
        UpdateScrollbar();
    }

    private void ApplyScrollPosition()
    {
        Vector2 anchoredPosition = content.anchoredPosition;
        anchoredPosition.x = 0f;
        anchoredPosition.y = currentScrollY;
        content.anchoredPosition = anchoredPosition;
    }

    private void ConfigureContentRoot()
    {
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.localScale = Vector3.one;
    }

    private static Vector2 GetCardSize(RectTransform card)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(card);

        float width = Mathf.Max(
            card.rect.width,
            LayoutUtility.GetPreferredWidth(card),
            LayoutUtility.GetMinWidth(card));

        float height = Mathf.Max(
            card.rect.height,
            LayoutUtility.GetPreferredHeight(card),
            LayoutUtility.GetMinHeight(card));

        if (width <= 0f)
            width = 200f;

        if (height <= 0f)
            height = 258f;

        return new Vector2(width, height);
    }

    private void UpdateScrollbar()
    {
        if (verticalScrollbar == null)
            return;

        float targetSize;
        if (autoSizeScrollbarHandle)
        {
            float viewportHeight = Mathf.Max(1f, viewport.rect.height);
            float contentHeight = Mathf.Max(viewportHeight, content.sizeDelta.y);
            targetSize = Mathf.Clamp01(viewportHeight / contentHeight);
        }
        else
        {
            targetSize = fixedScrollbarHandleSize;
        }

        verticalScrollbar.size = Mathf.Clamp01(targetSize);

        ignoreScrollbarCallback = true;
        verticalScrollbar.SetValueWithoutNotify(maxScrollY <= 0f ? 1f : Mathf.Lerp(1f, 0f, currentScrollY / maxScrollY));
        ignoreScrollbarCallback = false;
    }

    private void EnsureViewportComponents()
    {
        if (viewport == null)
            viewport = transform as RectTransform;
    }

    private void EnsureScrollbar()
    {
        if (verticalScrollbar != null || !createScrollbarIfMissing || viewport == null)
            return;
    }

    private void DisableConflictingContentLayout()
    {
        if (content == null)
            return;

        HorizontalLayoutGroup horizontalLayout = content.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayout != null)
            horizontalLayout.enabled = false;

        ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
            sizeFitter.enabled = false;
    }

    private void EnsureSelectionVisible(bool immediate)
    {
        int selectedIndex = GetCurrentSelectedIndex();
        if (selectedIndex < 0 && cardSelectables.Count > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(cardSelectables[0].gameObject);
            selectedIndex = 0;
        }

        if (selectedIndex >= 0)
            EnsureCardVisible(selectedIndex, immediate);
    }

    private void EnsureCardVisible(int index, bool immediate = false)
    {
        if (index < 0 || index >= cards.Count || viewport == null)
            return;

        RectTransform card = cards[index];
        float cardTop = -card.anchoredPosition.y - card.sizeDelta.y * 0.5f;
        float cardBottom = -card.anchoredPosition.y + card.sizeDelta.y * 0.5f;
        float viewportTop = currentScrollY;
        float viewportBottom = currentScrollY + viewport.rect.height;

        float targetScroll = currentScrollY;
        if (cardTop < viewportTop)
            targetScroll = cardTop;
        else if (cardBottom > viewportBottom)
            targetScroll = cardBottom - viewport.rect.height;

        if (immediate)
            SetScrollPosition(targetScroll);
        else if (!Mathf.Approximately(targetScroll, currentScrollY))
            SetScrollPosition(currentScrollY + Mathf.Sign(targetScroll - currentScrollY) * keyboardStep);
    }

    private static TMP_Text FindText(Component root, string childName)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == childName)
                return texts[i];
        }

        return null;
    }

    private static Image FindImage(Component root, string childName)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == childName)
                return images[i];
        }

        return null;
    }
}
