using MarketingAutomation.GameIntegration.Marketing;
using MarketingAutomation.GameIntegration.UI;
using MarketingAutomation.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace MarketingAutomation.UI;

// One campaign's visuals and availability; selection and threshold belong to the page.
internal sealed class CampaignCard
{
    private readonly Image _background;
    private readonly Button _button;
    private readonly Text _name;
    private readonly Text _gamePrice;
    private readonly Text _consolePrice;
    private readonly Text _gain;
    private readonly tooltip _details;
    private readonly GameObject _lockOverlay;

    internal CampaignCard(Button style, Transform parent, GUI_Main manager, int index, Action<int> selectCampaign)
    {
        var root = NativeVisualTree.Copy(style.transform, parent, manager);
        UiFactory.Place(root, new Vector2(index % 2 == 0 ? -140f : 140f, -272f - index / 2 * 94f),
            new Vector2(270f, 88f));

        _button = UiFactory.AddButton(root, style);
        _button.onClick.AddListener(() => selectCampaign(index));
        _background = root.GetComponent<Image>();

        _name = NativeUiLookup.RequireComponent<Text>(root, "Text");

        // TextPreis is the native price label; M1 through M6 identify the six campaigns.
        _gamePrice = NativeUiLookup.RequireComponent<Text>(root, $"TextPreis_M{index + 1}");
        _gain = NativeUiLookup.RequireComponent<Text>(root, $"TextHype_M{index + 1}");

        var consolePriceRoot = NativeVisualTree.Copy(_gamePrice.transform, root, manager);
        consolePriceRoot.name = "ConsolePrice";
        _consolePrice = consolePriceRoot.GetComponent<Text>();

        ConfigureLabel(_name, -14f, 20);
        ConfigureLabel(_gamePrice, -35f, 13);
        ConfigureLabel(_consolePrice, -53f, 13);
        ConfigureLabel(_gain, -74f, 13);
        UiFactory.Place(_gain.rectTransform, new Vector2(28f, -74f), new Vector2(164f, 18f));

        var hypeIcon = NativeUiLookup.RequireComponent<RectTransform>(root, "IconHype");
        UiFactory.Place(hypeIcon, new Vector2(-66f, -74f), new Vector2(16f, 16f));

        _details = root.gameObject.AddComponent<tooltip>();
        _lockOverlay = FindLockOverlay(root, index);
    }

    private static void ConfigureLabel(Text text, float y, int maxFontSize)
    {
        UiFactory.Place(text.rectTransform, new Vector2(20f, y), new Vector2(188f, maxFontSize > 14 ? 22f : 18f));
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = maxFontSize;
    }

    private static GameObject FindLockOverlay(RectTransform root, int index)
    {
        // Only the Internet and streamer campaigns have native unlock artwork.
        var lockRoot = index == 3 ? root.Find("LockM4") : index == 4 ? root.Find("LockM5") : null;
        if ((index == 3 || index == 4) && !lockRoot)
        {
            throw new InvalidOperationException("Native campaign lock artwork is missing.");
        }

        return lockRoot ? lockRoot.gameObject : null;
    }

    internal void Show(NativeCampaignOption option, mainScript company)
    {
        _name.text = option.Name;
        _gamePrice.text = $"Game: {company.GetMoney(option.GamePrice, true)}";
        _consolePrice.text = $"Console: {company.GetMoney(option.ConsolePrice, true)}";
        _gain.text = $"+{option.HypeGain} hype (max {option.HypeCap})";

        _button.interactable = option.Unlocked;
        if (_lockOverlay)
        {
            _lockOverlay.SetActive(!option.Unlocked);
        }

        _details.c = $"{option.Name}\nGame: {company.GetMoney(option.GamePrice, true)}\n" +
                     $"Console: {company.GetMoney(option.ConsolePrice, true)}\n" +
                     $"+{option.HypeGain} hype per campaign, up to {option.HypeCap}." +
                     (option.Unlocked ? "" : "\nThis campaign has not been unlocked yet.");
    }

    internal void SetSelectionColor(Color color) => _background.color = color;
}
