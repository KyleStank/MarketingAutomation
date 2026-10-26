using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration.Marketing;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

internal sealed class ProductListView
{
    private const float RowStride = 64f;

    private readonly GUI_Main _menuManager;

    private readonly ProductSelection _selection = new();
    private readonly List<MarketingProduct> _visibleProducts = [];
    private readonly List<ProductRow> _pooledRows = [];
    private List<MarketingProduct> _products = [];

    private InputField _searchInput;
    private Dropdown _sortDropdown;
    private ScrollRect _scroll;
    private RectTransform _content;
    private Text _selectionSummary;
    private Text _emptyMessage;
    private Button _selectAllButton;
    private GameObject _selectAllCheckmark;

    private textScript _texts;
    private sfxScript _soundEffects;

    private bool _scrollResetPending;
    private int _scrollResetRequestedFrame;

    internal ProductListView(
        RectTransform parent,
        GUI_Main manager,
        NativeMarketingSelectorUi selector,
        Text textStyle)
    {
        _menuManager = manager;

        BuildSearch(parent);
        BuildScroll(parent, selector);
        BuildSortDropdown(parent, selector);
        BuildSummaryLabels(parent, textStyle);

        var ui = new NativeGameUi(manager);
        var checkboxStyle = NativeUiLookup.RequireComponent<RectTransform>(
            ui.GameMarketingPanel.transform, "WindowMain/ToggleAuto/Background");
        BuildSelectAll(parent, checkboxStyle, textStyle);
        BuildRowPool(selector, checkboxStyle);

        _scroll.onValueChanged.AddListener(_ => RenderRows());
        _searchInput.onValueChanged.AddListener(_ => RefreshFilteredProducts());
    }

    private void BuildSearch(RectTransform window)
    {
        // Menu_Arcadepreise is the native arcade-pricing menu; reuse only its search-field visuals.
        var donor = _menuManager.GetComponentInChildren<Menu_Arcadepreise>(true);
        if (!donor)
        {
            throw new InvalidOperationException("Native arcade-price search template is unavailable.");
        }

        var arcadeUi = new NativeArcadePricingUi(donor);
        var source = arcadeUi.SearchField;

        var root = NativeVisualTree.Copy(source.transform, window, _menuManager);
        root.name = "SearchProducts";
        UiFactory.Place(root, new Vector2(-120f, -96f), new Vector2(330f, 32f));

        _searchInput = root.gameObject.AddComponent<InputField>();
        _searchInput.targetGraphic = root.GetComponent<Image>();
        _searchInput.textComponent = NativeUiLookup.RequireComponent<Text>(root, "Text");
        _searchInput.placeholder = NativeUiLookup.RequireComponent<Text>(root, "Placeholder");
        ((Text)_searchInput.placeholder).text = "Search games and consoles...";
        _searchInput.textComponent.text = "";

        _searchInput.colors = source.colors;
        _searchInput.selectionColor = source.selectionColor;
        _searchInput.caretBlinkRate = source.caretBlinkRate;
        _searchInput.caretWidth = source.caretWidth;
        _searchInput.contentType = InputField.ContentType.Standard;
        _searchInput.lineType = InputField.LineType.SingleLine;
        _searchInput.characterLimit = 0;
        _searchInput.navigation = new Navigation { mode = Navigation.Mode.None };

        var focus = root.gameObject.AddComponent<SearchInputFocus>();
        focus.MenuManager = _menuManager;
        focus.Input = _searchInput;
    }

    private void BuildScroll(RectTransform window, NativeMarketingSelectorUi selector)
    {
        var source = selector.ProductScroll;

        var root = NativeVisualTree.Copy(source.transform, window, _menuManager,
            child => child.parent != source.content);
        root.name = "ProductScroll";
        UiFactory.Place(root, new Vector2(0f, -397f), new Vector2(588f, 456f));

        _scroll = root.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = NativeUiLookup.RequireComponent<RectTransform>(root, "Viewport");

        _content = NativeUiLookup.RequireComponent<RectTransform>(root, "Viewport/Content");
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(-28f, 456f);

        _scroll.content = _content;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.inertia = source.inertia;
        _scroll.decelerationRate = source.decelerationRate;
        _scroll.scrollSensitivity = source.scrollSensitivity;

        var scrollbarRoot = NativeUiLookup.RequireComponent<RectTransform>(root, "Scrollbar Vertical");
        var scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = NativeUiLookup.RequireComponent<RectTransform>(scrollbarRoot, "Sliding Area/Handle");
        scrollbar.targetGraphic = scrollbar.handleRect.GetComponent<Image>();
        scrollbar.direction = source.verticalScrollbar.direction;
        scrollbar.colors = source.verticalScrollbar.colors;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

        _scroll.verticalScrollbar = scrollbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
    }

