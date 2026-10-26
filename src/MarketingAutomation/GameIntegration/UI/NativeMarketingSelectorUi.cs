using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeMarketingSelectorUi
{
    private readonly Menu_Marketing_SelectGame _menu;

    internal NativeMarketingSelectorUi(Menu_Marketing_SelectGame menu) => _menu = menu;

    internal RectTransform Window => (RectTransform)_menu.uiObjects[2].transform;

    internal ScrollRect ProductScroll => _menu.uiObjects[0].GetComponentInParent<ScrollRect>(true);

    internal Dropdown SortDropdown => _menu.uiObjects[1].GetComponent<Dropdown>();

    internal GameObject ProductRowPrefab => _menu.uiPrefabs[0];
}
