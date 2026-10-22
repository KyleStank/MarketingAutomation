using HarmonyLib;

namespace MarketingAutomation.GameIntegration;

public abstract class BaseIntegration
{
    public abstract void Install(Harmony harmony);

    protected virtual void Patch(Harmony harmony, Type type, string name, string prefix = null, string postfix = null)
    {
        var target = AccessTools.DeclaredMethod(type, name);
        if (target == null)
            throw new MissingMethodException(type.FullName, name);

        harmony.Patch(
            target,
            prefix: prefix == null
                ? null
                : new HarmonyMethod(GetType(), prefix),
            postfix: postfix == null
                ? null
                : new HarmonyMethod(GetType(), postfix));
    }
}
