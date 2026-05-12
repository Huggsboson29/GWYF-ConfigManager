using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TimeConfig.Configuration;
using TimeConfig.Network;
using TimeConfig.Runtime;

namespace TimeConfig;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class PluginMain : BaseUnityPlugin
{
    public const string PluginGuid = "com.dylan.gwyf.timeconfig";
    public const string PluginName = "TimeConfig";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Log = Logger;

        var activeSettings = ActiveSettings.Bind(Config);
        TimingCoordinator.Initialize(activeSettings, Log);
        TimingCoordinator.TryApplyFromResources("PluginMain.Awake");

        LobbyVisibility.Initialize(Log);
        LobbyVisibility.RegisterMessageDelegates();

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

        Log.LogInfo($"{PluginName} {PluginVersion} initialized.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
