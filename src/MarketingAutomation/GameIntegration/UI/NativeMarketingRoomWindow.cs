using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

// The Window_Marketing prefab uses roomWindow, but its slots differ from Window_Wait.
internal sealed class NativeMarketingRoomWindow
{
    private readonly roomWindow _window;

    internal NativeMarketingRoomWindow(roomWindow window) => _window = window;

    internal GameObject AutomationIndicator => _window.uiObjects[4];

    internal Image HypeWaitingWarningIcon => NativeUiLookup.RequireComponent<Image>(_window.uiObjects[5].transform, "Icon");
}
