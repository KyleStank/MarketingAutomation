using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeArcadePricingUi
{
    // Arcadepreise means arcade prices.
    private readonly Menu_Arcadepreise _menu;

    internal NativeArcadePricingUi(Menu_Arcadepreise menu) => _menu = menu;

    internal InputField SearchField => _menu.uiObjects[6].GetComponent<InputField>();
}
