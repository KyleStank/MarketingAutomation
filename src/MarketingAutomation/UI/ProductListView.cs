using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

internal sealed class ProductListView
{
    private const float RowStride = 64f;
    private readonly GUI_Main menuManager;
    private readonly ProductSelection selection = new();
    private readonly List<MarketingProduct> visibleProducts = new();
    private readonly List<ProductRow> pooledRows = new();
    private List<MarketingProduct> products = new();
    private InputField searchInput;
    private Dropdown sortDropdown;
    private ScrollRect scroll;
    private RectTransform content;
    private Text selectionSummary;
    private Text emptyMessage;
    private Button selectAllButton;
    private GameObject selectAllCheckmark;
    private textScript texts;
    private sfxScript soundEffects;
    private bool resetScrollAfterLayout;
    private int scrollResetRequestedFrame;

    internal ProductListView(
        RectTransform parent,
        GUI_Main manager,
        Menu_Marketing_SelectGame selector,
        Text textStyle)
    {
        menuManager = manager;
        BuildSearch(parent);
        BuildScroll(parent, selector);
        sortDropdown = NativeDropdown.Create(selector.uiObjects[1].GetComponent<Dropdown>(), parent,
            manager, "ProductListSort", ProductSortOptions.Labels());
        UiFactory.Place((RectTransform)sortDropdown.transform, new Vector2(175f, -96f), new Vector2(230f, 32f));
        sortDropdown.onValueChanged.AddListener(_ => Filter());

        selectionSummary = UiFactory.CreateText(
            "SelectionSummary", parent, textStyle, "",
            new Vector2(0f, -644f), new Vector2(560f, 28f));
        selectionSummary.fontSize = selectionSummary.resizeTextMaxSize = 16;
        selectionSummary.supportRichText = false;
        emptyMessage = UiFactory.CreateText(
            "Empty", scroll.viewport, textStyle, "",
            new Vector2(0f, -70f), new Vector2(510f, 90f));
        emptyMessage.supportRichText = false;

        var checkboxStyle = NativeUi.Require<RectTransform>(
            manager.uiObjects[89].transform, "WindowMain/ToggleAuto/Background");
        BuildSelectAll(parent, checkboxStyle, textStyle);
        var rowStyle = selector.uiPrefabs[0];
        var poolSize = Mathf.CeilToInt(scroll.viewport.rect.height / RowStride) + 2;
        for (var index = 0; index < poolSize; index++)
        {
            pooledRows.Add(new ProductRow(rowStyle, checkboxStyle, content, manager, ToggleProduct));
        }

        scroll.onValueChanged.AddListener(_ => RenderRows());
        searchInput.onValueChanged.AddListener(_ => Filter());
    }

    internal int ProductCount => products.Count;

    internal int SelectedCount => selection.CountSelected(GetAvailableKeys());

    internal void Show(mainScript company, sfxScript soundEffects)
    {
        texts = company.tS_;
        this.soundEffects = soundEffects;
        selection.Clear();
        products = MarketingProduct.ReadCatalog(company.myID);
        searchInput.SetTextWithoutNotify("");
        sortDropdown.SetValueWithoutNotify(ProductSortOptions.IndexOf(QueuePriority.Name));
        Filter();
    }

    internal List<MarketingProduct> GetSelectedProducts()
    {
        var selected = new List<MarketingProduct>();
        foreach (var product in products)
        {
            if (product.Exists && selection.Contains(product.Key))
            {
                selected.Add(product);
            }
        }
        return selected;
    }

    internal void ResetDraft()
    {
        selection.Clear();
        resetScrollAfterLayout = false;
    }

    internal void Refresh() => Filter();

    internal void UpdateLayout()
    {
        if (resetScrollAfterLayout && Time.frameCount > scrollResetRequestedFrame &&
            scroll && scroll.isActiveAndEnabled && Mathf.Abs(content.lossyScale.y) > 0.0001f)
        {
            // Opening animations can start at zero scale. Normalized positioning
            // before activation/layout uses collapsed bounds and offsets the list.
            Canvas.ForceUpdateCanvases();
            ResetScrollToTop();
            scroll.verticalNormalizedPosition = 1f;
            resetScrollAfterLayout = false;
            RenderRows();
        }
    }

    private void BuildSelectAll(RectTransform window, RectTransform checkboxStyle, Text textStyle)
    {
        var root = UiFactory.CreateRect("SelectAll", window);
        UiFactory.Place(root, new Vector2(0f, -140f), new Vector2(570f, 40f));
        var hitArea = root.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        var checkbox = NativeUi.CopyTree(checkboxStyle, root, menuManager);
        checkbox.anchorMin = checkbox.anchorMax = new Vector2(0f, 0.5f);
        checkbox.anchoredPosition = new Vector2(32f, 0f);
        // Keep the native 32px background and 29px mark, matching normal game controls.
        selectAllCheckmark = checkbox.Find("Checkmark").gameObject;
        selectAllCheckmark.SetActive(false);
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
        selectAllButton = root.gameObject.AddComponent<Button>();
        selectAllButton.targetGraphic = checkbox.GetComponent<Image>();
        selectAllButton.colors = checkboxStyle.GetComponentInParent<Toggle>(true).colors;
        selectAllButton.navigation = new Navigation { mode = Navigation.Mode.None };
        selectAllButton.onClick.AddListener(() =>
        {
            // Deliberately use the complete catalog, including search-hidden rows.
            selection.ToggleAll(GetAvailableKeys());
            soundEffects?.PlaySound(3, true);
            RefreshSummary();
            RenderRows();
        });
    }

