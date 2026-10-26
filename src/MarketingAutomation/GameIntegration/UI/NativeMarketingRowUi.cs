using UnityEngine.UI;

namespace MarketingAutomation.GameIntegration.UI;

internal sealed class NativeMarketingRowUi
{
    private readonly Item_Marketing_Game _row;

    internal NativeMarketingRowUi(Item_Marketing_Game row) => _row = row;

    internal Text ReleaseDateLabel => _row.uiObjects[2].GetComponent<Text>();
}
