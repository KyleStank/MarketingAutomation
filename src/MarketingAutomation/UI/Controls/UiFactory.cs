using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI.Controls;

// Compose fresh controls from native visual properties, avoiding serialized game listeners.
internal static class UiFactory
{
    internal static RectTransform CreateRect(string name, Transform parent)
    {
        var control = new GameObject(name, typeof(RectTransform));
        control.layer = parent.gameObject.layer;
        var rect = (RectTransform)control.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    internal static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    internal static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    internal static Image CopyImage(Image source, GameObject target)
    {
        var image = target.AddComponent<Image>();
        image.sprite = source.sprite;
        image.type = source.type;
        image.color = source.color;
        image.material = source.material;
        image.preserveAspect = source.preserveAspect;
        image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        image.raycastTarget = source.raycastTarget;
        image.enabled = source.enabled;
        image.maskable = source.maskable;
        return image;
    }

    internal static Button CreateMenuEntry(string name, Transform parent, Button style, string label, Sprite icon)
    {
        var sourceRect = (RectTransform)style.transform;
        var button = CreateButton(name, parent, style, null, "", sourceRect.anchoredPosition, sourceRect.sizeDelta);
        CopyRect(sourceRect, (RectTransform)button.transform);
        CopyEffects(style.gameObject, button.gameObject);

        // Recreate only the native visual children, in their original drawing order.
        // No persistent button listeners, setText, or game-owned behavior is cloned.
        foreach (Transform child in style.transform)
        {
            var sourceImage = child.GetComponent<Image>();
            var sourceText = child.GetComponent<Text>();
            if (sourceText)
            {
                var text = CreateText(child.name, button.transform, sourceText, label, Vector2.zero, Vector2.zero);
                CopyRect((RectTransform)child, text.rectTransform);
                text.alignment = sourceText.alignment;
                text.lineSpacing = sourceText.lineSpacing;
                text.horizontalOverflow = sourceText.horizontalOverflow;
                text.verticalOverflow = sourceText.verticalOverflow;
                text.alignByGeometry = sourceText.alignByGeometry;
                CopyEffects(child.gameObject, text.gameObject);
                continue;
            }

            var rect = CreateRect(child.name, button.transform);
            CopyRect((RectTransform)child, rect);
            if (sourceImage)
            {
                var image = CopyImage(sourceImage, rect.gameObject);
                image.raycastTarget = false;
                if (child.name == "Icon")
                {
                    image.sprite = icon;
                    image.preserveAspect = true;
                }
            }

            CopyEffects(child.gameObject, rect.gameObject);
        }

        return button;
    }

    internal static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition3D = source.anchoredPosition3D;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    internal static void CopyEffects(GameObject source, GameObject target)
    {
        foreach (var effect in source.GetComponents<Shadow>())
        {
            Shadow copy = effect is Outline ? target.AddComponent<Outline>() : target.AddComponent<Shadow>();
            copy.effectColor = effect.effectColor;
            copy.effectDistance = effect.effectDistance;
            copy.useGraphicAlpha = effect.useGraphicAlpha;
            copy.enabled = effect.enabled;
        }
    }

    internal static Text CreateText(string name, Transform parent, Text style, string value, Vector2 position,
        Vector2 size)
    {
        var rect = CreateRect(name, parent);
        Place(rect, position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = style.font;
        text.fontSize = style.fontSize;
        text.fontStyle = style.fontStyle;
        text.color = style.color;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = false;
        text.resizeTextForBestFit = style.resizeTextForBestFit;
        text.resizeTextMinSize = style.resizeTextMinSize;
        text.resizeTextMaxSize = style.resizeTextMaxSize;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    internal static Button CreateButton(string name, Transform parent, Button style, Text textStyle,
        string label, Vector2 position, Vector2 size)
    {
        var rect = CreateRect(name, parent);
        Place(rect, position, size);
        var image = CopyImage(style.image, rect.gameObject);
        image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = style.colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (textStyle)
        {
            var text = CreateText("Label", rect, textStyle, label, Vector2.zero, size);
            Stretch(text.rectTransform);
        }

        return button;
    }
}
