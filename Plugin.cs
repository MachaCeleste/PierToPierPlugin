using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Linq;
using Util;

namespace PierToPierPlugin;

[BepInPlugin("com.machaceleste.piertopierplugin", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        var harmony = new Harmony("com.machaceleste.piertopierplugin");
        harmony.PatchAll();
        var args = System.Environment.GetCommandLineArgs();
        if (args.Contains("-batchmode"))
        {
            new HeadlessHandler();
            HeadlessHandler.Singleton.SetupConsole();
            DataUtils.hosting = true;
            int portArg = Array.IndexOf(args, "-port");
            if (portArg >= 0 && portArg + 1 < args.Length)
            {
                if (int.TryParse(args[portArg + 1], out int port))
                {
                    Logger.LogInfo($"Port specified, setting port to {port}");
                    DataUtils.hostPort = port;
                }
                else
                {
                    Logger.LogWarning("Invalid port specified!");
                }
            }
            Networking.SetGameMode(Networking.GameMode.SinglePlayer);
            StartCoroutine(HeadlessHandler.Singleton.StartHeadless());
            HeadlessHandler.Singleton.StartConsoleListener();
        }
    }
}