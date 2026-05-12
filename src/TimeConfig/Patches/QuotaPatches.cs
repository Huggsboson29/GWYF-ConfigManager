using HarmonyLib;
using Mirror;
using TimeConfig.Runtime;

namespace TimeConfig.Patches;

[HarmonyPatch(typeof(LocalSaveManager), nameof(LocalSaveManager.CreateNewSave))]
internal static class QuotaSaveInitializationPatches
{
    private static void Prefix()
    {
        if (!NetworkServer.active) return;
        TimingCoordinator.TryApplyFromResources("LocalSaveManager.CreateNewSave");
    }
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.ResetCurrentSaveToDefaults))]
internal static class QuotaSaveResetPatches
{
    private static void Prefix()
    {
        if (!NetworkServer.active) return;
        TimingCoordinator.TryApplyFromResources("SaveManager.ResetCurrentSaveToDefaults");
    }
}
