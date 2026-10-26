using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

// One component per native picker; the native Init call refreshes its room on every opening.
public sealed class MarketingMenuExtension : MonoBehaviour
{
    private static readonly HashSet<MarketingMenuExtension> Instances = [];
    private Menu_Marketing_Main _picker;
    private GUI_Main _menuManager;
    private roomScript _room;
    private RectTransform _nativeWindow;
    private Vector2 _originalWindowSize;
    private RectTransform _nativeDescription;
    private Vector2 _originalDescriptionPosition;
    private GameObject _entry;
    private GameObject _panel;
    private AutomationScreen _automationScreen;
    private readonly List<tooltip> _suspendedShortcuts = [];
    private sfxScript _soundEffects;
    private bool _restoreShortcutsAfterUpdate;
    private bool _failed;
    private bool _layoutChanged;

    internal void Bind(Menu_Marketing_Main menu, roomScript selectedRoom)
    {
        _room = selectedRoom;

        if (_failed)
            return;

        try
        {
            if (!_entry)
            {
                _picker = menu;
                Instances.Add(this);
                Build();
            }
        }
        catch (Exception exception)
        {
            _failed = true;
            Cleanup();
            MarketingAutomationPlugin.Logger.LogError(
                $"Automation UI unavailable; native Marketing choices retained: {exception}");
        }
    }

    private void Build()
    {
        _menuManager = _picker.GetComponentInParent<GUI_Main>();
        if (!_menuManager)
            throw new InvalidOperationException("Marketing picker has no GUI_Main ancestor.");

        // Native hierarchy names: Menue means menu; Konsolen means consoles.
        // Paths verified against scene01.unity, Marketing root GO11143 / component92398.
        _nativeWindow = Require<RectTransform>(_picker.transform, "Menue");
        _nativeDescription = Require<RectTransform>(_nativeWindow, "Text");
        var gameButton = Require<Button>(_nativeWindow, "Button_Games");
        var consoleButton = Require<Button>(_nativeWindow, "Button_Konsolen");

        var soundObject = GameObject.Find("SFX");
        _soundEffects = soundObject ? soundObject.GetComponent<sfxScript>() : null;

        var overlay = _picker.GetComponent<Image>();
        if (!overlay || !gameButton.image)
            throw new InvalidOperationException("Marketing picker image bindings are incomplete.");

        _originalWindowSize = _nativeWindow.sizeDelta;
        _originalDescriptionPosition = _nativeDescription.anchoredPosition;

        BuildAutomationPanel(overlay);
        var spacing = AddAutomationEntry(gameButton, consoleButton);
        ExpandNativePicker(spacing);
    }

    private static T Require<T>(Transform root, string path) where T : Component
    {
        var child = root.Find(path);
        var component = child ? child.GetComponent<T>() : null;
        return !component
            ? throw new InvalidOperationException($"Missing Marketing UI binding: {path} ({typeof(T).Name}).")
            : component;
    }

    private void BuildAutomationPanel(Image overlay)
    {
        _panel = UiFactory.CreateRect("MarketingAutomationPanel", _picker.transform.parent).gameObject;
        _panel.SetActive(false);
        UiFactory.Stretch((RectTransform)_panel.transform);

        var panelImage = UiFactory.CopyImage(overlay, _panel);

        // Init may run before the native Fill_Window.Start initializes its material.
        panelImage.material = _menuManager.matFill_Window;

        _automationScreen = _panel.AddComponent<AutomationScreen>();
        _automationScreen.Build(_menuManager, FindAutomationIcon(), ReturnToCampaignPicker, CloseAfterAutomationStarted);
    }

    private Sprite FindAutomationIcon()
    {
        // Search the template's serialized images, including inactive task-specific panels.
        var roomUi = new NativeRoomUi(_room);
        var controlsPrefab = roomUi.ControlsPrefab;
        if (controlsPrefab)
        {
            foreach (var image in controlsPrefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite && image.sprite.name == "iconAutomatic")
                    return image.sprite;
            }
        }

