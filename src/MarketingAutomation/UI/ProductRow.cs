using MarketingAutomation.GameIntegration.Marketing;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

internal sealed class ProductRow
{
    private const float RowHeight = 56f;

    private readonly RectTransform _root;
    private readonly Text _nameLabel;
    private readonly Text _releaseDateLabel;
    private readonly Text _hypeLabel;
    private readonly Image _icon;
    private readonly GameObject _checkmark;
    private readonly tooltip _detailsTooltip;
    private MarketingProduct _product;

    internal ProductRow(
        GameObject style,
        RectTransform checkboxStyle,
        RectTransform parent,
        GUI_Main manager,
        Action<MarketingProduct> clicked)
    {
        _root = NativeVisualTree.Copy(style.transform, parent, manager);
        _root.name = "ProductRow";
        _root.anchorMin = new Vector2(0f, 1f);
        _root.anchorMax = new Vector2(1f, 1f);
        _root.pivot = new Vector2(0.5f, 1f);
        _root.sizeDelta = new Vector2(-8f, RowHeight);

        var button = UiFactory.AddButton(_root, style.GetComponent<Button>());
        button.onClick.AddListener(() =>
        {
            if (_product != null)
                clicked(_product);
        });

        _nameLabel = NativeUiLookup.RequireComponent<Text>(_root, "TextName");
        _releaseDateLabel = NativeUiLookup.RequireComponent<Text>(_root, "TextInfo");
        _hypeLabel = NativeUiLookup.RequireComponent<Text>(_root, "TextHype");

        PositionLabel(_nameLabel.rectTransform, 78f, -16f, 360f, TextAnchor.MiddleLeft, _nameLabel);
        PositionLabel(_releaseDateLabel.rectTransform, 78f, -41f, 360f, TextAnchor.MiddleLeft, _releaseDateLabel);
        _nameLabel.resizeTextMinSize = _releaseDateLabel.resizeTextMinSize = 10;
        _releaseDateLabel.resizeTextMaxSize = 14;
        PositionLabel(_hypeLabel.rectTransform, 492f, -28f, 48f, TextAnchor.MiddleCenter, _hypeLabel);

        var hypeIcon = NativeUiLookup.RequireComponent<RectTransform>(_root, "IconHype");
        Position(hypeIcon, new Vector2(466f, -28f), new Vector2(24f, 24f));

        var line = _root.Find("Line");
        if (line)
        {
            line.gameObject.SetActive(false);
        }

        var checkbox = NativeVisualTree.Copy(checkboxStyle, _root, manager);
        Position(checkbox, new Vector2(22f, -28f), checkboxStyle.sizeDelta);
        _checkmark = checkbox.Find("Checkmark").gameObject;

        var iconRoot = UiFactory.CreateRect("ProductType", _root);
        Position(iconRoot, new Vector2(55f, -28f), new Vector2(28f, 28f));
        _icon = iconRoot.gameObject.AddComponent<Image>();
        _icon.preserveAspect = true;

        // Let the whole row receive pointer/scroll events, including its checkbox.
        foreach (var graphic in _root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = graphic.gameObject == _root.gameObject;
        }

        _detailsTooltip = _root.gameObject.AddComponent<tooltip>();
    }

    private static void PositionLabel(
        RectTransform rect,
        float x,
        float y,
        float width,
        TextAnchor alignment,
        Text text)
    {
        Position(rect, new Vector2(x, y), new Vector2(width, 24f));
        rect.pivot = new Vector2(0f, 0.5f);
        text.alignment = alignment;
    }

    private static void Position(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    internal void Bind(MarketingProduct next, float offset, ProductSelection selection, textScript texts)
    {
        _root.gameObject.SetActive(next != null && next.Exists);
        if (next == null || !next.Exists)
        {
            _product = null;
            return;
        }

        _root.anchoredPosition = new Vector2(0f, -offset);
        if (_product != next)
        {
            _product = next;
            _nameLabel.text = _product.Name;
            _releaseDateLabel.text = _product.ReleaseDate(texts);
            _hypeLabel.text = Mathf.RoundToInt(_product.Hype).ToString();
            _icon.sprite = _product.Icon;
            _detailsTooltip.c = _product.Tooltip;
        }

        _checkmark.SetActive(selection.Contains(_product.Key));
    }
}
