using MarketingAutomation.Automation;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

public sealed class AutomationScreen : MonoBehaviour
{
    private GUI_Main menuManager;
    private ProductListView productList;
    private Button previousPageButton;
    private Button nextPageButton;
    private RectTransform productsPage;
    private RectTransform campaignPage;
    private AutomationCampaignPage campaignControls;
    private Image[] pageDots;
    private tooltip closeShortcut;
    private GameObject validationDialog;
    private int validationDialogSiblingIndex;
    private bool ownsValidationDialog;
    private readonly AutomationFormState form = new();
    private sfxScript soundEffects;
    private roomScript boundRoom;
    private Action started;

    internal void Build(GUI_Main manager, Sprite automationIcon, Action close, Action started)
    {
        menuManager = manager;
        this.started = started;
        var selector = manager.uiObjects[90].GetComponent<Menu_Marketing_SelectGame>();
        var sourceWindow = (RectTransform)selector.uiObjects[2].transform;
        // Preserve the selector's actual frame, inset fill, header geometry, shadow,
        // and opening animation. Do not approximate it using the picker's title fill.
        var window = NativeUi.CopyTree(sourceWindow, transform, manager, child =>
            child.parent != sourceWindow || child.name == "Fill_Window" ||
            child.name == "Fill_Titleleiste" || child.name == "Titelleiste");
        window.name = "WindowMain";
        NativeUi.Require<Text>(window, "Titelleiste/Title").text = "Marketing Automation";
        NativeUi.Require<Image>(window, "Titelleiste/Icon").sprite = automationIcon;
        var closeRoot = NativeUi.Require<RectTransform>(window, "Titelleiste/Button_Close");
        var closeStyle = NativeUi.Require<Button>(sourceWindow, "Titelleiste/Button_Close");
        NativeUi.AddButton(closeRoot, closeStyle).onClick.AddListener(() => close());
        var shortcut = closeRoot.gameObject.AddComponent<tooltip>();
        closeShortcut = shortcut;
        shortcut.shortcut = KeyCode.Escape;
        shortcut.textID = 152;
        shortcut.textArray = "text";

        productsPage = UiFactory.CreateRect("ProductsPage", window);
        UiFactory.Stretch(productsPage);
        campaignPage = UiFactory.CreateRect("CampaignPage", window);
        UiFactory.Stretch(campaignPage);
        campaignPage.gameObject.SetActive(false);
        var textStyle = selector.uiPrefabs[0].GetComponent<Item_Marketing_Game>().uiObjects[2].GetComponent<Text>();
        productList = new ProductListView(productsPage, manager, selector, textStyle);
        campaignControls = campaignPage.gameObject.AddComponent<AutomationCampaignPage>();
        campaignControls.Build(manager, textStyle);
        BuildPageNavigation(window);
    }

    private void BuildPageNavigation(RectTransform window)
    {
        // Menu_DevGame.OpenSide changes child pages within a single animated frame.
        // Copy only its footer visuals, never its development callbacks or data.
        var development = menuManager.uiObjects[56].GetComponent<Menu_DevGame>();
        if (!development)
        {
            throw new InvalidOperationException("Native game-development form template is unavailable.");
        }

        var sourceLeft = NativeUi.Require<Button>(development.transform, "WindowMain/ButtonL");
        var sourceRight = NativeUi.Require<Button>(development.transform, "WindowMain/ButtonR");
        var sourceConfirm = NativeUi.Require<Button>(development.transform, "WindowMain/Button_Yes");
        var sourceDots = development.uiObjects[32].transform;

        var dots = NativeUi.CopyTree(sourceDots, window, menuManager,
            child => child.parent != sourceDots || child.GetSiblingIndex() < 2);
        dots.name = "PageIndicators";
        dots.sizeDelta = new Vector2(50f, 27.5f);
        dots.anchorMin = dots.anchorMax = new Vector2(0.5f, 0f);
        dots.anchoredPosition = new Vector2(-14.9f, 38.6f);
        pageDots = new Image[2];
        for (var index = 0; index < pageDots.Length; index++)
        {
            var page = (AutomationPage)index;
            var dot = (RectTransform)dots.GetChild(index);
            dot.anchoredPosition = new Vector2(12.5f + index * 25f, -13.75f);
            pageDots[index] = dot.GetComponent<Image>();
            NativeUi.AddButton(dot, sourceDots.GetChild(index).GetComponent<Button>())
                .onClick.AddListener(() => OpenPage(page));
            dot.gameObject.AddComponent<tooltip>().c = index == 0 ? "Products" : "Campaign";
        }
        var left = NativeUi.CopyTree(sourceLeft.transform, window, menuManager);
        PlaceFooter(left, new Vector2(0.5f, 0f), new Vector2(-69.9f, 38.6f));
        previousPageButton = NativeUi.AddButton(left, sourceLeft);
        previousPageButton.onClick.AddListener(() => OpenPage(AutomationPage.Products));
        var right = NativeUi.CopyTree(sourceRight.transform, window, menuManager);
        PlaceFooter(right, new Vector2(0.5f, 0f), new Vector2(40.1f, 38.6f));
        nextPageButton = NativeUi.AddButton(right, sourceRight);
        nextPageButton.onClick.AddListener(() => OpenPage(AutomationPage.Campaign));

        var confirm = NativeUi.CopyTree(sourceConfirm.transform, window, menuManager);
        PlaceFooter(confirm, new Vector2(1f, 0f), new Vector2(-37f, 38.3f));
        NativeUi.AddButton(confirm, sourceConfirm).onClick.AddListener(Confirm);
        confirm.gameObject.AddComponent<tooltip>().c = "Confirm automation";
    }

