using Crpg.Module.Common.KeyBinder;
using HarmonyLib;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Options;

namespace Crpg.Module.HarmonyPatches;

#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
[HarmonyPatch]
public static class Patch_HotKeyManager_InitialContexts
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HotKeyManager), nameof(HotKeyManager.RegisterInitialContexts))]
    public static bool Prefix(ref IEnumerable<GameKeyContext> contexts)
    {
        TaleWorlds.Library.Debug.Print("HarmonyPrefix Patch initial contexts", 0, TaleWorlds.Library.Debug.DebugColor.Cyan);
        List<GameKeyContext> newContexts = contexts.ToList();
        foreach (GameKeyContext context in KeyBinder.KeyContexts.Values)
        {
            if (!newContexts.Contains(context))
            {
                newContexts.Add(context);
            }
        }

        contexts = newContexts;
        return true;
    }
}
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName

[HarmonyPatch]
public static class Patch_OptionsProvider_GameKeyList
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionsProvider), nameof(OptionsProvider.GetGameKeyCategoriesList))]
    public static IEnumerable<string> Postfix(IEnumerable<string> __result)
    {
        TaleWorlds.Library.Debug.Print("HarmonyPostfix Patch OptionsProvider", 0, TaleWorlds.Library.Debug.DebugColor.Cyan);
        // Combine the existing result with the new categories
        return __result.Concat(KeyBinder.KeysCategories.Select(c => c.CategoryId).Distinct());
    }
}
