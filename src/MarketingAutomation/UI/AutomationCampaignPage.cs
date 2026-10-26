using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration.Marketing;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

// Draft configuration. Native campaign menus remain untouched and are never initialized.
public sealed class AutomationCampaignPage : MonoBehaviour
{
    private readonly CampaignConfiguration _configuration = new();
    private readonly CampaignCard[] _cards = new CampaignCard[6];

    private GUI_Main _menuManager;
    private NativeGameUi _gameUi;

    private Slider _threshold;
    private Dropdown _priorityField;
    private Dropdown _priorityDirection;
    private Text _priorityExplanation;
    private Text _thresholdValue;
    private Text _thresholdRange;
    private Text _thresholdExplanation;

    private NativeCampaignOption[] _options;
    private sfxScript _soundEffects;

    internal bool HasSelectedCampaign => _configuration.HasSelection && _options != null &&
                                         _options[_configuration.SelectedCampaign].Unlocked;

    internal int SelectedThreshold => _configuration.Threshold ?? 0;

    internal int SelectedCampaign => _configuration.SelectedCampaign;

    internal QueuePriority SelectedPriority => _configuration.Priority;

    internal QueueDirection SelectedDirection => _configuration.Direction;

    internal void Build(GUI_Main manager, Text textStyle)
    {
        _menuManager = manager;
        _gameUi = new NativeGameUi(manager);

        BuildPriorityControls(textStyle);
        BuildCampaignCards(textStyle);
        BuildThresholdControls(textStyle);
    }

    private void BuildPriorityControls(Text textStyle)
    {
        var heading = UiFactory.CreateText("QueuePriorityHeading", transform, textStyle, "Queue priority",
            new Vector2(0f, -88f), new Vector2(550f, 22f));
        heading.resizeTextMaxSize = 18;

        var selectorUi = new NativeMarketingSelectorUi(_gameUi.MarketingGameSelector);
        var donor = selectorUi.SortDropdown;
        _priorityField = DropdownFactory.Create(donor, transform, _menuManager, "QueuePriority",
            ProductSortOptions.Labels());
        UiFactory.Place((RectTransform)_priorityField.transform, new Vector2(-140f, -118f), new Vector2(270f, 32f));

        _priorityDirection = DropdownFactory.Create(donor, transform, _menuManager, "QueueDirection",
            ["Ascending", "Descending"]);
        UiFactory.Place((RectTransform)_priorityDirection.transform, new Vector2(140f, -118f), new Vector2(270f, 32f));

        _priorityExplanation = UiFactory.CreateText("QueueExplanation", transform, textStyle, "",
            new Vector2(0f, -165f), new Vector2(550f, 54f));
        _priorityExplanation.resizeTextMaxSize = 14;
        _priorityExplanation.supportRichText = false;

        _priorityField.onValueChanged.AddListener(_ => UpdatePriority());
        _priorityDirection.onValueChanged.AddListener(_ => UpdatePriority());
    }

    private void UpdatePriority()
    {
        _configuration.SetPriority(ProductSortOptions.FromIndex(_priorityField.value),
            (QueueDirection)_priorityDirection.value);
        _priorityExplanation.text = QueuePriorityText.Describe(_configuration.Priority, _configuration.Direction);
    }

    private void BuildCampaignCards(Text textStyle)
    {
        // The native menu supplies visuals, not draft state.
        var templates = _gameUi.GameMarketingMenu;
        var campaignUi = new NativeCampaignMenuUi(templates);
        var panelStyle = NativeUiLookup.RequireComponent<RectTransform>(templates.transform, "WindowMain/PanelButtons");

        // Reuse the native group's image, outline, and shadow without its grid
        // layout or children; the custom campaign cards keep their own geometry.
        var background = NativeVisualTree.Copy(panelStyle, transform, _menuManager, _ => false);
        background.name = "CampaignBackground";
        UiFactory.Place(background, new Vector2(0f, -366f), new Vector2(578f, 284f));
        background.GetComponent<Image>().raycastTarget = false;

        var heading = UiFactory.CreateText("CampaignHeading", transform, textStyle, "Choose a marketing campaign",
            new Vector2(0f, -208f), new Vector2(550f, 26f));
        heading.resizeTextMaxSize = 18;

        for (var index = 0; index < _cards.Length; index++)
        {
            var style = campaignUi.GetCampaignButton(index);
            _cards[index] = new CampaignCard(style, transform, _menuManager, index, SelectCampaign);
        }
    }

