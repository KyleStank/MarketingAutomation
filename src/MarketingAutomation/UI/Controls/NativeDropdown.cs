using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation;

internal static class NativeDropdown
{
    internal static Dropdown Create(Dropdown source, Transform parent, GUI_Main manager, string name, List<string> options)
    {
        var mapping = new Dictionary<Transform, RectTransform>();
        var root = NativeUi.CopyTree(source.transform, parent, manager, mapping: mapping);
        root.name = name;

        // The dropdown template needs functional uGUI controls, but none of the
        // original menu's serialized listeners, PlayerPrefs, or row behavior.
        foreach (var original in source.GetComponentsInChildren<Toggle>(true))
        {
            var toggle = mapping[original.transform].gameObject.AddComponent<Toggle>();
            CopySelectable(original, toggle, mapping);
            toggle.graphic = Map(original.graphic, mapping);
            toggle.toggleTransition = original.toggleTransition;
            toggle.SetIsOnWithoutNotify(false);
        }
        foreach (var original in source.GetComponentsInChildren<Scrollbar>(true))
        {
            var scrollbar = mapping[original.transform].gameObject.AddComponent<Scrollbar>();
            CopySelectable(original, scrollbar, mapping);
            scrollbar.handleRect = mapping[original.handleRect];
            scrollbar.direction = original.direction;
            scrollbar.numberOfSteps = original.numberOfSteps;
        }
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
            scroll.horizontalScrollbar = original.horizontalScrollbar ?
                mapping[original.horizontalScrollbar.transform].GetComponent<Scrollbar>() : null;
            scroll.verticalScrollbar = original.verticalScrollbar ?
                mapping[original.verticalScrollbar.transform].GetComponent<Scrollbar>() : null;
            scroll.horizontalScrollbarVisibility = original.horizontalScrollbarVisibility;
            scroll.verticalScrollbarVisibility = original.verticalScrollbarVisibility;
            scroll.horizontalScrollbarSpacing = original.horizontalScrollbarSpacing;
            scroll.verticalScrollbarSpacing = original.verticalScrollbarSpacing;
        }
        var dropdown = root.gameObject.AddComponent<Dropdown>();
        CopySelectable(source, dropdown, mapping);
        dropdown.template = mapping[source.template];
        dropdown.captionText = Map(source.captionText, mapping);
        dropdown.itemText = Map(source.itemText, mapping);
        dropdown.captionImage = Map(source.captionImage, mapping);
        dropdown.itemImage = Map(source.itemImage, mapping);
        dropdown.alphaFadeSpeed = source.alphaFadeSpeed;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);
        return dropdown;
    }

    private static T Map<T>(T source, Dictionary<Transform, RectTransform> mapping) where T : Component =>
        source ? mapping[source.transform].GetComponent<T>() : null;

    private static void CopySelectable(Selectable source, Selectable target, Dictionary<Transform, RectTransform> mapping)
    {
        target.targetGraphic = Map(source.targetGraphic, mapping);
        target.transition = source.transition;
        target.colors = source.colors;
        target.spriteState = source.spriteState;
        target.navigation = new Navigation { mode = Navigation.Mode.None };
    }
}
