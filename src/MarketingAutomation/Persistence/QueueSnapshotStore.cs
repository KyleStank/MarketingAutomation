using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using MarketingAutomation.GameIntegration;

namespace MarketingAutomation.Persistence;

internal enum SnapshotMatch
{
    Missing,
    Matched,
    Conflicting,
    Damaged
}

// Immutable, content-addressed history. Native files are only opened for reading.
internal sealed class QueueSnapshotStore
{
    private const int MaximumRecordBytes = 16 * 1024 * 1024;
    private readonly string root;

    internal QueueSnapshotStore(string root) => this.root = Path.GetFullPath(root);

    internal static string Fingerprint(string nativePath, out long length)
    {
        using var stream = new FileStream(nativePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        length = stream.Length;
        using var sha = SHA256.Create();
        return Hex(sha.ComputeHash(stream));
    }

    internal void Publish(string nativePath, QueueSaveSnapshot snapshot)
    {
        var fingerprint = Fingerprint(nativePath, out var length);
        var payload = Serialize(snapshot);
        var payloadHash = Hash(payload);
        var directory = Path.Combine(root, fingerprint);
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, payloadHash + ".json");
        var envelope = new SnapshotEnvelope
        {
            Schema = 1,
            NativeHash = fingerprint,
            NativeLength = length,
            PayloadHash = payloadHash,
            Payload = Convert.ToBase64String(payload)
        };
        if (File.Exists(destination))
        {
            ReadEnvelope(destination, fingerprint, length);
            return;
        }

        var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            var bytes = Serialize(envelope);
            if (bytes.Length > MaximumRecordBytes)
            {
                throw new InvalidDataException("Automation snapshot is too large.");
            }

            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    internal SnapshotMatch Read(string nativePath, out QueueSaveSnapshot snapshot, out string fingerprint)
    {
        snapshot = null;
        fingerprint = Fingerprint(nativePath, out var length);
        var directory = Path.Combine(root, fingerprint);
        if (!Directory.Exists(directory))
        {
            return SnapshotMatch.Missing;
        }

        string chosenPayload = null;
        var conflicting = false;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var envelope = ReadEnvelope(path, fingerprint, length);
                snapshot = Deserialize<QueueSaveSnapshot>(Convert.FromBase64String(envelope.Payload));
                if (snapshot.Schema != 1)
                {
                    throw new InvalidDataException("Unsupported automation snapshot version.");
                }

                // Older records kept the pre-save float's extra digits. They
                // describe the same paid progress when native serialization agrees.
                NativeCampaignProgress.Normalize(snapshot);
                var comparablePayload = Hash(Serialize(snapshot));
                if (chosenPayload != null && chosenPayload != comparablePayload)
                {
                    conflicting = true;
                }

                chosenPayload = comparablePayload;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException ||
                                              exception is SerializationException ||
                                              exception is FormatException || exception is ArgumentException ||
                                              exception is NullReferenceException)
            {
                snapshot = null;
                return SnapshotMatch.Damaged;
            }
        }

        if (conflicting)
        {
            snapshot = null;
            return SnapshotMatch.Conflicting;
        }

        return snapshot == null ? SnapshotMatch.Missing : SnapshotMatch.Matched;
    }

    private static SnapshotEnvelope ReadEnvelope(string path, string fingerprint, long length)
    {
        var info = new FileInfo(path);
        if (info.Length <= 0 || info.Length > MaximumRecordBytes)
        {
            throw new InvalidDataException("Invalid automation snapshot size.");
        }

        var record = Deserialize<SnapshotEnvelope>(File.ReadAllBytes(path));
        if (record == null || record.Schema != 1 || record.NativeHash != fingerprint || record.NativeLength != length ||
            record.Payload == null || record.PayloadHash != Hash(Convert.FromBase64String(record.Payload)))
        {
            throw new InvalidDataException("Automation snapshot integrity check failed.");
        }

        return record;
    }

    private static byte[] Serialize<T>(T value)
    {
        using var stream = new MemoryStream();
        new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
        return stream.ToArray();
    }

    private static T Deserialize<T>(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, false);
        return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
    }

    private static string Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Hex(sha.ComputeHash(bytes));
    }

    private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

    [DataContract]
    private sealed class SnapshotEnvelope
    {
        [DataMember(Order = 0)] internal int Schema;
        [DataMember(Order = 1)] internal string NativeHash;
        [DataMember(Order = 2)] internal long NativeLength;
        [DataMember(Order = 3)] internal string PayloadHash;
        [DataMember(Order = 4)] internal string Payload;
    }
}
