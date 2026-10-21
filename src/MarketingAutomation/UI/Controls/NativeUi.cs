using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation;

// Copy visual components explicitly. Instantiating native menu/row scripts would
// also copy purchase callbacks, localization writers, and destructive OnDisable logic.
internal static class NativeUi
{
    internal static RectTransform CopyTree(Transform source, Transform parent, GUI_Main manager,
        Func<Transform, bool> include = null, Dictionary<Transform, RectTransform> mapping = null)
    {
        var target = UiFactory.CreateRect(source.name, parent);
        if (mapping != null)
        {
            mapping[source] = target;
        }

        UiFactory.CopyRect((RectTransform)source, target);
        var image = source.GetComponent<Image>();
        var text = source.GetComponent<Text>();
        if (image)
        {
            var copy = UiFactory.CopyImage(image, target.gameObject);
            if (source.GetComponent<Fill_Window>())
            {
                copy.material = manager.matFill_Window;
            }
        }
        if (text)
        {
            var copy = target.gameObject.AddComponent<Text>();
            copy.font = text.font;
            copy.fontStyle = text.fontStyle;
            copy.fontSize = text.fontSize;
            copy.color = text.color;
            copy.material = text.material;
            copy.alignment = text.alignment;
            copy.lineSpacing = text.lineSpacing;
            copy.alignByGeometry = text.alignByGeometry;
            copy.supportRichText = text.supportRichText;
            copy.horizontalOverflow = text.horizontalOverflow;
            copy.verticalOverflow = text.verticalOverflow;
            copy.resizeTextForBestFit = text.resizeTextForBestFit;
            copy.resizeTextMinSize = text.resizeTextMinSize;
            copy.resizeTextMaxSize = text.resizeTextMaxSize;
            copy.text = text.text;
            copy.raycastTarget = false;
            copy.enabled = text.enabled;
            // This native visual-only component chooses the configured language's
            // font. Unlike setText it cannot overwrite the mod's label contents.
            if (source.GetComponent<setFont>())
            {
                target.gameObject.AddComponent<setFont>();
            }
        }
        UiFactory.CopyEffects(source.gameObject, target.gameObject);
        var mask = source.GetComponent<Mask>();
        if (mask)
        {
            target.gameObject.AddComponent<Mask>().showMaskGraphic = mask.showMaskGraphic;
        }

        if (source.GetComponent<RectMask2D>())
        {
            target.gameObject.AddComponent<RectMask2D>();
        }

        var animation = source.GetComponent<Animation>();
        if (animation && animation.clip)
        {
            var copy = target.gameObject.AddComponent<Animation>();
            copy.AddClip(animation.clip, animation.clip.name);
            copy.clip = animation.clip;
            copy.playAutomatically = animation.playAutomatically;
            copy.wrapMode = animation.wrapMode;
            copy.cullingType = animation.cullingType;
        }
        foreach (Transform child in source)
        {
            if (include == null || include(child))
            {
                CopyTree(child, target, manager, include, mapping);
            }
        }
        target.gameObject.SetActive(source.gameObject.activeSelf);
        return target;
    }

    internal static Button AddButton(RectTransform root, Button style)
    {
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        button.transition = style.transition;
        button.colors = style.colors;
        button.spriteState = style.spriteState;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    internal static T Require<T>(Transform root, string path) where T : Component
    {
        var child = root.Find(path);
        var value = child ? child.GetComponent<T>() : null;
        if (!value)
        {
            throw new InvalidOperationException($"Missing native UI template: {path} ({typeof(T).Name}).");
        }

        return value;
    }
}
