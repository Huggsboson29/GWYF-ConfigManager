using HarmonyLib;
using Mirror;
using TimeConfig.Network;

namespace TimeConfig.Patches;

/// <summary>
/// Broadcasts the active timing profile to all clients when the server starts and
/// to individual clients when they become scene-ready.
/// </summary>
[HarmonyPatch(typeof(GameManager))]
internal static class LobbyPatches
{
    [HarmonyPostfix, HarmonyPatch("OnStartServer")]
    private static void OnStartServer_Postfix()
    {
        // The server just started — send the current timing state to whoever is already connected.
        // This covers the host-only launch case (listen server) and any clients that somehow
        // connected before OnStartServer fired (edge case, usually nobody).
        LobbyVisibility.BroadcastCurrentState();
    }

    // Fires on the server when an individual client has loaded the scene and is ready to play.
    // Signature: void ServerOnClientScenePlayReady(NetworkConnectionToClient conn, int epoch)
    [HarmonyPostfix, HarmonyPatch("ServerOnClientScenePlayReady")]
    private static void ServerOnClientScenePlayReady_Postfix(NetworkConnectionToClient conn)
    {
        LobbyVisibility.SendToClient(conn);
    }
}
