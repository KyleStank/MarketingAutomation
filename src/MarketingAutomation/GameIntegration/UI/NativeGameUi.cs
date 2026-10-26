using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

// Named access to the GUI_Main slots used by this mod. The indices belong to the native scene,
// not to the individual menus' own uiObjects arrays. Resolve on access rather than caching scene objects.
internal sealed class NativeGameUi
{
    private readonly GUI_Main _manager;

    internal NativeGameUi(GUI_Main manager) => _manager = manager;

    internal GameObject MessageDialog => _manager.uiObjects[40];

    // Preserve the underlying menu; callers own any temporary dialog layering.
    internal void ShowMessage(string message) => _manager.MessageBox(message, false);

    internal Toggle EmployeeSelectionToggle => _manager.uiObjects[2].GetComponent<Toggle>();

    internal Menu_DevGame GameDevelopmentMenu => _manager.uiObjects[56].GetComponent<Menu_DevGame>();

    internal Menu_Marketing_SelectGame MarketingGameSelector =>
        _manager.uiObjects[90].GetComponent<Menu_Marketing_SelectGame>();

    internal GameObject GameMarketingPanel => _manager.uiObjects[89];

    // Native German names: GameKampagne means game campaign; KonsoleKampagne means console campaign.
    internal Menu_Marketing_GameKampagne GameMarketingMenu =>
        GameMarketingPanel.GetComponent<Menu_Marketing_GameKampagne>();

    internal Menu_Marketing_KonsoleKampagne ConsoleMarketingMenu =>
        _manager.uiObjects[321].GetComponent<Menu_Marketing_KonsoleKampagne>();
}