    private static void PlaceFooter(RectTransform rect, Vector2 anchor, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
    }

    private void OpenPage(AutomationPage page)
    {
        if (ownsValidationDialog)
        {
            return;
        }

        form.OpenPage(page);
        RefreshPages();
        soundEffects?.PlaySound(3, true);
    }

    private void RefreshPages()
    {
        productsPage.gameObject.SetActive(form.Page == AutomationPage.Products);
        campaignPage.gameObject.SetActive(form.Page == AutomationPage.Campaign);
        previousPageButton.interactable = form.CanGoBack;
        nextPageButton.interactable = form.CanGoForward;
        for (var index = 0; index < pageDots.Length; index++)
        {
            pageDots[index].color = (int)form.Page == index ? Color.grey : Color.white;
        }
    }

    private void Confirm()
    {
        if (ownsValidationDialog)
        {
            return;
        }

        soundEffects?.PlaySound(3, true);
        var count = productList.SelectedCount;
        var issue = AutomationFormState.Validate(count, campaignControls.HasSelectedCampaign);
        if (issue != AutomationValidation.None)
        {
            form.OpenPage(issue == AutomationValidation.MissingProducts ? AutomationPage.Products : AutomationPage.Campaign);
            RefreshPages();
        }
        string error = null;
        if (issue == AutomationValidation.None)
        {
            var selected = productList.GetSelectedProducts();
            try
            {
                if (MarketingQueue.TryStart(boundRoom, menuManager, selected, campaignControls.SelectedCampaign,
                    campaignControls.SelectedThreshold, campaignControls.SelectedPriority, campaignControls.SelectedDirection, out error))
                {
                    started();
                    return;
                }
            }
            catch (Exception exception)
            {
                MarketingAutomationPlugin.Logger.LogError($"Unable to start Marketing queue: {exception}");
                error = "Marketing automation could not start. Please check the plugin log for details.";
            }
        }
        validationDialog = menuManager.uiObjects[40];
        validationDialogSiblingIndex = validationDialog.transform.GetSiblingIndex();
        ownsValidationDialog = true;
        closeShortcut.enabled = false;
        var message = issue == AutomationValidation.MissingProducts
            ? "Please select at least one product for automation."
            : issue == AutomationValidation.MissingCampaign
                ? "Please select a marketing campaign for automation."
                : error;
        menuManager.MessageBox(message, false);
        // The mod panel was appended to the canvas. Raise the native dialog above it
        // temporarily, then restore the native ordering once the dialog is dismissed.
        validationDialog.transform.SetAsLastSibling();
    }

    private void LateUpdate()
    {
        if (ownsValidationDialog && (!validationDialog || !validationDialog.activeSelf))
        {
            ReleaseValidationDialog();
        }

        productList?.UpdateLayout();
    }

    private void ReleaseValidationDialog()
    {
        if (validationDialog)
        {
            validationDialog.transform.SetSiblingIndex(validationDialogSiblingIndex);
        }

        ownsValidationDialog = false;
        if (closeShortcut)
        {
            closeShortcut.enabled = true;
        }
    }

    internal void Show(roomScript room)
    {
        if (!room || !room.mS_)
        {
            throw new InvalidOperationException("No Marketing room/company is bound to the product screen.");
        }

        boundRoom = room;
        var soundObject = GameObject.Find("SFX");
        soundEffects = soundObject ? soundObject.GetComponent<sfxScript>() : null;
        // Each opening begins a new configuration. Only navigation between the
        // two child windows preserves the screen-owned draft.
        form.Reset();
        campaignControls.Show(room.mS_);
        productList.Show(room.mS_, soundEffects);
        RefreshPages();
        MarketingAutomationPlugin.Logger.LogInfo($"Automation catalog for room {room.myID}: {productList.ProductCount} products.");
    }

    private void OnEnable()
    {
        // Recompute after activation so scroll geometry includes the live canvas.
        if (productList != null)
        {
            productList.Refresh();
            RefreshPages();
        }
    }

    private void OnDisable()
    {
        // X, Escape, cleanup, and parent-menu deactivation all hide this common
        // root. Changing form pages hides only their content containers instead.
        productList?.ResetDraft();
        form.Reset();
        if (campaignControls)
        {
            campaignControls.ResetDraft();
        }

        if (ownsValidationDialog)
        {
            if (validationDialog)
            {
                validationDialog.SetActive(false);
            }

            ReleaseValidationDialog();
        }
    }
}
