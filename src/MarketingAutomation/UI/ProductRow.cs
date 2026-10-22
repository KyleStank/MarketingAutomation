using MarketingAutomation.GameIntegration;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

internal sealed class ProductRow
{
    private const float RowHeight = 56f;

    private readonly RectTransform root;
    private readonly Text nameLabel;
    private readonly Text releaseDateLabel;
    private readonly Text hypeLabel;
    private readonly Image icon;
    private readonly GameObject checkmark;
    private readonly tooltip detailsTooltip;
    private MarketingProduct product;

    internal ProductRow(
        GameObject style,
        RectTransform checkboxStyle,
        RectTransform parent,
        GUI_Main manager,
        Action<MarketingProduct> clicked)
    {
        root = NativeUi.CopyTree(style.transform, parent, manager);
        root.name = "ProductRow";
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(-8f, RowHeight);
        var button = NativeUi.AddButton(root, style.GetComponent<Button>());
        button.onClick.AddListener(() =>
        {
            if (product != null)
            {
                clicked(product);
            }
        });
        nameLabel = NativeUi.Require<Text>(root, "TextName");
        releaseDateLabel = NativeUi.Require<Text>(root, "TextInfo");
        hypeLabel = NativeUi.Require<Text>(root, "TextHype");
        PositionLabel(nameLabel.rectTransform, 78f, -16f, 360f, TextAnchor.MiddleLeft, nameLabel);
        PositionLabel(releaseDateLabel.rectTransform, 78f, -41f, 360f, TextAnchor.MiddleLeft, releaseDateLabel);
        nameLabel.resizeTextMinSize = releaseDateLabel.resizeTextMinSize = 10;
        releaseDateLabel.resizeTextMaxSize = 14;
        PositionLabel(hypeLabel.rectTransform, 492f, -28f, 48f, TextAnchor.MiddleCenter, hypeLabel);
        var hypeIcon = NativeUi.Require<RectTransform>(root, "IconHype");
        Position(hypeIcon, new Vector2(466f, -28f), new Vector2(24f, 24f));
        var line = root.Find("Line");
        if (line)
        {
            line.gameObject.SetActive(false);
        }

        var checkbox = NativeUi.CopyTree(checkboxStyle, root, manager);
        Position(checkbox, new Vector2(22f, -28f), checkboxStyle.sizeDelta);
        checkmark = checkbox.Find("Checkmark").gameObject;
        var iconRoot = UiFactory.CreateRect("ProductType", root);
        Position(iconRoot, new Vector2(55f, -28f), new Vector2(28f, 28f));
        icon = iconRoot.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        // Let the whole row receive pointer/scroll events, including its checkbox.
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = graphic.gameObject == root.gameObject;
        }

        detailsTooltip = root.gameObject.AddComponent<tooltip>();
    }

    internal void Bind(MarketingProduct next, float offset, ProductSelection selection, textScript texts)
    {
        root.gameObject.SetActive(next != null && next.Exists);
        if (next == null || !next.Exists)
        {
            product = null;
            return;
        }

        root.anchoredPosition = new Vector2(0f, -offset);
        if (product != next)
        {
            product = next;
            nameLabel.text = product.Name;
            releaseDateLabel.text = product.ReleaseDate(texts);
            hypeLabel.text = Mathf.RoundToInt(product.Hype).ToString();
            icon.sprite = product.Icon;
            detailsTooltip.c = product.Tooltip;
        }

        checkmark.SetActive(selection.Contains(product.Key));
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
}
