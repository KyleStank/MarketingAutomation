using MarketingAutomation.Automation.Runtime;
using MarketingAutomation.GameIntegration.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

// Presentation for queue-owned room tasks. Harmony adapters decide whether a queue owns the interaction.
internal static class QueueRoomPresentation
{
    internal static void ShowWaitingStatus(roomWindow window, MarketingQueue queue)
    {
        new NativeWaitingRoomWindow(window).StatusText.text =
            $"Waiting for eligible products...\nHype at or below <color=blue>{queue.WaitingThreshold}</color>";
    }

    internal static void ShowCampaignAutomation(roomWindow window, MarketingQueue queue)
    {
        new NativeMarketingRoomWindow(window).AutomationIndicator.SetActive(queue.Running);
    }

    internal static void ToggleWaitingRoomMenu(roomButtonScript buttons, MarketingQueue queue)
    {
        // Marketing has no native taskWait context branch. Reuse its active-campaign
        // panel so Stop Automation and Cancel Task remain available while waiting.
        queue.Room.mS_.sfx_.PlaySound(3, true);
        queue.Manager.guiMainButtons_.CloseAllDropdowns();

        var controls = new NativeRoomControls(buttons);
        var wasOpen = controls.ActiveMarketingOptions.activeSelf;
        queue.Manager.CloseAllRoomButtons();

        controls.ActiveMarketingOptions.SetActive(!wasOpen);
        controls.SharedRoomOptions.SetActive(!wasOpen);

        controls.SharedRoomOptions.transform.SetParent(controls.ActiveMarketingOptions.transform);

        buttons.transform.SetAsLastSibling();
        queue.Room.mS_.PauseGame(!wasOpen);
    }

    internal static void RestoreWaitingStatusWindow(roomButtonScript buttons, GameObject waitingWindow)
    {
        var controls = new NativeRoomControls(buttons);
        controls.WaitingStatusWindow = waitingWindow;

        // Reuse native Marketing's yellow hype-wait warning sprite without changing
        // the generic Waiting panel's icon geometry.
        var warning = controls.MarketingStatusWindow.HypeWaitingWarningIcon;
        new NativeWaitingRoomWindow(waitingWindow.GetComponent<roomWindow>()).StatusIcon.sprite = warning.sprite;
    }

    internal static void ShowAutomationControl(lockAutomatic control, roomScript room, MarketingQueue queue)
    {
        control.lockGameObject.SetActive(false);
        control.GetComponent<Button>().interactable = queue.Running;

        control.transform.GetChild(0).GetComponent<Text>().text = queue.Running
            ? room.mS_.tS_.GetText(168)
            : "Automation stopped";
    }
}
