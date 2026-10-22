using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

// Draft configuration. Native campaign menus remain untouched and are never initialized.
public sealed class AutomationCampaignPage : MonoBehaviour
{
    private readonly CampaignConfiguration configuration = new();
    private readonly CampaignCard[] cards = new CampaignCard[6];
    private GUI_Main menuManager;
    private Slider threshold;
    private Dropdown priorityField;
    private Dropdown priorityDirection;
    private Text priorityExplanation;
    private Text thresholdValue;
    private Text thresholdRange;
    private Text thresholdExplanation;
    private NativeCampaignOption[] options;
    private mainScript company;
    private sfxScript soundEffects;

    internal bool HasSelectedCampaign => configuration.HasSelection && options != null &&
                                         options[configuration.SelectedCampaign].Unlocked;

    internal int SelectedThreshold => configuration.Threshold ?? 0;
    internal string SelectedCampaignName => HasSelectedCampaign ? options[configuration.SelectedCampaign].Name : "";
    internal string SelectedPrioritySummary => configuration.PrioritySummary;
    internal int SelectedCampaign => configuration.SelectedCampaign;
    internal QueuePriority SelectedPriority => configuration.Priority;
    internal QueueDirection SelectedDirection => configuration.Direction;

    internal void Build(GUI_Main manager, Text textStyle)
    {
        menuManager = manager;
        BuildPriority(textStyle);
        var templates = manager.uiObjects[89].GetComponent<Menu_Marketing_GameKampagne>();
        var panelStyle = NativeUi.Require<RectTransform>(templates.transform, "WindowMain/PanelButtons");
        // Reuse the native group's image, outline, and shadow without its grid
        // layout or children; the custom campaign cards keep their own geometry.
        var background = NativeUi.CopyTree(panelStyle, transform, manager, _ => false);
        background.name = "CampaignBackground";
        UiFactory.Place(background, new Vector2(0f, -366f), new Vector2(578f, 284f));
        background.GetComponent<Image>().raycastTarget = false;
        var heading = UiFactory.CreateText("CampaignHeading", transform, textStyle, "Choose a marketing campaign",
            new Vector2(0f, -208f), new Vector2(550f, 26f));
        heading.resizeTextMaxSize = 18;
        for (var index = 0; index < cards.Length; index++)
        {
            var campaign = index;
            var style = templates.uiObjects[index + 2].GetComponent<Button>();
            var root = NativeUi.CopyTree(style.transform, transform, manager);
            UiFactory.Place(root, new Vector2(index % 2 == 0 ? -140f : 140f, -272f - index / 2 * 94f),
                new Vector2(270f, 88f));
            var button = NativeUi.AddButton(root, style);
            button.onClick.AddListener(() => Select(campaign));
            var name = NativeUi.Require<Text>(root, "Text");
            var gamePrice = NativeUi.Require<Text>(root, $"TextPreis_M{index + 1}");
            var gain = NativeUi.Require<Text>(root, $"TextHype_M{index + 1}");
            var consolePriceRoot = NativeUi.CopyTree(gamePrice.transform, root, manager);
            consolePriceRoot.name = "ConsolePrice";
            var consolePrice = consolePriceRoot.GetComponent<Text>();
            ConfigureLabel(name, -14f, 20);
            ConfigureLabel(gamePrice, -35f, 13);
            ConfigureLabel(consolePrice, -53f, 13);
            ConfigureLabel(gain, -74f, 13);
            UiFactory.Place(gain.rectTransform, new Vector2(28f, -74f), new Vector2(164f, 18f));
            var hypeIcon = NativeUi.Require<RectTransform>(root, "IconHype");
            UiFactory.Place(hypeIcon, new Vector2(-66f, -74f), new Vector2(16f, 16f));
            var tooltip = root.gameObject.AddComponent<tooltip>();
            var lockRoot = index == 3 ? root.Find("LockM4") : index == 4 ? root.Find("LockM5") : null;
            if ((index == 3 || index == 4) && !lockRoot)
            {
                throw new InvalidOperationException("Native campaign lock artwork is missing.");
            }

            cards[index] = new CampaignCard(root.GetComponent<Image>(), button, name, gamePrice,
                consolePrice, gain, tooltip, lockRoot ? lockRoot.gameObject : null);
        }

        thresholdValue = UiFactory.CreateText("ThresholdValue", transform, textStyle, "",
            new Vector2(0f, -529f), new Vector2(550f, 26f));
        thresholdValue.resizeTextMaxSize = 18;
        BuildSlider();
        thresholdRange = UiFactory.CreateText("ThresholdRange", transform, textStyle, "",
            new Vector2(0f, -586f), new Vector2(530f, 22f));
        thresholdRange.resizeTextMaxSize = 14;
        thresholdExplanation = UiFactory.CreateText("ThresholdExplanation", transform, textStyle, "",
            new Vector2(0f, -623f), new Vector2(550f, 46f));
        thresholdExplanation.resizeTextMaxSize = 14;
        thresholdExplanation.supportRichText = false;
    }

