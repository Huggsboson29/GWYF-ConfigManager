using HarmonyLib;
using Mirror;
using TimeConfig.Network;
using TimeConfig.Runtime;

namespace TimeConfig.Patches;

[HarmonyPatch(typeof(GameManager), "OnAwake")]
internal static class DayTimerPatches
{
    private static readonly AccessTools.FieldRef<GameManager, GameSettings> GameSettingsRef =
        AccessTools.FieldRefAccess<GameManager, GameSettings>("_gs");

    private static void Postfix(GameManager __instance)
    {
        if (__instance == null) return;

        var gameSettings = GameSettingsRef(__instance);
        if (gameSettings == null) return;

        // Only the host/server applies overrides.
        if (!NetworkServer.active) return;

        TimingCoordinator.TryApplyToGameSettings(gameSettings, "GameManager.OnAwake");

        // Inform connected clients of the applied timing profile.
        LobbyVisibility.BroadcastCurrentState();
    }
}
