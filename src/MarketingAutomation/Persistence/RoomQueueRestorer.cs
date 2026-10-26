using MarketingAutomation.Automation;
using MarketingAutomation.Automation.Runtime;
using MarketingAutomation.GameIntegration.Marketing;
using UnityEngine;

namespace MarketingAutomation.Persistence;

// Resolves saved identities against objects reconstructed by the native loader.
// Validation must finish before a queue adopts paid work; restoration never purchases a campaign.
internal static class RoomQueueRestorer
{
    internal static void Restore(mainScript company, RoomQueueSnapshot record,
        List<MarketingProduct> allProducts, roomScript[] rooms, GameObject[] tasks)
    {
        var room = FindMarketingRoom(company, record.RoomId, rooms);
        var selectedProducts = ResolveSelectedProducts(record.Products, allProducts);
        var paidCampaign = ResolvePaidCampaign(record, room, selectedProducts, rooms, tasks);

        MarketingQueue.Restore(room, company.guiMain_, selectedProducts, record, paidCampaign);
    }

    private static roomScript FindMarketingRoom(mainScript company, int roomId, roomScript[] rooms)
    {
        roomScript match = null;
        foreach (var candidate in rooms)
        {
            if (candidate.myID != roomId)
            {
                continue;
            }

            if (match)
            {
                throw new InvalidDataException("Room identity is ambiguous.");
            }

            match = candidate;
        }

        // Native typ means "type"; room type 6 is Marketing.
        if (!match || match.typ != 6 || match.mS_ != company)
        {
            throw new InvalidDataException("Marketing room no longer exists.");
        }

        return match;
    }

    private static List<MarketingProduct> ResolveSelectedProducts(ProductIdentity[] identities,
        List<MarketingProduct> allProducts)
    {
        var selectedProducts = new List<MarketingProduct>();
        foreach (var identity in identities)
        {
            var product = FindSelectedProduct(identity, allProducts);
            if (product != null)
            {
                selectedProducts.Add(product);
            }
        }

        // Removed products cannot run, but MarketingQueue retains their saved identities for later checkpoints.
        return selectedProducts;
    }

    private static MarketingProduct FindSelectedProduct(ProductIdentity identity, List<MarketingProduct> allProducts)
    {
        MarketingProduct match = null;
        foreach (var product in allProducts)
        {
            if (!identity.Matches(product.Key))
            {
                continue;
            }

            if (match != null)
            {
                throw new InvalidDataException("Product identity is ambiguous.");
            }

            match = product;
        }

        return match;
    }

    private static taskMarketing ResolvePaidCampaign(RoomQueueSnapshot record, roomScript room,
        List<MarketingProduct> selectedProducts, roomScript[] rooms, GameObject[] tasks)
    {
        if (record.Paid == null)
        {
            if (room.taskID != -1)
            {
                throw new InvalidDataException("The waiting room now has an unrelated native task.");
            }

            return null;
        }

        if (room.taskID != record.Paid.TaskId ||
            !selectedProducts.Exists(product => record.Paid.Target.Matches(product.Key)))
        {
            throw new InvalidDataException("The paid campaign binding does not match.");
        }

        var paidCampaign = FindMatchingPaidCampaign(record, tasks);
        ValidatePaidProgress(record.Paid, paidCampaign);
        ValidateExclusiveRoomAssignment(room, paidCampaign, rooms);
        return paidCampaign;
    }

    private static taskMarketing FindMatchingPaidCampaign(RoomQueueSnapshot record, GameObject[] tasks)
    {
        taskMarketing paidCampaign = null;
        var matches = 0;
        foreach (var task in tasks)
        {
            if (task.name == "Task_" + record.Paid.TaskId)
            {
                matches++;
                paidCampaign = task.GetComponent<taskMarketing>();
            }
        }

        // Native typ is product type (0 game, 1 console); kampagne is the campaign index.
        // disableWarten means "disable waiting". All native repeat/stop/wait flags must remain off.
        if (matches != 1 || !paidCampaign || paidCampaign.myID != record.Paid.TaskId ||
            paidCampaign.typ != record.Paid.Target.Kind || paidCampaign.targetID != record.Paid.Target.Id ||
            paidCampaign.kampagne != record.Campaign || paidCampaign.automatic ||
            paidCampaign.stopAutomatic || paidCampaign.disableWarten)
        {
            throw new InvalidDataException(
                $"Native campaign identity/flags differ from checkpoint task {record.Paid.TaskId}; matches={matches}.");
        }

        return paidCampaign;
    }

    private static void ValidatePaidProgress(PaidCampaignSnapshot checkpoint, taskMarketing paidCampaign)
    {
        if (!NativeCampaignProgress.Matches(checkpoint.Points, paidCampaign.points) ||
            !NativeCampaignProgress.Matches(checkpoint.PointsLeft, paidCampaign.pointsLeft))
        {
            throw new InvalidDataException($"Native campaign progress differs for task {paidCampaign.myID}: " +
                                           $"checkpoint total/left={checkpoint.Points:R}/{checkpoint.PointsLeft:R}, " +
                                           $"loaded total/left={paidCampaign.points:R}/{paidCampaign.pointsLeft:R}.");
        }
    }

    private static void ValidateExclusiveRoomAssignment(roomScript owner, taskMarketing paidCampaign, roomScript[] rooms)
    {
        foreach (var candidate in rooms)
        {
            if (candidate != owner && candidate.taskID == paidCampaign.myID)
            {
                throw new InvalidDataException("The campaign is assigned to more than one room.");
            }
        }
    }
}