    private void BuildSortDropdown(RectTransform parent, NativeMarketingSelectorUi selector)
    {
        _sortDropdown = DropdownFactory.Create(selector.SortDropdown, parent,
            _menuManager, "ProductListSort", ProductSortOptions.Labels());
        UiFactory.Place((RectTransform)_sortDropdown.transform, new Vector2(175f, -96f), new Vector2(230f, 32f));
        _sortDropdown.onValueChanged.AddListener(_ => RefreshFilteredProducts());
    }

    private void BuildSummaryLabels(RectTransform parent, Text textStyle)
    {
        _selectionSummary = UiFactory.CreateText(
            "SelectionSummary", parent, textStyle, "",
            new Vector2(0f, -644f), new Vector2(560f, 28f));
        _selectionSummary.fontSize = _selectionSummary.resizeTextMaxSize = 16;
        _selectionSummary.supportRichText = false;

        _emptyMessage = UiFactory.CreateText(
            "Empty", _scroll.viewport, textStyle, "",
            new Vector2(0f, -70f), new Vector2(510f, 90f));
        _emptyMessage.supportRichText = false;
    }

    private void BuildSelectAll(RectTransform window, RectTransform checkboxStyle, Text textStyle)
    {
        var root = UiFactory.CreateRect("SelectAll", window);
        UiFactory.Place(root, new Vector2(0f, -140f), new Vector2(570f, 40f));
        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;

        var checkbox = NativeVisualTree.Copy(checkboxStyle, root, _menuManager);
        checkbox.anchorMin = checkbox.anchorMax = new Vector2(0f, 0.5f);
        checkbox.anchoredPosition = new Vector2(32f, 0f);
        // Keep the native 32px background and 29px mark, matching normal game controls.
        _selectAllCheckmark = checkbox.Find("Checkmark").gameObject;
        _selectAllCheckmark.SetActive(false);
        foreach (var image in checkbox.GetComponentsInChildren<Image>(true))
        {
            image.raycastTarget = false;
        }

        var label = UiFactory.CreateText(
            "Label", root, textStyle, "Select all products",
            Vector2.zero, new Vector2(450f, 32f));
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        label.rectTransform.pivot = new Vector2(0f, 0.5f);
        label.rectTransform.anchoredPosition = new Vector2(60f, 0f);
        label.alignment = TextAnchor.MiddleLeft;
        label.resizeTextMaxSize = 16;

        _selectAllButton = root.gameObject.AddComponent<Button>();
        _selectAllButton.targetGraphic = checkbox.GetComponent<Image>();
        _selectAllButton.colors = checkboxStyle.GetComponentInParent<Toggle>(true).colors;
        _selectAllButton.navigation = new Navigation { mode = Navigation.Mode.None };
        _selectAllButton.onClick.AddListener(ToggleAllProducts);
    }

    private void ToggleAllProducts()
    {
        // Deliberately use the complete catalog, including search-hidden rows.
        _selection.ToggleAll(GetAvailableKeys());
        _soundEffects?.PlaySound(3, true);
        RefreshSummary();
        RenderRows();
    }

    private void BuildRowPool(NativeMarketingSelectorUi selector, RectTransform checkboxStyle)
    {
        // Two extra rows cover the partially visible rows at the viewport edges.
        var rowStyle = selector.ProductRowPrefab;
        var poolSize = Mathf.CeilToInt(_scroll.viewport.rect.height / RowStride) + 2;
        for (var index = 0; index < poolSize; index++)
        {
            _pooledRows.Add(new ProductRow(rowStyle, checkboxStyle, _content, _menuManager, ToggleProduct));
        }
    }

    internal int ProductCount => _products.Count;

    internal int SelectedCount => _selection.CountSelected(GetAvailableKeys());

    internal void Show(mainScript company, sfxScript soundEffects)
    {
        _texts = company.tS_;
        _soundEffects = soundEffects;

        _selection.Clear();
        _products = MarketingProduct.ReadCatalog(company.myID);

        _searchInput.SetTextWithoutNotify("");
        _sortDropdown.SetValueWithoutNotify(ProductSortOptions.IndexOf(QueuePriority.Name));
        RefreshFilteredProducts();
    }

