using HarmonyLib;
using MarketingAutomation.Automation.Runtime;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI;
using UnityEngine;

namespace MarketingAutomation.GameIntegration;

// Native terminology: Abbrechen = cancel; Aufgabe = task; typ = room type.
// Menu_W_Aufgabe_Abbrechen is the native task-cancellation confirmation dialog.
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
    }

    // ReSharper disable once InconsistentNaming
    private static void AfterCampaignCompleted(taskMarketing __instance) =>
        MarketingQueue.ForTask(__instance)?.RecordCampaignCompletion();

    // ReSharper disable once InconsistentNaming
    private static void BeforeCampaignCanceled(taskMarketing __instance) =>
        MarketingQueue.ForTask(__instance)?.Stop("The native campaign was canceled.");

    // ReSharper disable once InconsistentNaming
    private static void BeforeWaitingCanceled(taskWait __instance) =>
        MarketingQueue.ForTask(__instance)?.Stop("Waiting was canceled.");

    // ReSharper disable once InconsistentNaming
    private static void AfterWaitingDisplay(roomWindow __instance, taskWait __0)
    {
        var queue = MarketingQueue.ForTask(__0);
        if (!queue || !queue.Waiting)
        {
            return;
        }

        QueueRoomPresentation.ShowWaitingStatus(__instance, queue);
    }

    // ReSharper disable once InconsistentNaming
    private static void AfterCampaignDisplay(roomWindow __instance, taskMarketing __3)
    {
        var queue = MarketingQueue.ForTask(__3);
        if (queue)
        {
            QueueRoomPresentation.ShowCampaignAutomation(__instance, queue);
        }
    }

    // ReSharper disable once InconsistentNaming
    private static void AfterRoomWaitingCheck(roomScript __instance, ref bool __result)
    {
        var queue = MarketingQueue.ForRoom(__instance);
        if (queue && queue.Waiting)
        {
            __result = true; // Native Marketing Support checks this before attempting work.
        }
    }

    // ReSharper disable once InconsistentNaming
    private static bool BeforeRoomMenu(roomButtonScript __instance)
    {
        var queue = MarketingQueue.ForRoom(__instance.rS_);
        if (!queue || !queue.Waiting)
        {
            return true;
        }

        QueueRoomPresentation.ToggleWaitingRoomMenu(__instance, queue);
        return false;
    }

    // ReSharper disable once InconsistentNaming
    private static void BeforeRoomPanelsPruned(roomButtonScript __instance, out GameObject __state)
    {
        __state = null;

        var room = __instance.rS_;
        var controls = new NativeRoomControls(__instance);
        if (!room || room.typ != 6 || !room.mS_ || room.mS_.multiplayer ||
            !controls.HasWaitingWindowSlot)
        {
            return;
        }

        // Native Marketing cleanup destroys the generic Waiting window. Temporarily hide its
        // reference from that cleanup so the normal UpdateWindowWait path works.
        __state = controls.WaitingStatusWindow;
        controls.WaitingStatusWindow = null;
    }

    // ReSharper disable once InconsistentNaming
    private static void AfterRoomPanelsPruned(roomButtonScript __instance, GameObject __state)
    {
        if (__state)
        {
            QueueRoomPresentation.RestoreWaitingStatusWindow(__instance, __state);
        }
    }

    // ReSharper disable once InconsistentNaming
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

    // ReSharper disable once InconsistentNaming
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

        QueueRoomPresentation.ShowAutomationControl(__instance, room, queue);
        return false;
    }

    // ReSharper disable once InconsistentNaming
    private static void BeforeCancelConfirmed(roomScript ___rS_)
    {
        var queue = MarketingQueue.ForRoom(___rS_);
        if (queue && queue.Waiting)
        {
            queue.Stop("Cancel Task was confirmed while Waiting.");
        }
    }
}
