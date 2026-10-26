using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MarketingAutomation.Automation.Runtime;
using MarketingAutomation.GameIntegration;
using MarketingAutomation.UI;

namespace MarketingAutomation;

[BepInPlugin(PluginMetadata.Id, PluginMetadata.Name, PluginMetadata.Version)]
public class MarketingAutomationPlugin : BaseUnityPlugin
{
    private const string TargetedGameAssemblyGuid = "540f0e40-727e-4ed5-a8b6-1d988c4ec8e4";

    internal new static ManualLogSource Logger;
    private Harmony _harmony;

    private void Awake()
    {
        Logger = base.Logger;

        Logger.LogInfo($"Plugin {PluginMetadata.Id} is loading...");
        try
        {
            var gameAssembly = typeof(Menu_Marketing_Main).Assembly;
            var fingerprint = gameAssembly.ManifestModule.ModuleVersionId;
            Logger.LogInfo($"Game MVID: {fingerprint}; Unity: {UnityEngine.Application.unityVersion}; " +
                           $"BepInEx: {typeof(BaseUnityPlugin).Assembly.GetName().Version}; " +
                           $"Harmony: {typeof(Harmony).Assembly.GetName().Version}.");
            if (fingerprint != new Guid(TargetedGameAssemblyGuid))
                Logger.LogWarning("The game assembly differs from the targeted version.");

            _harmony = new Harmony(PluginMetadata.Id);
            InstallIntegration<MarketingMenuIntegration>();
            InstallIntegration<QueueIntegration>();
            InstallIntegration<QueuePersistenceIntegration>();

            Logger.LogInfo($"{PluginMetadata.Name} finished loading.");
        }
        catch (Exception exception)
        {
            _harmony?.UnpatchSelf();
            Logger.LogError($"{PluginMetadata.Name} failed to load: {exception}");
        }
    }

    private void InstallIntegration<T>()
        where T : BaseIntegration, new()
        => new T().Install(_harmony);

    private void OnDestroy()
    {
        MarketingQueue.RemoveAll();
        MarketingMenuExtension.RemoveAll();

        _harmony?.UnpatchSelf();
    }
}
