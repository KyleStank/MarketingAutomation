using HarmonyLib;
using MarketingAutomation.UI;

namespace MarketingAutomation.GameIntegration;

public class MarketingMenuIntegration : BaseIntegration
{
    public override void Install(Harmony harmony)
    {
        var initializeMenu = AccessTools.DeclaredMethod(
            typeof(Menu_Marketing_Main),
            nameof(Menu_Marketing_Main.Init),
            [typeof(roomScript)]);
        if (initializeMenu == null || initializeMenu.ReturnType != typeof(void))
            throw new MissingMethodException(
                $"{nameof(Menu_Marketing_Main)}.{nameof(Menu_Marketing_Main.Init)}({nameof(roomScript)}) is unavailable");

        harmony.Patch(initializeMenu,
            postfix: new HarmonyMethod(typeof(MarketingMenuIntegration), nameof(PatchInitializeMenu)));
    }

    // ReSharper disable once InconsistentNaming
    private static void PatchInitializeMenu(
        Menu_Marketing_Main __instance,
        [HarmonyArgument(0)] roomScript room)
    {
        if (!__instance || !room) return;

        var extension = __instance.GetComponent<MarketingMenuExtension>();
        if (!extension)
            extension = __instance.gameObject.AddComponent<MarketingMenuExtension>();

        extension.Bind(__instance, room);
    }
}