    private void BuildThresholdControls(Text textStyle)
    {
        _thresholdValue = UiFactory.CreateText("ThresholdValue", transform, textStyle, "",
            new Vector2(0f, -529f), new Vector2(550f, 26f));
        _thresholdValue.resizeTextMaxSize = 18;
        BuildThresholdSlider();

        _thresholdRange = UiFactory.CreateText("ThresholdRange", transform, textStyle, "",
            new Vector2(0f, -586f), new Vector2(530f, 22f));
        _thresholdRange.resizeTextMaxSize = 14;

        _thresholdExplanation = UiFactory.CreateText("ThresholdExplanation", transform, textStyle, "",
            new Vector2(0f, -623f), new Vector2(550f, 46f));
        _thresholdExplanation.resizeTextMaxSize = 14;
        _thresholdExplanation.supportRichText = false;
    }

    private void BuildThresholdSlider()
    {
        // Menu_Abo_Preis is the native subscription-price menu; only its slider visuals are reused.
        var donor = _menuManager.GetComponentInChildren<Menu_Abo_Preis>(true);
        if (!donor)
            throw new InvalidOperationException("Native price-slider template is unavailable.");

        var priceUi = new NativeSubscriptionPriceUi(donor);
        var source = priceUi.PriceSlider;

        var root = NativeVisualTree.Copy(source.transform, transform, _menuManager);
        root.name = "HypeThreshold";
        UiFactory.Place(root, new Vector2(0f, -560f), new Vector2(520f, 24f));

        _threshold = root.gameObject.AddComponent<Slider>();
        _threshold.fillRect = NativeUiLookup.RequireComponent<RectTransform>(root, "Fill Area/Fill");
        _threshold.handleRect = NativeUiLookup.RequireComponent<RectTransform>(root, "Handle Slide Area/Handle");
        _threshold.targetGraphic = _threshold.handleRect.GetComponent<Image>();
        _threshold.direction = source.direction;
        _threshold.colors = source.colors;
        _threshold.navigation = new Navigation { mode = Navigation.Mode.None };

        _threshold.minValue = 0;
        _threshold.maxValue = 1;
        _threshold.wholeNumbers = true;
        _threshold.interactable = false;

        _threshold.onValueChanged.AddListener(value =>
        {
            _configuration.SetThreshold(Mathf.RoundToInt(value));
            RefreshThresholdLabels();
        });
    }

    internal void Show(mainScript currentCompany)
    {
        _options = NativeCampaignCatalog.Read(_menuManager, currentCompany);

        var soundObject = GameObject.Find("SFX");
        _soundEffects = soundObject ? soundObject.GetComponent<sfxScript>() : null;

        _configuration.Reset();
        _priorityField.SetValueWithoutNotify(ProductSortOptions.IndexOf(_configuration.Priority));
        _priorityDirection.SetValueWithoutNotify((int)_configuration.Direction);
        _priorityExplanation.text = QueuePriorityText.Describe(_configuration.Priority, _configuration.Direction);

        for (var index = 0; index < _cards.Length; index++)
        {
            _cards[index].Show(_options[index], currentCompany);
        }

        RefreshCampaignSelection();
    }

    internal void ResetDraft() => _configuration.Reset();

    private void SelectCampaign(int index)
    {
        var option = _options[index];
        _configuration.Select(index, option.HypeCap, option.Unlocked);

        _soundEffects?.PlaySound(3, true);
        RefreshCampaignSelection();
    }

    private void RefreshCampaignSelection()
    {
        for (var index = 0; index < _cards.Length; index++)
        {
            _cards[index].SetSelectionColor(
                _configuration.SelectedCampaign == index ? _menuManager.colors[7] : Color.white);
        }

        _threshold.interactable = HasSelectedCampaign;
        _threshold.maxValue = HasSelectedCampaign ? _configuration.HypeCap : 1;
        _threshold.SetValueWithoutNotify(SelectedThreshold);
        RefreshThresholdLabels();
    }

    private void RefreshThresholdLabels()
    {
        if (!HasSelectedCampaign)
        {
            _thresholdValue.text = "Hype threshold: choose a campaign";
            _thresholdRange.text = "";
            _thresholdExplanation.text = "Select a campaign to configure its hype threshold.";
            return;
        }

        _thresholdValue.text = $"Hype threshold: {SelectedThreshold}";
        _thresholdRange.text = $"0 - {_configuration.HypeCap} hype";
        _thresholdExplanation.text =
            $"Selected products become eligible for marketing when their hype is at or below {SelectedThreshold}.\n" +
            $"Once marketing starts, it continues toward the campaign cap of {_configuration.HypeCap}.";
    }
}
