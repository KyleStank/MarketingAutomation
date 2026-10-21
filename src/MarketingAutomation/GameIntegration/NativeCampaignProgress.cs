using System.Globalization;

namespace MarketingAutomation;

internal static class NativeCampaignProgress
{
    // This game's Mono float formatter writes seven significant digits through
    // ES3JSONWriter.WritePrimitive(float). Compare the saved representation rather
    // than requiring all bits of the pre-save in-memory value to survive.
    internal static float SerializedValue(float value) =>
        float.Parse(value.ToString("G7", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    internal static bool Matches(float checkpoint, float loaded) =>
        Finite(checkpoint) && Finite(loaded) && (checkpoint == loaded || SerializedValue(checkpoint) == loaded);

    internal static void Normalize(QueueSaveSnapshot snapshot)
    {
        if (snapshot?.Rooms == null)
        {
            return;
        }

        foreach (var room in snapshot.Rooms)
        {
            if (room?.Paid == null)
            {
                continue;
            }

            room.Paid.Points = SerializedValue(room.Paid.Points);
            room.Paid.PointsLeft = SerializedValue(room.Paid.PointsLeft);
        }
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
