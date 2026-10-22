using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

// One component per native picker; the native Init call refreshes its room on every opening.
public sealed class MarketingMenuExtension : MonoBehaviour
{
    private static readonly HashSet<MarketingMenuExtension> Instances = new();
    private Menu_Marketing_Main picker;
    private GUI_Main menuManager;
    private roomScript room;
    private RectTransform nativeWindow;
    private Vector2 originalWindowSize;
    private RectTransform nativeDescription;
    private Vector2 originalDescriptionPosition;
    private GameObject entry;
    private GameObject panel;
    private AutomationScreen automationScreen;
    private readonly List<tooltip> suspendedShortcuts = new();
    private sfxScript soundEffects;
    private bool restoreShortcutsAfterUpdate;
    private bool failed;
    private bool layoutChanged;

    internal void Bind(Menu_Marketing_Main menu, roomScript selectedRoom)
    {
        room = selectedRoom;
        if (failed)
        {
            return;
        }

        try
        {
            if (!entry)
            {
                picker = menu;
                Instances.Add(this);
                Build();
                MarketingAutomationPlugin.Logger.LogInfo(
                    "Automation entry and panel created for the Marketing picker.");
            }
        }
        catch (Exception exception)
        {
            failed = true;
            Cleanup();
            MarketingAutomationPlugin.Logger.LogError(
                $"Automation UI unavailable; native Marketing choices retained: {exception}");
        }
    }

    private void Build()
    {
        menuManager = picker.GetComponentInParent<GUI_Main>();
        if (!menuManager)
        {
            throw new InvalidOperationException("Marketing picker has no GUI_Main ancestor.");
        }

        // Paths verified against scene01.unity, Marketing root GO11143 / component92398.
        nativeWindow = Require<RectTransform>(picker.transform, "Menue");
        var gameButton = Require<Button>(nativeWindow, "Button_Games");
        var consoleButton = Require<Button>(nativeWindow, "Button_Konsolen");
        var soundObject = GameObject.Find("SFX");
        soundEffects = soundObject ? soundObject.GetComponent<sfxScript>() : null;
        nativeDescription = Require<RectTransform>(nativeWindow, "Text");
        var overlay = picker.GetComponent<Image>();
        if (!overlay || !gameButton.image)
        {
            throw new InvalidOperationException("Marketing picker image bindings are incomplete.");
        }

        originalWindowSize = nativeWindow.sizeDelta;
        originalDescriptionPosition = nativeDescription.anchoredPosition;
        panel = UiFactory.CreateRect("MarketingAutomationPanel", picker.transform.parent).gameObject;
        panel.SetActive(false);
        UiFactory.Stretch((RectTransform)panel.transform);
        var panelImage = UiFactory.CopyImage(overlay, panel);
        // Init may run before the native Fill_Window.Start initializes its material.
        panelImage.material = menuManager.matFill_Window;

        automationScreen = panel.AddComponent<AutomationScreen>();
        automationScreen.Build(menuManager, FindAutomationIcon(), Back, Started);

        // New controls copy visual properties only: no native callbacks or label scripts.
        var spacing = Mathf.Abs(((RectTransform)consoleButton.transform).anchoredPosition.y -
                                ((RectTransform)gameButton.transform).anchoredPosition.y);
        var entryButton = UiFactory.CreateMenuEntry("MarketingAutomationEntry", nativeWindow, gameButton,
            "Automation", FindAutomationIcon());
        entry = entryButton.gameObject;
        ((RectTransform)entry.transform).anchoredPosition =
            ((RectTransform)consoleButton.transform).anchoredPosition - new Vector2(0f, spacing);
        entryButton.onClick.AddListener(Open);

        layoutChanged = true;
        nativeWindow.sizeDelta = originalWindowSize + new Vector2(0f, spacing);
        // Preserve the original description's distance from the top of the enlarged picker.
        nativeDescription.anchoredPosition = originalDescriptionPosition + new Vector2(0f, spacing / 2f);
    }

