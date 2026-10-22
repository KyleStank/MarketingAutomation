using HarmonyLib;
using MarketingAutomation.Automation;
using MarketingAutomation.Persistence;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration;

internal class QueueIntegration : BaseIntegration
{
    public override void Install(Harmony harmony)
    {
        Patch(harmony, typeof(taskMarketing), "Complete", postfix: nameof(AfterCampaignCompleted));
        Patch(harmony, typeof(taskMarketing), "Abbrechen", prefix: nameof(BeforeCampaignCanceled));
        Patch(harmony, typeof(taskWait), "Abbrechen", prefix: nameof(BeforeWaitingCanceled));
        Patch(harmony, typeof(roomWindow), "Window_Wait", postfix: nameof(AfterWaitingDisplay));
        Patch(harmony, typeof(roomWindow), "Window_Marketing", postfix: nameof(AfterCampaignDisplay));
        Patch(harmony, typeof(roomScript), "WaitForMinimumHype", postfix: nameof(AfterRoomWaitingCheck));
        Patch(harmony, typeof(roomButtonScript), "BUTTON_Main", prefix: nameof(BeforeRoomMenu));
        Patch(harmony, typeof(roomButtonScript), "RemoveMenus", prefix: nameof(BeforeRoomPanelsPruned),
            postfix: nameof(AfterRoomPanelsPruned));
        Patch(harmony, typeof(roomButtonScript), "BUTTON_Marketing_AutoEnd", prefix: nameof(BeforeAutomationToggle));
        Patch(harmony, typeof(lockAutomatic), "OnEnable", prefix: nameof(BeforeAutomationLabel));
        Patch(harmony, typeof(Menu_W_Aufgabe_Abbrechen), "BUTTON_Yes", prefix: nameof(BeforeCancelConfirmed));
        Patch(harmony, typeof(GUI_Main), "InitToggles", prefix: nameof(BeforePreferencesRestored));
        Patch(harmony, typeof(savegameScript), "Load", prefix: nameof(BeforeLoad), postfix: nameof(AfterLoad));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(savegameScript), "Save", new[] { typeof(int) }),
            prefix: new HarmonyMethod(typeof(QueueIntegration), nameof(BeforeSave)),
            finalizer: new HarmonyMethod(typeof(QueueIntegration), nameof(AfterSave)));
    }

    private static void AfterCampaignCompleted(taskMarketing __instance) =>
        MarketingQueue.ForTask(__instance)?.Completed(__instance);

    private static void BeforeCampaignCanceled(taskMarketing __instance) =>
        MarketingQueue.ForTask(__instance)?.Stop("The native campaign was canceled.");

    private static void BeforeWaitingCanceled(taskWait __instance) =>
        MarketingQueue.ForTask(__instance)?.Stop("Waiting was canceled.");

    private static void BeforeRoomPanelsPruned(roomButtonScript __instance, out GameObject __state)
    {
        __state = null;
        var room = __instance.rS_;
        if (!room || room.typ != 6 || !room.mS_ || room.mS_.multiplayer ||
            __instance.uiWindows == null || __instance.uiWindows.Length <= 26)
        {
            return;
        }

        // Native Marketing cleanup destroys slot 26. Temporarily hide its
        // reference from that cleanup so the normal UpdateWindowWait path works.
        __state = __instance.uiWindows[26];
        __instance.uiWindows[26] = null;
    }

    private static void AfterRoomPanelsPruned(roomButtonScript __instance, GameObject __state)
    {
        if (__state)
        {
            __instance.uiWindows[26] = __state;
            // Reuse the yellow warning sprite from native Marketing's hype-wait
            // message, preserving the generic Waiting panel's icon geometry.
            var marketing = __instance.uiWindows[4].GetComponent<roomWindow>();
            var warning = NativeUi.Require<Image>(marketing.uiObjects[5].transform, "Icon");
            __state.GetComponent<roomWindow>().uiObjects[1].GetComponent<Image>().sprite = warning.sprite;
        }
    }

    private static void AfterWaitingDisplay(roomWindow __instance, taskWait __0)
    {
        var queue = MarketingQueue.ForTask(__0);
        if (!queue || !queue.Waiting)
        {
            return;
        }

        __instance.uiObjects[0].GetComponent<Text>().text =
            $"Waiting for eligible products...\nHype at or below <color=blue>{queue.WaitingThreshold}</color>";
    }

    private static void AfterCampaignDisplay(roomWindow __instance, taskMarketing __3)
    {
        var queue = MarketingQueue.ForTask(__3);
        if (queue)
        {
            __instance.uiObjects[4].SetActive(queue.Running);
        }
    }

    private static void AfterRoomWaitingCheck(roomScript __instance, ref bool __result)
    {
        var queue = MarketingQueue.ForRoom(__instance);
        if (queue && queue.Waiting)
        {
            __result = true; // Native Marketing Support checks this before attempting work.
        }
    }

    private static bool BeforeRoomMenu(roomButtonScript __instance)
    {
        var queue = MarketingQueue.ForRoom(__instance.rS_);
        if (!queue || !queue.Waiting)
        {
            return true;
        }

        // Marketing has no native taskWait context branch. Use its normal active
        // campaign panel so Stop Automation and Cancel Task remain reachable.
        queue.Room.mS_.sfx_.PlaySound(3, true);
        queue.Manager.guiMainButtons_.CloseAllDropdowns();
        var wasOpen = __instance.uiOptions[12].activeSelf;
        queue.Manager.CloseAllRoomButtons();
        __instance.uiOptions[12].SetActive(!wasOpen);
        __instance.uiObjects[2].SetActive(!wasOpen);
        __instance.uiObjects[2].transform.SetParent(__instance.uiOptions[12].transform);
        __instance.transform.SetAsLastSibling();
        queue.Room.mS_.PauseGame(!wasOpen);
        return false;
    }

    private static bool BeforeAutomationToggle(roomButtonScript __instance)
    {
        var queue = MarketingQueue.ForRoom(__instance.rS_);
        if (!queue || !MarketingQueue.ForTask(__instance.rS_.taskGameObject
                ? __instance.rS_.taskGameObject.GetComponent<QueueTaskOwnership>()
                : null))
        {
            return true;
        }

        queue.Room.mS_.sfx_.PlaySound(3, true);
        queue.Stop("Stop Automation was pressed; any paid campaign will finish.");
        __instance.CloseAllMenus();
        queue.Room.mS_.PauseGame(false);
        return false;
    }

    private static bool BeforeAutomationLabel(lockAutomatic __instance)
    {
        if (!__instance.uiRoom)
        {
            return true;
        }

        var buttons = __instance.uiRoom.GetComponent<roomButtonScript>();
        var room = buttons ? buttons.rS_ : null;
        var queue = MarketingQueue.ForRoom(room);
        if (!queue || !room.taskGameObject ||
            !MarketingQueue.ForTask(room.taskGameObject.GetComponent<QueueTaskOwnership>()))
        {
            return true;
        }

        __instance.lockGameObject.SetActive(false);
        __instance.GetComponent<Button>().interactable = queue.Running;
        __instance.transform.GetChild(0).GetComponent<Text>().text = queue.Running
            ? room.mS_.tS_.GetText(168)
            : "Automation stopped";
        return false;
    }

    private static void BeforeCancelConfirmed(roomScript ___rS_)
    {
        var queue = MarketingQueue.ForRoom(___rS_);
        if (queue && queue.Waiting)
        {
            queue.Stop("Cancel Task was confirmed while Waiting.");
        }
    }

    private static void BeforePreferencesRestored(GUI_Main __instance, mainScript ___mS_)
    {
        if (!___mS_ || ___mS_.multiplayer || !___mS_.save_ || !___mS_.save_.loadingSavegame)
        {
            return;
        }

        // InitToggles replays a stored UI preference using Toggle.isOn, which
        // otherwise invokes the same confirmation used for an actual user click.
        // Seed only this toggle silently; preserve its preference and normal clicks.
        __instance.uiObjects[2].GetComponent<Toggle>()
            .SetIsOnWithoutNotify(PlayerPrefs.GetInt("Toggle_PickChars") == 1);
    }

    private static void BeforeLoad(savegameScript __instance, int __0, out QueueLoadCheckpoint __state) =>
        __state = QueuePersistence.BeginLoad(__instance, __0);

    private static void AfterLoad(savegameScript __instance, QueueLoadCheckpoint __state) =>
        QueuePersistence.FinishLoad(__instance, __state);

    private static void BeforeSave(savegameScript __instance, int __0, out QueueSaveCheckpoint __state) =>
        __state = QueuePersistence.BeginSave(__instance, __0);

    private static Exception AfterSave(savegameScript __instance, ES3Writer ___writer, QueueSaveCheckpoint __state,
        Exception __exception)
    {
        QueuePersistence.FinishSave(__instance, ___writer, __state, __exception);
        return __exception; // Preserve native success/failure exactly.
    }
}
