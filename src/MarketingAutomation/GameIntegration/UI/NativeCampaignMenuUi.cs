using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeCampaignMenuUi
{
    // GameKampagne is the native game-campaign menu. Its first two slots are not campaign buttons.
    private const int FirstCampaignButtonIndex = 2;
    private readonly Menu_Marketing_GameKampagne _menu;

    internal NativeCampaignMenuUi(Menu_Marketing_GameKampagne menu) => _menu = menu;

    internal Button GetCampaignButton(int campaignIndex) =>
        _menu.uiObjects[FirstCampaignButtonIndex + campaignIndex].GetComponent<Button>();
}
