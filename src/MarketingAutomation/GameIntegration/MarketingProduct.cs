using System.Collections.Generic;
using UnityEngine;

namespace MarketingAutomation;

internal sealed class MarketingProduct
{
    private readonly gameScript game;
    private readonly platformScript console;
    internal ProductKey Key
    {
        get;
    }
    internal string SearchName
    {
        get;
    }
    internal string SortName => game ? game.GetNameSimple() : console.GetName();
    internal float NativeReleaseSortKey => game
        ? ProductListSorting.ReleaseKey(game.date_year, game.date_month, game.inDevelopment || game.schublade)
        : ProductListSorting.ReleaseKey(console.date_year, console.date_month, !console.isUnlocked);
    internal bool Exists => game || console;
    internal string Name => game ? game.GetNameWithTag() : console.GetName();
    internal float Hype => game ? game.GetHype() : console.GetHype();
    internal Sprite Icon => game ? game.GetTypSprite() : console.GetTypSprite();
    internal string Tooltip => game ? game.GetTooltip() : console.GetTooltip();
    internal gameScript NativeGame => game;
    internal platformScript NativeConsole => console;
    internal bool CanMarket(int companyId) => game
        ? (game.developerID == companyId || game.publisherID == companyId) &&
          (game.inDevelopment || game.isOnMarket || game.schublade) && !game.typ_contractGame &&
          (game.isOnMarket || game.sellsTotal <= 0)
        : console && console.ownerID == companyId && !console.vomMarktGenommen;

    internal int? ReleaseMonth
    {
        get
        {
            var hasDate = game ? !game.inDevelopment && !game.schublade : console && console.isUnlocked;
            var year = game ? game.date_year : console ? console.date_year : 0;
            var month = game ? game.date_month : console ? console.date_month : 0;
            return hasDate && year > 0 && month >= 1 && month <= 12 ? year * 12 + month : (int?)null;
        }
    }

    private MarketingProduct(gameScript value, bool readName = true)
    {
        game = value;
        Key = new ProductKey(ProductKind.Game, value.myID, value.GetInstanceID());
        SearchName = readName ? value.GetNameSimple() : "";
    }

    private MarketingProduct(platformScript value, bool readName = true)
    {
        console = value;
        Key = new ProductKey(ProductKind.Console, value.myID, value.GetInstanceID());
        SearchName = readName ? value.GetName() : "";
    }

    internal string ReleaseDate(textScript texts) => game ? game.GetReleaseDateString() :
        console.isUnlocked ? console.GetDateString() : texts.GetText(528);

    internal static List<MarketingProduct> ReadCatalog(int companyId)
    {
        var result = new List<MarketingProduct>();
        // Match Menu_Marketing_SelectGame.CheckGameData exactly. Do not call the
        // singleton menu's Init/SetData, which changes its native controls and rows.
        foreach (var item in GameObject.FindGameObjectsWithTag("Game"))
        {
            var game = item.GetComponent<gameScript>();
            if (game && (game.developerID == companyId || game.publisherID == companyId) &&
                (game.inDevelopment || game.isOnMarket || game.schublade) && !game.typ_contractGame)
            {
                result.Add(new MarketingProduct(game));
            }
        }
        // Match Menu_Marketing_SelectKonsole.SetData, including unreleased consoles.
        foreach (var item in GameObject.FindGameObjectsWithTag("Platform"))
        {
            var console = item.GetComponent<platformScript>();
            if (console && console.ownerID == companyId && !console.vomMarktGenommen)
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