    private void Open()
    {
        if (!room || !menuManager || !panel || panel.activeSelf)
        {
            return;
        }

        try
        {
            automationScreen.Show(room);
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogError($"Unable to open Automation product catalog: {exception}");
            return;
        }

        RestoreShortcuts();
        panel.transform.SetAsLastSibling();
        // Keep the picker underneath, as BUTTON_Game/BUTTON_Konsole do. Its Escape
        // handler must not compete with the child menu for the same key release.
        foreach (var shortcut in picker.GetComponentsInChildren<tooltip>(true))
        {
            if (shortcut.enabled && shortcut.shortcut != KeyCode.None)
            {
                suspendedShortcuts.Add(shortcut);
                shortcut.enabled = false;
            }
        }

        soundEffects?.PlaySound(3, true);
        menuManager.ActivateMenu(panel);
        MarketingAutomationPlugin.Logger.LogInfo($"Opened Automation for Marketing room {room.myID}.");
    }

    private void Back()
    {
        panel.SetActive(false);
        // Restore after all native tooltip Update calls, so this Escape release
        // cannot also close the underlying picker during the same frame.
        restoreShortcutsAfterUpdate = true;
        soundEffects?.PlaySound(3, true);
        // The existing modal stays open, preserving the game's pause/menu state.
    }

    private void Started()
    {
        panel.SetActive(false);
        RestoreShortcuts();
        picker.gameObject.SetActive(false);
        menuManager.CloseMenu();
    }

    private void RestoreShortcuts()
    {
        restoreShortcutsAfterUpdate = false;
        foreach (var shortcut in suspendedShortcuts)
        {
            if (shortcut)
            {
                shortcut.enabled = true;
            }
        }

        suspendedShortcuts.Clear();
    }

    private void LateUpdate()
    {
        if (restoreShortcutsAfterUpdate)
        {
            RestoreShortcuts();
        }
    }

    private static T Require<T>(Transform root, string path) where T : Component
    {
        var child = root.Find(path);
        var component = child ? child.GetComponent<T>() : null;
        if (!component)
        {
            throw new InvalidOperationException($"Missing Marketing UI binding: {path} ({typeof(T).Name}).");
        }

        return component;
    }

    private Sprite FindAutomationIcon()
    {
        // roomScript.InitUI uses uiObjects[0] as the native room-control prefab.
        // Search its serialized images, including inactive task-specific panels.
        if (room.uiObjects != null && room.uiObjects.Length > 0 && room.uiObjects[0])
        {
            foreach (var image in room.uiObjects[0].GetComponentsInChildren<Image>(true))
            {
                if (image.sprite && image.sprite.name == "iconAutomatic")
                {
                    return image.sprite;
                }
            }
        }

        throw new InvalidOperationException("Native room controls have no iconAutomatic sprite.");
    }

    internal static void RemoveAll()
    {
        foreach (var extension in new List<MarketingMenuExtension>(Instances))
        {
            if (!extension)
            {
                continue;
            }

            extension.Cleanup();
            Destroy(extension);
        }

        Instances.Clear();
    }

    private void Cleanup(bool closeModal = true)
    {
        if (panel)
        {
            if (closeModal && panel.activeSelf && menuManager && (!picker || !picker.gameObject.activeInHierarchy))
            {
                menuManager.CloseMenu();
            }

            panel.SetActive(false);
            Destroy(panel);
        }

        RestoreShortcuts();
        if (entry)
        {
            entry.SetActive(false);
            Destroy(entry);
        }

        if (layoutChanged && nativeWindow)
        {
            nativeWindow.sizeDelta = originalWindowSize;
            if (nativeDescription)
            {
                nativeDescription.anchoredPosition = originalDescriptionPosition;
            }

            layoutChanged = false;
        }
    }

    private void OnDestroy()
    {
        // Scene destruction must not call back into a partly destroyed native GUI.
        Cleanup(false);
        Instances.Remove(this);
    }
}