    private void BuildSearch(RectTransform window)
    {
        var donor = menuManager.GetComponentInChildren<Menu_Arcadepreise>(true);
        if (!donor)
        {
            throw new InvalidOperationException("Native arcade-price search template is unavailable.");
        }

        var source = donor.uiObjects[6].GetComponent<InputField>();
        var root = NativeUi.CopyTree(source.transform, window, menuManager);
        root.name = "SearchProducts";
        UiFactory.Place(root, new Vector2(-120f, -96f), new Vector2(330f, 32f));
        searchInput = root.gameObject.AddComponent<InputField>();
        searchInput.targetGraphic = root.GetComponent<Image>();
        searchInput.textComponent = NativeUi.Require<Text>(root, "Text");
        searchInput.placeholder = NativeUi.Require<Text>(root, "Placeholder");
        ((Text)searchInput.placeholder).text = "Search games and consoles...";
        searchInput.textComponent.text = "";
        searchInput.colors = source.colors;
        searchInput.selectionColor = source.selectionColor;
        searchInput.caretBlinkRate = source.caretBlinkRate;
        searchInput.caretWidth = source.caretWidth;
        searchInput.contentType = InputField.ContentType.Standard;
        searchInput.lineType = InputField.LineType.SingleLine;
        searchInput.characterLimit = 0;
        searchInput.navigation = new Navigation { mode = Navigation.Mode.None };
        var focus = root.gameObject.AddComponent<SearchInputFocus>();
        focus.MenuManager = menuManager;
        focus.Input = searchInput;
    }

    private void BuildScroll(RectTransform window, Menu_Marketing_SelectGame selector)
    {
        var source = selector.uiObjects[0].GetComponentInParent<ScrollRect>(true);
        var root = NativeUi.CopyTree(source.transform, window, menuManager,
            child => child.parent != source.content);
        root.name = "ProductScroll";
        UiFactory.Place(root, new Vector2(0f, -397f), new Vector2(588f, 456f));
        scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = NativeUi.Require<RectTransform>(root, "Viewport");
        content = NativeUi.Require<RectTransform>(root, "Viewport/Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(-28f, 456f);
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = source.inertia;
        scroll.decelerationRate = source.decelerationRate;
        scroll.scrollSensitivity = source.scrollSensitivity;
        var scrollbarRoot = NativeUi.Require<RectTransform>(root, "Scrollbar Vertical");
        var scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = NativeUi.Require<RectTransform>(scrollbarRoot, "Sliding Area/Handle");
        scrollbar.targetGraphic = scrollbar.handleRect.GetComponent<Image>();
        scrollbar.direction = source.verticalScrollbar.direction;
        scrollbar.colors = source.verticalScrollbar.colors;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
    }

    private void Filter()
    {
        visibleProducts.Clear();
        foreach (var product in products)
        {
            if (product.Exists && ProductSelection.MatchesSearch(product.SearchName, searchInput.text))
            {
                visibleProducts.Add(product);
            }
        }
        var sortValues = new Dictionary<ProductKey, ProductSortValue>(visibleProducts.Count);
        foreach (var product in visibleProducts)
        {
            sortValues.Add(product.Key, new ProductSortValue(
                product.Key, product.SortName, product.Hype, product.NativeReleaseSortKey));
        }

        var field = ProductSortOptions.FromIndex(sortDropdown.value);
        visibleProducts.Sort((left, right) => ProductListSorting.Compare(sortValues[left.Key], sortValues[right.Key], field));
        content.sizeDelta = new Vector2(
            content.sizeDelta.x,
            Mathf.Max(scroll.viewport.rect.height, visibleProducts.Count * RowStride));
        ResetScrollToTop();
        resetScrollAfterLayout = true;
        scrollResetRequestedFrame = Time.frameCount;
        RefreshSummary();
        RenderRows();
    }

    private void ResetScrollToTop()
    {
        scroll.StopMovement();
        content.anchoredPosition = Vector2.zero;
        if (scroll.verticalScrollbar)
        {
            scroll.verticalScrollbar.SetValueWithoutNotify(1f);
        }
    }

    private void ToggleProduct(MarketingProduct product)
    {
        if (!product.Exists)
        {
            return;
        }

        selection.Toggle(product.Key);
        soundEffects?.PlaySound(3, true);
        RefreshSummary();
        RenderRows();
    }

    private void RefreshSummary()
    {
        var keys = GetAvailableKeys();
        var count = selection.CountSelected(keys);
        selectAllButton.interactable = keys.Count > 0;
        selectAllCheckmark.SetActive(keys.Count > 0 && count == keys.Count);
        selectionSummary.text = $"{count} selected   |   {visibleProducts.Count} of {products.Count} products shown";
        emptyMessage.text = products.Count == 0 ? "No products are available for marketing." : "No products match your search.";
        emptyMessage.gameObject.SetActive(visibleProducts.Count == 0);
    }

    private List<ProductKey> GetAvailableKeys()
    {
        var keys = new List<ProductKey>(products.Count);
        foreach (var product in products)
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
        if (selection == null)
        {
            return;
        }

        var first = Mathf.Max(0, Mathf.FloorToInt(content.anchoredPosition.y / RowStride));
        first = Mathf.Min(first, Mathf.Max(0, visibleProducts.Count - pooledRows.Count));
        for (var index = 0; index < pooledRows.Count; index++)
        {
            var productIndex = first + index;
            var product = productIndex < visibleProducts.Count ? visibleProducts[productIndex] : null;
            pooledRows[index].Bind(product, productIndex * RowStride, selection, texts);
        }
    }
}