    private void BuildPriority(Text textStyle)
    {
        var heading = UiFactory.CreateText("QueuePriorityHeading", transform, textStyle, "Queue priority",
            new Vector2(0f, -88f), new Vector2(550f, 22f));
        heading.resizeTextMaxSize = 18;
        var donor = menuManager.uiObjects[90].GetComponent<Menu_Marketing_SelectGame>().uiObjects[1]
            .GetComponent<Dropdown>();
        priorityField = NativeDropdown.Create(donor, transform, menuManager, "QueuePriority",
            ProductSortOptions.Labels());
        UiFactory.Place((RectTransform)priorityField.transform, new Vector2(-140f, -118f), new Vector2(270f, 32f));
        priorityDirection = NativeDropdown.Create(donor, transform, menuManager, "QueueDirection",
            new List<string> { "Ascending", "Descending" });
        UiFactory.Place((RectTransform)priorityDirection.transform, new Vector2(140f, -118f), new Vector2(270f, 32f));
        priorityExplanation = UiFactory.CreateText("QueueExplanation", transform, textStyle, "",
            new Vector2(0f, -165f), new Vector2(550f, 54f));
        priorityExplanation.resizeTextMaxSize = 14;
        priorityExplanation.supportRichText = false;
        priorityField.onValueChanged.AddListener(_ => UpdatePriority());
        priorityDirection.onValueChanged.AddListener(_ => UpdatePriority());
    }

    private void UpdatePriority()
    {
        configuration.SetPriority(ProductSortOptions.FromIndex(priorityField.value),
            (QueueDirection)priorityDirection.value);
        priorityExplanation.text = configuration.PriorityExplanation;
    }

    private void BuildSlider()
    {
        var donor = menuManager.GetComponentInChildren<Menu_Abo_Preis>(true);
        if (!donor)
        {
            throw new InvalidOperationException("Native price-slider template is unavailable.");
        }

        var source = donor.uiObjects[2].GetComponent<Slider>();
        var root = NativeUi.CopyTree(source.transform, transform, menuManager);
        root.name = "HypeThreshold";
        UiFactory.Place(root, new Vector2(0f, -560f), new Vector2(520f, 24f));
        threshold = root.gameObject.AddComponent<Slider>();
        threshold.fillRect = NativeUi.Require<RectTransform>(root, "Fill Area/Fill");
        threshold.handleRect = NativeUi.Require<RectTransform>(root, "Handle Slide Area/Handle");
        threshold.targetGraphic = threshold.handleRect.GetComponent<Image>();
        threshold.direction = source.direction;
        threshold.colors = source.colors;
        threshold.navigation = new Navigation { mode = Navigation.Mode.None };
        threshold.minValue = 0;
        threshold.maxValue = 1;
        threshold.wholeNumbers = true;
        threshold.interactable = false;
        threshold.onValueChanged.AddListener(value =>
        {
            configuration.SetThreshold(Mathf.RoundToInt(value));
            RefreshThreshold();
        });
    }

