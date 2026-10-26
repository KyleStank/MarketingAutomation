using UnityEngine;

namespace MarketingAutomation.GameIntegration.UI;

// Native hierarchy paths remain exact; callers choose which component they need from each node.
internal static class NativeUiLookup
{
    internal static T RequireComponent<T>(Transform root, string path) where T : Component
    {
        var child = root.Find(path);
        var value = child ? child.GetComponent<T>() : null;
        return !value
            ? throw new InvalidOperationException($"Missing native UI template: {path} ({typeof(T).Name}).")
            : value;
    }
}
