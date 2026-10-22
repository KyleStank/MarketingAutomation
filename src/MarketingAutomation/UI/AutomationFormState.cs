namespace MarketingAutomation.UI;

internal enum AutomationPage
{
    Products,
    Campaign
}

internal enum AutomationValidation
{
    None,
    MissingProducts,
    MissingCampaign
}

// Navigation is independent of validation: the user can configure either page first.
internal sealed class AutomationFormState
{
    internal AutomationPage Page
    {
        get;
        private set;
    }

    internal bool CanGoBack => Page != AutomationPage.Products;
    internal bool CanGoForward => Page != AutomationPage.Campaign;
    internal void OpenPage(AutomationPage page) => Page = page;
    internal void Reset() => Page = AutomationPage.Products;

    internal static AutomationValidation Validate(int productCount, bool campaignSelected)
    {
        if (productCount <= 0)
        {
            return AutomationValidation.MissingProducts;
        }

        return campaignSelected ? AutomationValidation.None : AutomationValidation.MissingCampaign;
    }
}
