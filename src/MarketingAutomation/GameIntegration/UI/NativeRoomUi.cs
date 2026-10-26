using UnityEngine;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeRoomUi
{
    private readonly roomScript _room;

    internal NativeRoomUi(roomScript room) => _room = room;

    // InitUI clones this template into the room's live controls. Some room states lack the template.
    internal GameObject ControlsPrefab =>
        _room.uiObjects != null && _room.uiObjects.Length > 0 ? _room.uiObjects[0] : null;
}