    internal void Show(mainScript currentCompany)
    {
        company = currentCompany;
        options = NativeCampaignCatalog.Read(menuManager, company);
        var soundObject = GameObject.Find("SFX");
        soundEffects = soundObject ? soundObject.GetComponent<sfxScript>() : null;
        configuration.Reset();
        priorityField.SetValueWithoutNotify(ProductSortOptions.IndexOf(configuration.Priority));
        priorityDirection.SetValueWithoutNotify((int)configuration.Direction);
        priorityExplanation.text = configuration.PriorityExplanation;
        for (var index = 0; index < cards.Length; index++)
        {
            cards[index].Show(options[index], company);
        }

        Refresh();
    }

    internal void ResetDraft() => configuration.Reset();

    private void Select(int index)
    {
        var option = options[index];
        configuration.Select(index, option.HypeCap, option.Unlocked);
        soundEffects?.PlaySound(3, true);
        Refresh();
    }

    private void Refresh()
    {
        for (var index = 0; index < cards.Length; index++)
        {
            cards[index].Background.color =
                configuration.SelectedCampaign == index ? menuManager.colors[7] : Color.white;
        }

        threshold.interactable = HasSelectedCampaign;
        threshold.maxValue = HasSelectedCampaign ? configuration.HypeCap : 1;
        threshold.SetValueWithoutNotify(SelectedThreshold);
        RefreshThreshold();
    }

    private void RefreshThreshold()
    {
        if (!HasSelectedCampaign)
        {
            thresholdValue.text = "Hype threshold: choose a campaign";
            thresholdRange.text = "";
            thresholdExplanation.text = "Select a campaign to configure its hype threshold.";
            return;
        }

        thresholdValue.text = $"Hype threshold: {SelectedThreshold}";
        thresholdRange.text = $"0 - {configuration.HypeCap} hype";
        thresholdExplanation.text =
            $"Selected products become eligible for marketing when their hype is at or below {SelectedThreshold}.\n" +
            $"Once marketing starts, it continues toward the campaign cap of {configuration.HypeCap}.";
    }

    private static void ConfigureLabel(Text text, float y, int maxFontSize)
    {
        UiFactory.Place(text.rectTransform, new Vector2(20f, y), new Vector2(188f, maxFontSize > 14 ? 22f : 18f));
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = maxFontSize;
    }

    private sealed class CampaignCard
    {
        internal readonly Image Background;
        private readonly Button button;
        private readonly Text name;
        private readonly Text gamePrice;
        private readonly Text consolePrice;
        private readonly Text gain;
        private readonly tooltip details;
        private readonly GameObject lockOverlay;

        internal CampaignCard(Image background, Button button, Text name, Text gamePrice,
            Text consolePrice, Text gain, tooltip details, GameObject lockOverlay)
        {
            Background = background;
            this.button = button;
            this.name = name;
            this.gamePrice = gamePrice;
            this.consolePrice = consolePrice;
            this.gain = gain;
            this.details = details;
            this.lockOverlay = lockOverlay;
        }

        internal void Show(NativeCampaignOption option, mainScript company)
        {
            name.text = option.Name;
            gamePrice.text = $"Game: {company.GetMoney(option.GamePrice, true)}";
            consolePrice.text = $"Console: {company.GetMoney(option.ConsolePrice, true)}";
            gain.text = $"+{option.HypeGain} hype (max {option.HypeCap})";
            button.interactable = option.Unlocked;
            if (lockOverlay)
            {
                lockOverlay.SetActive(!option.Unlocked);
            }

            details.c = $"{option.Name}\nGame: {company.GetMoney(option.GamePrice, true)}\n" +
                        $"Console: {company.GetMoney(option.ConsolePrice, true)}\n" +
                        $"+{option.HypeGain} hype per campaign, up to {option.HypeCap}." +
                        (option.Unlocked ? "" : "\nThis campaign has not been unlocked yet.");
        }
    }
}
