using UnityEngine;

namespace MarketingAutomation.GameIntegration.UI;

// roomButtonScript owns the room's shared controls, context panels and status windows.
// Each array has its own slot meanings; access stays live because native cleanup replaces entries.
internal sealed class NativeRoomControls
{
    private const int WaitingWindowSlot = 26;
    private readonly roomButtonScript _buttons;

    internal NativeRoomControls(roomButtonScript buttons) => _buttons = buttons;

    internal GameObject SharedRoomOptions => _buttons.uiObjects[2];

    // Native prefab name OptionenMarketingAktiv means active Marketing options.
    internal GameObject ActiveMarketingOptions => _buttons.uiOptions[12];

    internal NativeMarketingRoomWindow MarketingStatusWindow =>
        new(_buttons.uiWindows[4].GetComponent<roomWindow>());

    internal bool HasWaitingWindowSlot =>
        _buttons.uiWindows != null && _buttons.uiWindows.Length > WaitingWindowSlot;

    // RemoveMenus destroys unsupported windows. The integration temporarily clears this
    // exact array entry and restores it afterwards; a cached reference would not protect it.
    internal GameObject WaitingStatusWindow
    {
        get => _buttons.uiWindows[WaitingWindowSlot];
        set => _buttons.uiWindows[WaitingWindowSlot] = value;
    }
}
