using System.Reflection;
using HarmonyLib;
using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MarketingAutomation.GameIntegration.Marketing;

// Native German names: scriptMarketingKonsole_ caches the console campaign menu;
// kampagne is the selected campaign index;
// typ is product type (0 = game, 1 = console); disableWarten disables native waiting.
// art is the waiting-task kind (-1 keeps it dormant). Keep these native names unchanged.
internal static class NativeMarketingTasks
{
    private static readonly MethodInfo FindTaskDependencies =
        AccessTools.DeclaredMethod(typeof(taskMarketing), "FindScripts");

    internal static taskMarketing PurchaseCampaign(
        roomScript room,
        GUI_Main manager,
        MarketingProduct product,
        int campaign,
        NativeCampaignOption option)
    {
        var ui = new NativeGameUi(manager);
        var gameCampaigns = ui.GameMarketingMenu;
        var consoleCampaigns = ui.ConsoleMarketingMenu;
        var isGame = product.Key.Kind == ProductKind.Game;
        var price = isGame ? option.GamePrice : option.ConsolePrice;
        var work = isGame ? gameCampaigns.workPoints[campaign] : consoleCampaigns.workPoints[campaign];

        if (price < 0 || work <= 0 || room.mS_.money < price || !product.CanMarket(room.mS_.myID))
        {
            throw new InvalidOperationException("Native campaign data or eligibility changed before purchase.");
        }

        var task = CreateInitializedCampaign(manager);
        task.Init(false);
        task.targetID = product.Key.NativeId;
        task.typ = isGame ? 0 : 1;
        task.kampagne = campaign;

        task.automatic = task.stopAutomatic = task.disableWarten = false;
        task.points = task.pointsLeft = work;

        task.gS_ = product.NativeGame;
        task.pS_ = product.NativeConsole;
        task.scriptMarketing_ = gameCampaigns;
        task.scriptMarketingKonsole_ = consoleCampaigns;

        // All checks precede payment, and each new native task is purchased exactly once.
        room.mS_.Pay(price, 12);
        return task;
    }

    private static taskMarketing CreateInitializedCampaign(GUI_Main manager)
    {
        var task = manager.AddTask_Marketing();
        try
        {
            InitializeDependencies(task);
            return task;
        }
        catch
        {
            task.gameObject.SetActive(false);
            Object.Destroy(task.gameObject);
            throw;
        }
    }

    internal static void InitializeDependencies(taskMarketing task)
    {
        // Confirmation can run partway through a frame, before Unity calls Start.
        // Restored paid tasks also need their native dependency caches before adoption.
        FindTaskDependencies.Invoke(task, null);
    }

    internal static taskWait CreateWaitingTask(GUI_Main manager)
    {
        var task = manager.AddTask_Wait();
        task.Init(false);
        task.art = -1; // Dormant: never dispatch QA work.
        return task;
    }
}
