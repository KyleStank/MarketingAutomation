using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration;
using MarketingAutomation.UI;

namespace MarketingAutomation;

[BepInPlugin(PluginMetadata.Guid, PluginMetadata.Name, PluginMetadata.Version)]
public class MarketingAutomationPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Harmony patches;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginMetadata.Guid} is loaded!");
        try
        {
            var gameAssembly = typeof(Menu_Marketing_Main).Assembly;
            var fingerprint = gameAssembly.ManifestModule.ModuleVersionId;
            Logger.LogInfo($"Game MVID: {fingerprint}; Unity: {UnityEngine.Application.unityVersion}; " +
                $"BepInEx: {typeof(BaseUnityPlugin).Assembly.GetName().Version}; " +
                $"Harmony: {typeof(Harmony).Assembly.GetName().Version}.");
            if (fingerprint != new Guid("540f0e40-727e-4ed5-a8b6-1d988c4ec8e4"))
            {
                throw new NotSupportedException("The game assembly differs from the researched version.");
            }

            var initializeMenu = AccessTools.DeclaredMethod(typeof(Menu_Marketing_Main), "Init", new[] { typeof(roomScript) });
            if (initializeMenu == null || initializeMenu.ReturnType != typeof(void))
            {
                throw new MissingMethodException("Menu_Marketing_Main.Init(roomScript) is unavailable.");
            }

            patches = new Harmony(PluginMetadata.Guid);
            patches.Patch(initializeMenu, postfix: new HarmonyMethod(typeof(MarketingAutomationPlugin), nameof(AfterMarketingMenuInitialized)));
            QueueIntegration.Install(patches);
            Logger.LogInfo("Marketing Automation UI and queue patches installed.");
        }
        catch (Exception exception)
        {
            patches?.UnpatchSelf();
            Logger.LogError($"Marketing Automation UI disabled: {exception}");
        }
    }

    // __0 binds the first native argument without relying on its external parameter name.
    private static void AfterMarketingMenuInitialized(Menu_Marketing_Main __instance, roomScript __0)
    {
        if (!__instance || !__0)
        {
            return;
        }

        var extension = __instance.GetComponent<MarketingMenuExtension>();
        if (!extension)
        {
            extension = __instance.gameObject.AddComponent<MarketingMenuExtension>();
        }

        extension.Bind(__instance, __0);
    }

    private void OnDestroy()
    {
        MarketingQueue.RemoveAll();
        patches?.UnpatchSelf();
        MarketingMenuExtension.RemoveAll();
    }
}
