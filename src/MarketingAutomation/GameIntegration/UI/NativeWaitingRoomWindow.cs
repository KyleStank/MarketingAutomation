using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

// Named status text and icon slots for the generic Window_Wait prefab.
internal sealed class NativeWaitingRoomWindow
{
    private readonly roomWindow _window;

    internal NativeWaitingRoomWindow(roomWindow window) => _window = window;

    internal Text StatusText => _window.uiObjects[0].GetComponent<Text>();

    internal Image StatusIcon => _window.uiObjects[1].GetComponent<Image>();
}
