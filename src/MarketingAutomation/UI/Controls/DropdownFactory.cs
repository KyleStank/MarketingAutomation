using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI.Controls;

internal static class DropdownFactory
{
    internal static Dropdown Create(Dropdown source, Transform parent, GUI_Main manager, string name,
        List<string> options)
    {
        var mapping = new Dictionary<Transform, RectTransform>();
        var root = NativeVisualTree.Copy(source.transform, parent, manager, mapping: mapping);
        root.name = name;

        // The dropdown template needs functional uGUI controls, but none of the
        // original menu's serialized listeners, PlayerPrefs, or row behavior.
        CopyTemplateToggles(source, mapping);

        // Scroll views resolve the copied scrollbars, so create those bindings first.
        CopyTemplateScrollbars(source, mapping);
        CopyTemplateScrollViews(source, mapping);

        var dropdown = root.gameObject.AddComponent<Dropdown>();
        CopySelectable(source, dropdown, mapping);

        dropdown.template = mapping[source.template];
        dropdown.captionText = ResolveCopiedComponent(source.captionText, mapping);
        dropdown.itemText = ResolveCopiedComponent(source.itemText, mapping);
        dropdown.captionImage = ResolveCopiedComponent(source.captionImage, mapping);
        dropdown.itemImage = ResolveCopiedComponent(source.itemImage, mapping);
        dropdown.alphaFadeSpeed = source.alphaFadeSpeed;

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);

        return dropdown;
    }

    private static void CopyTemplateToggles(Dropdown source, Dictionary<Transform, RectTransform> mapping)
    {
        foreach (var original in source.GetComponentsInChildren<Toggle>(true))
        {
            var toggle = mapping[original.transform].gameObject.AddComponent<Toggle>();
            CopySelectable(original, toggle, mapping);
            toggle.graphic = ResolveCopiedComponent(original.graphic, mapping);
            toggle.toggleTransition = original.toggleTransition;
            toggle.SetIsOnWithoutNotify(false);
        }
    }

    private static void CopySelectable(Selectable source, Selectable target,
        Dictionary<Transform, RectTransform> mapping)
    {
        target.targetGraphic = ResolveCopiedComponent(source.targetGraphic, mapping);
        target.transition = source.transition;
        target.colors = source.colors;
        target.spriteState = source.spriteState;
        target.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    private static T ResolveCopiedComponent<T>(T source, Dictionary<Transform, RectTransform> mapping) where T : Component =>
        source ? mapping[source.transform].GetComponent<T>() : null;

    private static void CopyTemplateScrollbars(Dropdown source, Dictionary<Transform, RectTransform> mapping)
    {
        foreach (var original in source.GetComponentsInChildren<Scrollbar>(true))
        {
            var scrollbar = mapping[original.transform].gameObject.AddComponent<Scrollbar>();
            CopySelectable(original, scrollbar, mapping);
            scrollbar.handleRect = mapping[original.handleRect];
            scrollbar.direction = original.direction;
            scrollbar.numberOfSteps = original.numberOfSteps;
        }
    }

    private static void CopyTemplateScrollViews(Dropdown source, Dictionary<Transform, RectTransform> mapping)
    {
        foreach (var original in source.GetComponentsInChildren<ScrollRect>(true))
        {
            var scroll = mapping[original.transform].gameObject.AddComponent<ScrollRect>();
            scroll.content = mapping[original.content];
            scroll.viewport = mapping[original.viewport];

            scroll.horizontal = original.horizontal;
            scroll.vertical = original.vertical;

            scroll.movementType = original.movementType;
            scroll.elasticity = original.elasticity;
            scroll.inertia = original.inertia;
            scroll.decelerationRate = original.decelerationRate;
            scroll.scrollSensitivity = original.scrollSensitivity;

            scroll.horizontalScrollbar = original.horizontalScrollbar
                ? mapping[original.horizontalScrollbar.transform].GetComponent<Scrollbar>()
                : null;
            scroll.verticalScrollbar = original.verticalScrollbar
                ? mapping[original.verticalScrollbar.transform].GetComponent<Scrollbar>()
                : null;

            scroll.horizontalScrollbarVisibility = original.horizontalScrollbarVisibility;
            scroll.verticalScrollbarVisibility = original.verticalScrollbarVisibility;
            scroll.horizontalScrollbarSpacing = original.horizontalScrollbarSpacing;
            scroll.verticalScrollbarSpacing = original.verticalScrollbarSpacing;
        }
    }
}
