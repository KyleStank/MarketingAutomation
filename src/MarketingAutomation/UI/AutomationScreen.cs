using MarketingAutomation.Automation.Runtime;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

public sealed class AutomationScreen : MonoBehaviour
{
    private GUI_Main _menuManager;
    private NativeGameUi _gameUi;
    private roomScript _boundRoom;
    private sfxScript _soundEffects;
    private Action _started;

    private readonly AutomationFormState _form = new();
    private ProductListView _productList;
    private AutomationCampaignPage _campaignControls;
    private RectTransform _productsPage;
    private RectTransform _campaignPage;

    private Button _previousPageButton;
    private Button _nextPageButton;
    private Image[] _pageDots;
    private tooltip _closeShortcut;

    private GameObject _validationDialog;
    private int _validationDialogSiblingIndex;
    private bool _ownsValidationDialog;

    internal void Build(GUI_Main manager, Sprite automationIcon, Action close, Action started)
    {
        _menuManager = manager;
        _gameUi = new NativeGameUi(manager);
        _started = started;

        var selector = new NativeMarketingSelectorUi(_gameUi.MarketingGameSelector);
        var window = BuildWindowFrame(selector, automationIcon, close);
        BuildPages(window, selector);
        BuildPageNavigation(window);
    }

    private RectTransform BuildWindowFrame(NativeMarketingSelectorUi selector, Sprite automationIcon, Action close)
    {
        var sourceWindow = selector.Window;

        // Titelleiste is the title bar; Fill_Titleleiste is its background fill.
        // Preserve the selector's actual frame, inset fill, header geometry, shadow,
        // and opening animation. Do not approximate it using the picker's title fill.
        var window = NativeVisualTree.Copy(sourceWindow, transform, _menuManager, child =>
            child.parent != sourceWindow || child.name == "Fill_Window" ||
            child.name == "Fill_Titleleiste" || child.name == "Titelleiste");
        window.name = "WindowMain";

        NativeUiLookup.RequireComponent<Text>(window, "Titelleiste/Title").text = "Marketing Automation";
        NativeUiLookup.RequireComponent<Image>(window, "Titelleiste/Icon").sprite = automationIcon;

        var closeRoot = NativeUiLookup.RequireComponent<RectTransform>(window, "Titelleiste/Button_Close");
        var closeStyle = NativeUiLookup.RequireComponent<Button>(sourceWindow, "Titelleiste/Button_Close");
        UiFactory.AddButton(closeRoot, closeStyle).onClick.AddListener(() => close());

        var shortcut = closeRoot.gameObject.AddComponent<tooltip>();
        _closeShortcut = shortcut;
        shortcut.shortcut = KeyCode.Escape;
        shortcut.textID = 152;
        shortcut.textArray = "text";

        return window;
    }

    private void BuildPages(RectTransform window, NativeMarketingSelectorUi selector)
    {
        _productsPage = UiFactory.CreateRect("ProductsPage", window);
        UiFactory.Stretch(_productsPage);

        _campaignPage = UiFactory.CreateRect("CampaignPage", window);
        UiFactory.Stretch(_campaignPage);
        _campaignPage.gameObject.SetActive(false);

        var rowUi = new NativeMarketingRowUi(selector.ProductRowPrefab.GetComponent<Item_Marketing_Game>());
        var textStyle = rowUi.ReleaseDateLabel;

        _productList = new ProductListView(_productsPage, _menuManager, selector, textStyle);
        _campaignControls = _campaignPage.gameObject.AddComponent<AutomationCampaignPage>();
        _campaignControls.Build(_menuManager, textStyle);
    }

    private void BuildPageNavigation(RectTransform window)
    {
        // Menu_DevGame.OpenSide changes child pages within a single animated frame.
        // Copy only its footer visuals, never its development callbacks or data.
        var development = _gameUi.GameDevelopmentMenu;
        if (!development)
            throw new InvalidOperationException("Native game-development form template is unavailable.");

        var sourceLeft = NativeUiLookup.RequireComponent<Button>(development.transform, "WindowMain/ButtonL");
        var sourceRight = NativeUiLookup.RequireComponent<Button>(development.transform, "WindowMain/ButtonR");
        var sourceConfirm = NativeUiLookup.RequireComponent<Button>(development.transform, "WindowMain/Button_Yes");
        var developmentUi = new NativeGameDevelopmentUi(development);
        var sourceDots = developmentUi.PageIndicators;

        BuildPageIndicators(window, sourceDots);

        var left = NativeVisualTree.Copy(sourceLeft.transform, window, _menuManager);
        PlaceFooter(left, new Vector2(0.5f, 0f), new Vector2(-69.9f, 38.6f));
        _previousPageButton = UiFactory.AddButton(left, sourceLeft);
        _previousPageButton.onClick.AddListener(() => OpenPage(AutomationPage.Products));

        var right = NativeVisualTree.Copy(sourceRight.transform, window, _menuManager);
        PlaceFooter(right, new Vector2(0.5f, 0f), new Vector2(40.1f, 38.6f));
        _nextPageButton = UiFactory.AddButton(right, sourceRight);
        _nextPageButton.onClick.AddListener(() => OpenPage(AutomationPage.Campaign));

        var confirm = NativeVisualTree.Copy(sourceConfirm.transform, window, _menuManager);
        PlaceFooter(confirm, new Vector2(1f, 0f), new Vector2(-37f, 38.3f));
        UiFactory.AddButton(confirm, sourceConfirm).onClick.AddListener(Confirm);
        confirm.gameObject.AddComponent<tooltip>().c = "Confirm automation";
    }