        throw new InvalidOperationException("Native room controls have no iconAutomatic sprite.");
    }

    private float AddAutomationEntry(Button gameButton, Button consoleButton)
    {
        // New controls copy visual properties only: no native callbacks or label scripts.
        var spacing = Mathf.Abs(((RectTransform)consoleButton.transform).anchoredPosition.y -
                                ((RectTransform)gameButton.transform).anchoredPosition.y);
        var entryButton = UiFactory.CreateMenuEntry("MarketingAutomationEntry", _nativeWindow, gameButton,
            "Automation", FindAutomationIcon());
        _entry = entryButton.gameObject;
        ((RectTransform)_entry.transform).anchoredPosition =
            ((RectTransform)consoleButton.transform).anchoredPosition - new Vector2(0f, spacing);
        entryButton.onClick.AddListener(OpenAutomationForm);

        return spacing;
    }

    private void ExpandNativePicker(float spacing)
    {
        _layoutChanged = true;
        _nativeWindow.sizeDelta = _originalWindowSize + new Vector2(0f, spacing);

        // Preserve the original description's distance from the top of the enlarged picker.
        _nativeDescription.anchoredPosition = _originalDescriptionPosition + new Vector2(0f, spacing / 2f);
    }

    private void OpenAutomationForm()
    {
        if (!_room || !_menuManager || !_panel || _panel.activeSelf)
            return;

        try
        {
            _automationScreen.Show(_room);
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogError($"Unable to open Automation product catalog: {exception}");
            return;
        }

        RestoreShortcuts();
        _panel.transform.SetAsLastSibling();

        // Keep the picker underneath, as the native game/console actions do. Its Escape
        // handler must not compete with the child menu for the same key release.
        foreach (var shortcut in _picker.GetComponentsInChildren<tooltip>(true))
        {
            if (shortcut.enabled && shortcut.shortcut != KeyCode.None)
            {
                _suspendedShortcuts.Add(shortcut);
                shortcut.enabled = false;
            }
        }

        _soundEffects?.PlaySound(3, true);
        _menuManager.ActivateMenu(_panel);
    }

    private void RestoreShortcuts()
    {
        _restoreShortcutsAfterUpdate = false;

        foreach (var shortcut in _suspendedShortcuts)
        {
            if (shortcut)
                shortcut.enabled = true;
        }

        _suspendedShortcuts.Clear();
    }

    private void ReturnToCampaignPicker()
    {
        _panel.SetActive(false);

        // Restore after all native tooltip Update calls, so this Escape release
        // cannot also close the underlying picker during the same frame.
        _restoreShortcutsAfterUpdate = true;
        _soundEffects?.PlaySound(3, true);

        // The existing modal stays open, preserving the game's pause/menu state.
    }

    private void CloseAfterAutomationStarted()
    {
        _panel.SetActive(false);
        RestoreShortcuts();
        _picker.gameObject.SetActive(false);
        _menuManager.CloseMenu();
    }

    private void LateUpdate()
    {
        if (_restoreShortcutsAfterUpdate)
            RestoreShortcuts();
    }

    internal static void RemoveAll()
    {
        foreach (var extension in new List<MarketingMenuExtension>(Instances))
        {
            if (!extension)
                continue;

            extension.Cleanup();
            Destroy(extension);
        }

        Instances.Clear();
    }

    private void Cleanup(bool closeModal = true)
    {
        if (_panel)
        {
            if (closeModal && _panel.activeSelf && _menuManager && (!_picker || !_picker.gameObject.activeInHierarchy))
                _menuManager.CloseMenu();

            _panel.SetActive(false);
            Destroy(_panel);
        }

        RestoreShortcuts();

        if (_entry)
        {
            _entry.SetActive(false);
            Destroy(_entry);
        }

        if (_layoutChanged && _nativeWindow)
        {
            _nativeWindow.sizeDelta = _originalWindowSize;
            if (_nativeDescription)
                _nativeDescription.anchoredPosition = _originalDescriptionPosition;

            _layoutChanged = false;
        }
    }

    private void OnDestroy()
    {
        // Scene destruction must not call back into a partly destroyed native GUI.
        Cleanup(false);
        Instances.Remove(this);
    }
}
