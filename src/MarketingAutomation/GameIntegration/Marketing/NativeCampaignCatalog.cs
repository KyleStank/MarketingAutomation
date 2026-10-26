using MarketingAutomation.GameIntegration.UI;

namespace MarketingAutomation.GameIntegration.Marketing;

internal static class NativeCampaignCatalog
{
    private const int CampaignCount = 6;

    // Native German names: Kampagne = campaign, Konsole = console, preise = prices,
    // hypeProKampagne = hype gained per campaign. These menus also supply data to native tasks.
    internal static NativeCampaignOption[] Read(GUI_Main manager, mainScript company)
    {
        var ui = new NativeGameUi(manager);
        var games = ui.GameMarketingMenu;
        var consoles = ui.ConsoleMarketingMenu;
        var unlocks = company.GetComponent<unlockScript>();
        ValidateArrayLengths(games, consoles, unlocks);

        var options = new NativeCampaignOption[CampaignCount];
        for (var index = 0; index < options.Length; index++)
        {
            ValidateCampaignData(games, consoles, index);
            options[index] = new NativeCampaignOption
            {
                Index = index,
                Name = company.tS_.GetText(518 + index),
                GamePrice = games.preise[index],
                ConsolePrice = consoles.preise[index],
                HypeGain = games.hypeProKampagne[index],
                HypeCap = games.maxHype[index],
                Unlocked = IsCampaignUnlocked(unlocks, index)
            };
        }

        return options;
    }

    private static void ValidateArrayLengths(
        Menu_Marketing_GameKampagne games,
        Menu_Marketing_KonsoleKampagne consoles,
        unlockScript unlocks)
    {
        if (!unlocks || games.preise.Length != CampaignCount || consoles.preise.Length != CampaignCount ||
            games.maxHype.Length != CampaignCount || consoles.maxHype.Length != CampaignCount ||
            games.workPoints.Length != CampaignCount || consoles.workPoints.Length != CampaignCount ||
            games.hypeProKampagne.Length != CampaignCount || consoles.hypeProKampagne.Length != CampaignCount)
        {
            throw new NotSupportedException("Native marketing campaign data is incomplete.");
        }
    }

    private static void ValidateCampaignData(
        Menu_Marketing_GameKampagne games,
        Menu_Marketing_KonsoleKampagne consoles,
        int index)
    {
        if (games.preise[index] < 0 || consoles.preise[index] < 0 ||
            games.workPoints[index] <= 0 || consoles.workPoints[index] <= 0 ||
            games.maxHype[index] <= 0 || games.hypeProKampagne[index] <= 0)
        {
            throw new NotSupportedException("Native marketing prices, work, or hype data is invalid.");
        }

        // One shared campaign selector is valid only while games and consoles have identical hype rules.
        if (games.maxHype[index] != consoles.maxHype[index] ||
            games.hypeProKampagne[index] != consoles.hypeProKampagne[index])
        {
            throw new NotSupportedException("Game/console campaign hype rules differ; shared controls need review.");
        }
    }

    private static bool IsCampaignUnlocked(unlockScript unlocks, int index)
    {
        switch (index)
        {
            case 3:
                return unlocks.Get(unlockScript.marketingInternet);
            case 4:
                return unlocks.Get(unlockScript.marketingStreamer);
            default:
                return true;
        }
    }
}
