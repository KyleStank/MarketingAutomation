using System;

namespace MarketingAutomation;

internal sealed class NativeCampaignOption
{
    internal int Index;
    internal string Name;
    internal int GamePrice;
    internal int ConsolePrice;
    internal int HypeGain;
    internal int HypeCap;
    internal bool Unlocked;
}

internal static class NativeCampaignCatalog
{
    internal static NativeCampaignOption[] Read(GUI_Main manager, mainScript company)
    {
        var games = manager.uiObjects[89].GetComponent<Menu_Marketing_GameKampagne>();
        var consoles = manager.uiObjects[321].GetComponent<Menu_Marketing_KonsoleKampagne>();
        var unlocks = company.GetComponent<unlockScript>();
        if (!unlocks || games.preise.Length != 6 || consoles.preise.Length != 6 ||
            games.maxHype.Length != 6 || consoles.maxHype.Length != 6 ||
            games.workPoints.Length != 6 || consoles.workPoints.Length != 6 ||
            games.hypeProKampagne.Length != 6 || consoles.hypeProKampagne.Length != 6)
        {
            throw new NotSupportedException("Native marketing campaign data is incomplete.");
        }

        var options = new NativeCampaignOption[6];
        for (var index = 0; index < options.Length; index++)
        {
            if (games.preise[index] < 0 || consoles.preise[index] < 0 ||
                games.workPoints[index] <= 0 || consoles.workPoints[index] <= 0 ||
                games.maxHype[index] <= 0 || games.hypeProKampagne[index] <= 0)
            {
                throw new NotSupportedException("Native marketing prices, work, or hype data is invalid.");
            }

            if (games.maxHype[index] != consoles.maxHype[index] ||
                games.hypeProKampagne[index] != consoles.hypeProKampagne[index])
            {
                throw new NotSupportedException("Game/console campaign hype rules differ; shared controls need review.");
            }

            options[index] = new NativeCampaignOption
            {
                Index = index,
                Name = company.tS_.GetText(518 + index),
                GamePrice = games.preise[index],
                ConsolePrice = consoles.preise[index],
                HypeGain = games.hypeProKampagne[index],
                HypeCap = games.maxHype[index],
                Unlocked = index == 3 ? unlocks.Get(unlockScript.marketingInternet) :
                    index != 4 || unlocks.Get(unlockScript.marketingStreamer)
            };
        }
        return options;
    }
}
