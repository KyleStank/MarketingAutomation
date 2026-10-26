using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeSubscriptionPriceUi
{
    // Abo means subscription; Preis means price.
    private readonly Menu_Abo_Preis _menu;

    internal NativeSubscriptionPriceUi(Menu_Abo_Preis menu) => _menu = menu;

    internal Slider PriceSlider => _menu.uiObjects[2].GetComponent<Slider>();
}