    private void BuildPageIndicators(RectTransform window, Transform sourceDots)
    {
        var dots = NativeVisualTree.Copy(sourceDots, window, _menuManager,
            child => child.parent != sourceDots || child.GetSiblingIndex() < 2);
        dots.name = "PageIndicators";
        dots.sizeDelta = new Vector2(50f, 27.5f);
        dots.anchorMin = dots.anchorMax = new Vector2(0.5f, 0f);
        dots.anchoredPosition = new Vector2(-14.9f, 38.6f);

        _pageDots = new Image[2];
        for (var index = 0; index < _pageDots.Length; index++)
        {
            var page = (AutomationPage)index;
            var dot = (RectTransform)dots.GetChild(index);
            dot.anchoredPosition = new Vector2(12.5f + index * 25f, -13.75f);
            _pageDots[index] = dot.GetComponent<Image>();
            UiFactory.AddButton(dot, sourceDots.GetChild(index).GetComponent<Button>())
                .onClick.AddListener(() => OpenPage(page));
            dot.gameObject.AddComponent<tooltip>().c = index == 0 ? "Products" : "Campaign";
        }
    }

    private static void PlaceFooter(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
    }

    private void OpenPage(AutomationPage page)
    {
        if (_ownsValidationDialog)
            return;

        _form.OpenPage(page);
        RefreshPages();
        _soundEffects?.PlaySound(3, true);
    }

    private void RefreshPages()
    {
        _productsPage.gameObject.SetActive(_form.Page == AutomationPage.Products);
        _campaignPage.gameObject.SetActive(_form.Page == AutomationPage.Campaign);
        _previousPageButton.interactable = _form.CanGoBack;
        _nextPageButton.interactable = _form.CanGoForward;

        for (var index = 0; index < _pageDots.Length; index++)
        {
            _pageDots[index].color = (int)_form.Page == index ? Color.grey : Color.white;
        }
    }

    private void Confirm()
    {
        if (_ownsValidationDialog)
            return;

        _soundEffects?.PlaySound(3, true);
        var issue = ValidateAndRevealIncompletePage();

        string error = null;
        if (issue == AutomationValidation.None && TryStartConfirmedQueue(out error))
            return;

        ShowValidationDialog(issue, error);
    }

    private AutomationValidation ValidateAndRevealIncompletePage()
    {
        var issue = AutomationFormState.Validate(_productList.SelectedCount, _campaignControls.HasSelectedCampaign);
        if (issue == AutomationValidation.None)
            return issue;

        _form.OpenPage(issue == AutomationValidation.MissingProducts
            ? AutomationPage.Products
            : AutomationPage.Campaign);
        RefreshPages();
        return issue;
    }

    private bool TryStartConfirmedQueue(out string error)
    {
        var selected = _productList.GetSelectedProducts();

        try
        {
            if (MarketingQueue.TryStart(_boundRoom, _menuManager, selected, _campaignControls.SelectedCampaign,
                    _campaignControls.SelectedThreshold, _campaignControls.SelectedPriority,
                    _campaignControls.SelectedDirection, out error))
            {
                _started();
                return true;
            }
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogError($"Unable to start Marketing queue: {exception}");
            error = "Marketing automation could not start. Please check the plugin log for details.";
        }

        return false;
    }

    private void ShowValidationDialog(AutomationValidation issue, string startError)
    {
        _validationDialog = _gameUi.MessageDialog;
        _validationDialogSiblingIndex = _validationDialog.transform.GetSiblingIndex();
        _ownsValidationDialog = true;
        _closeShortcut.enabled = false;

        var message = issue switch
        {
            AutomationValidation.MissingProducts => "Please select at least one product for automation.",
            AutomationValidation.MissingCampaign => "Please select a marketing campaign for automation.",
            _ => startError
        };
        _gameUi.ShowMessage(message);

        // The mod panel was appended to the canvas. Raise the native dialog above it
        // temporarily, then restore the native ordering once the dialog is dismissed.
        _validationDialog.transform.SetAsLastSibling();
    }

    private void LateUpdate()
    {
        if (_ownsValidationDialog && (!_validationDialog || !_validationDialog.activeSelf))
            ReleaseValidationDialog();

        _productList?.ApplyPendingScrollReset();
    }

    private void ReleaseValidationDialog()
    {
        if (_validationDialog)
            _validationDialog.transform.SetSiblingIndex(_validationDialogSiblingIndex);

        _ownsValidationDialog = false;

        if (_closeShortcut)
            _closeShortcut.enabled = true;
    }

    internal void Show(roomScript room)
    {
        if (!room || !room.mS_)
            throw new InvalidOperationException("No Marketing room/company is bound to the product screen.");

        _boundRoom = room;
        var soundObject = GameObject.Find("SFX");
        _soundEffects = soundObject ? soundObject.GetComponent<sfxScript>() : null;

        // Each opening begins a new configuration. Only navigation between the
        // two child windows preserves the screen-owned draft.
        _form.Reset();
        _campaignControls.Show(room.mS_);
        _productList.Show(room.mS_, _soundEffects);
        RefreshPages();

        MarketingAutomationPlugin.Logger.LogInfo(
            $"Automation catalog for room {room.myID}: {_productList.ProductCount} products.");
    }

    private void OnEnable()
    {
        // Recompute after activation so scroll geometry includes the live canvas.
        if (_productList != null)
        {
            _productList.RefreshFilteredProducts();
            RefreshPages();
        }
    }

    private void OnDisable()
    {
        // X, Escape, cleanup, and parent-menu deactivation all hide this common
        // root. Changing form pages hides only their content containers instead.
        _productList?.ResetDraft();
        _form.Reset();
        if (_campaignControls)
            _campaignControls.ResetDraft();

        if (_ownsValidationDialog)
        {
            if (_validationDialog)
                _validationDialog.SetActive(false);

            ReleaseValidationDialog();
        }
    }
}
