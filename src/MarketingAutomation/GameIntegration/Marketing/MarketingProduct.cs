using MarketingAutomation.Automation;
using UnityEngine;

namespace MarketingAutomation.GameIntegration.Marketing;

internal sealed class MarketingProduct
{
    // Native German names: schublade ("drawer") means shelved; vomMarktGenommen means withdrawn
    // from the market; typ_contractGame identifies a contract game; GetTypSprite returns a product-type icon.
    // Menu_Marketing_SelectKonsole is the native console selector (Konsole = console).
    private readonly gameScript _game;
    private readonly platformScript _console;

    internal ProductKey Key
    {
        get;
    }

    internal string SearchName
    {
        get;
    }

    internal string SortName => _game ? _game.GetNameSimple() : _console.GetName();

    internal float NativeReleaseSortKey => _game
        ? NativeProductOrdering.ReleaseDateKey(_game.date_year, _game.date_month, _game.inDevelopment || _game.schublade)
        : NativeProductOrdering.ReleaseDateKey(_console.date_year, _console.date_month, !_console.isUnlocked);

    internal bool Exists => _game || _console;
    internal string Name => _game ? _game.GetNameWithTag() : _console.GetName();
    internal float Hype => _game ? _game.GetHype() : _console.GetHype();
    internal Sprite Icon => _game ? _game.GetTypSprite() : _console.GetTypSprite();
    internal string Tooltip => _game ? _game.GetTooltip() : _console.GetTooltip();
    internal gameScript NativeGame => _game;
    internal platformScript NativeConsole => _console;

    internal bool CanMarket(int companyId)
    {
        if (_game)
        {
            // Catalog membership is broader than dispatch eligibility: a previously sold game
            // can remain in the catalog, but it must still be on the market to buy another campaign.
            return IsGameInCatalog(_game, companyId) && (_game.isOnMarket || _game.sellsTotal <= 0);
        }

        return IsConsoleInCatalog(_console, companyId);
    }

    private static bool IsGameInCatalog(gameScript game, int companyId)
    {
        if (!game)
            return false;

        var belongsToCompany = game.developerID == companyId || game.publisherID == companyId;
        var availableForMarketing = game.inDevelopment || game.isOnMarket || game.schublade;

        return belongsToCompany && availableForMarketing && !game.typ_contractGame;
    }

    private static bool IsConsoleInCatalog(platformScript console, int companyId) =>
        console && console.ownerID == companyId && !console.vomMarktGenommen;

    internal int? ReleaseMonth
    {
        get
        {
            if (_game)
            {
                if (_game.inDevelopment || _game.schublade)
                    return null;

                return GetReleaseMonth(_game.date_year, _game.date_month);
            }

            if (!_console || !_console.isUnlocked)
                return null;

            return GetReleaseMonth(_console.date_year, _console.date_month);
        }
    }

    private static int? GetReleaseMonth(int year, int month) =>
        year > 0 && month >= 1 && month <= 12 ? year * 12 + month : (int?)null;

    private MarketingProduct(gameScript value, bool readName = true)
    {
        _game = value;
        Key = new ProductKey(ProductKind.Game, value.myID, value.GetInstanceID());
        SearchName = readName ? value.GetNameSimple() : "";
    }

    private MarketingProduct(platformScript value, bool readName = true)
    {
        _console = value;
        Key = new ProductKey(ProductKind.Console, value.myID, value.GetInstanceID());
        SearchName = readName ? value.GetName() : "";
    }

    internal string ReleaseDate(textScript texts)
    {
        if (_game)
            return _game.GetReleaseDateString();

        return _console.isUnlocked ? _console.GetDateString() : texts.GetText(528);
    }

    internal static List<MarketingProduct> ReadCatalog(int companyId)
    {
        var result = new List<MarketingProduct>();
        // Match Menu_Marketing_SelectGame.CheckGameData exactly. Do not call the
        // singleton menu's Init/SetData, which changes its native controls and rows.
        foreach (var item in GameObject.FindGameObjectsWithTag("Game"))
        {
            var game = item.GetComponent<gameScript>();
            if (IsGameInCatalog(game, companyId))
            {
                result.Add(new MarketingProduct(game));
            }
        }

        // Match Menu_Marketing_SelectKonsole.SetData, including unreleased consoles.
        foreach (var item in GameObject.FindGameObjectsWithTag("Platform"))
        {
            var console = item.GetComponent<platformScript>();
            if (IsConsoleInCatalog(console, companyId))
            {
                result.Add(new MarketingProduct(console));
            }
        }

        return result;
    }

    internal static List<MarketingProduct> ReadAll()
    {
        var result = new List<MarketingProduct>();
        foreach (var item in GameObject.FindGameObjectsWithTag("Game"))
        {
            var value = item.GetComponent<gameScript>();
            if (value)
            {
                result.Add(new MarketingProduct(value, false));
            }
        }

        foreach (var item in GameObject.FindGameObjectsWithTag("Platform"))
        {
            var value = item.GetComponent<platformScript>();
            if (value)
            {
                result.Add(new MarketingProduct(value, false));
            }
        }

        return result;
    }
}
