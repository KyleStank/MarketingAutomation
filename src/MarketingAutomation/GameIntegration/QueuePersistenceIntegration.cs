using HarmonyLib;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.Persistence;
using UnityEngine;

namespace MarketingAutomation.GameIntegration;

// Saving uses a finalizer so live bindings are restored even when native Save throws.
internal sealed class QueuePersistenceIntegration : BaseIntegration
{
    public override void Install(Harmony harmony)
    {
        Patch(harmony, typeof(GUI_Main), "InitToggles", prefix: nameof(BeforePreferencesRestored));
        Patch(harmony, typeof(savegameScript), "Load", prefix: nameof(BeforeLoad), postfix: nameof(AfterLoad));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(savegameScript), "Save", [typeof(int)]),
            prefix: new HarmonyMethod(typeof(QueuePersistenceIntegration), nameof(BeforeSave)),
            finalizer: new HarmonyMethod(typeof(QueuePersistenceIntegration), nameof(AfterSave)));
    }

    // ReSharper disable once InconsistentNaming
    private static void BeforePreferencesRestored(GUI_Main __instance, mainScript ___mS_)
    {
        if (!___mS_ || ___mS_.multiplayer || !___mS_.save_ || !___mS_.save_.loadingSavegame)
        {
            return;
        }

        // InitToggles replays a stored UI preference using Toggle.isOn, which
        // otherwise invokes the same confirmation used for an actual user click.
        // Seed only this toggle silently; preserve its preference and normal clicks.
        var ui = new NativeGameUi(__instance);
        ui.EmployeeSelectionToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Toggle_PickChars") == 1);
    }

    // ReSharper disable once InconsistentNaming
    private static void BeforeLoad(savegameScript __instance, int __0, out QueueLoadCheckpoint __state) =>
        __state = QueuePersistence.BeginLoad(__instance, __0);

    // ReSharper disable once InconsistentNaming
    private static void AfterLoad(savegameScript __instance, QueueLoadCheckpoint __state) =>
        QueuePersistence.FinishLoad(__instance, __state);

    // ReSharper disable once InconsistentNaming
    private static void BeforeSave(savegameScript __instance, int __0, out QueueSaveCheckpoint __state) =>
        __state = QueuePersistence.BeginSave(__instance, __0);

    // ReSharper disable once InconsistentNaming
    private static Exception AfterSave(savegameScript __instance, ES3Writer ___writer, QueueSaveCheckpoint __state,
        Exception __exception)
    {
        QueuePersistence.FinishSave(__instance, ___writer, __state, __exception);
        return __exception; // Preserve native success/failure exactly.
    }
}
