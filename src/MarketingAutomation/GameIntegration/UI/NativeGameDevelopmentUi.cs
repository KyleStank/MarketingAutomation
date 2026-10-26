using UnityEngine;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeGameDevelopmentUi
{
    private readonly Menu_DevGame _menu;

    internal NativeGameDevelopmentUi(Menu_DevGame menu) => _menu = menu;

    internal Transform PageIndicators => _menu.uiObjects[32].transform;
}