    internal void RefreshFilteredProducts()
    {
        _visibleProducts.Clear();
        foreach (var product in _products)
        {
            if (product.Exists && ProductSelection.MatchesSearch(product.SearchName, _searchInput.text))
            {
                _visibleProducts.Add(product);
            }
        }

        SortVisibleProducts();

        _content.sizeDelta = new Vector2(
            _content.sizeDelta.x,
            Mathf.Max(_scroll.viewport.rect.height, _visibleProducts.Count * RowStride));

        ResetScrollToTop();
        _scrollResetPending = true;
        _scrollResetRequestedFrame = Time.frameCount;

        RefreshSummary();
        RenderRows();
    }

    private void SortVisibleProducts()
    {
        // Read live values once per refresh so comparisons use one consistent snapshot.
        var sortValues = new Dictionary<ProductKey, ProductSortValue>(_visibleProducts.Count);
        foreach (var product in _visibleProducts)
        {
            sortValues.Add(product.Key, new ProductSortValue(
                product.Key, product.SortName, product.Hype, product.NativeReleaseSortKey));
        }

        var field = ProductSortOptions.FromIndex(_sortDropdown.value);
        _visibleProducts.Sort((left, right) =>
            ProductListSorting.Compare(sortValues[left.Key], sortValues[right.Key], field));
    }

    private void ResetScrollToTop()
    {
        _scroll.StopMovement();
        _content.anchoredPosition = Vector2.zero;
        if (_scroll.verticalScrollbar)
        {
            _scroll.verticalScrollbar.SetValueWithoutNotify(1f);
        }
    }

    private void ToggleProduct(MarketingProduct product)
    {
        if (!product.Exists)
        {
            return;
        }

        _selection.Toggle(product.Key);
        _soundEffects?.PlaySound(3, true);
        RefreshSummary();
        RenderRows();
    }

    private void RefreshSummary()
    {
        var keys = GetAvailableKeys();
        var count = _selection.CountSelected(keys);

        _selectAllButton.interactable = keys.Count > 0;
        _selectAllCheckmark.SetActive(keys.Count > 0 && count == keys.Count);
        _selectionSummary.text = $"{count} selected   |   {_visibleProducts.Count} of {_products.Count} products shown";

        _emptyMessage.text = _products.Count == 0
            ? "No products are available for marketing."
            : "No products match your search.";
        _emptyMessage.gameObject.SetActive(_visibleProducts.Count == 0);
    }

    private List<ProductKey> GetAvailableKeys()
    {
        var keys = new List<ProductKey>(_products.Count);
        foreach (var product in _products)
        {
            if (product.Exists)
            {
                keys.Add(product.Key);
            }
        }

        return keys;
    }

    private void RenderRows()
    {
        if (_selection == null)
        {
            return;
        }

        var first = Mathf.Max(0, Mathf.FloorToInt(_content.anchoredPosition.y / RowStride));
        first = Mathf.Min(first, Mathf.Max(0, _visibleProducts.Count - _pooledRows.Count));

        for (var index = 0; index < _pooledRows.Count; index++)
        {
            var productIndex = first + index;
            var product = productIndex < _visibleProducts.Count ? _visibleProducts[productIndex] : null;
            _pooledRows[index].Bind(product, productIndex * RowStride, _selection, _texts);
        }
    }

    internal List<MarketingProduct> GetSelectedProducts()
    {
        var selected = new List<MarketingProduct>();
        foreach (var product in _products)
        {
            if (product.Exists && _selection.Contains(product.Key))
            {
                selected.Add(product);
            }
        }

        return selected;
    }

    internal void ResetDraft()
    {
        _selection.Clear();
        _scrollResetPending = false;
    }

    internal void ApplyPendingScrollReset()
    {
        if (_scrollResetPending && Time.frameCount > _scrollResetRequestedFrame &&
            _scroll && _scroll.isActiveAndEnabled && Mathf.Abs(_content.lossyScale.y) > 0.0001f)
        {
            // Opening animations can start at zero scale. Normalized positioning
            // before activation/layout uses collapsed bounds and offsets the list.
            Canvas.ForceUpdateCanvases();
            ResetScrollToTop();
            _scroll.verticalNormalizedPosition = 1f;
            _scrollResetPending = false;

            RenderRows();
        }
    }
}
