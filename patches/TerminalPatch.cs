using PierToPierPlugin;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using Util;
using System.Reflection;
using System.Collections.Concurrent;

[HarmonyPatch]
public class TerminalPatch
{
    private static bool ignoreMm = false;
    [HarmonyPatch(typeof(Terminal), "RebootSystem")]
    class GoToMainMenuPatch
    {
        static bool Prefix(Terminal __instance, ref bool goToMainMenu, ref bool waitTime, ref bool forceSafeMode)
        {
            if (Networking.IsSinglePlayer() && DataUtils.hosting == true && goToMainMenu == true)
            {
                if (!ignoreMm)
                {
                    DataUtils.webHookHandler?.SendEmbedAsync("Server Info", "Server shutdown in 30 seconds!");
                    ignoreMm = true;
                }
                else
                {
                    ignoreMm = false;
                    FieldInfo fieldInfo = AccessTools.Field(typeof(ServerListener), "players");
                    foreach (var player in (fieldInfo.GetValue(ServerListener.Singleton) as ConcurrentDictionary<string, PlayerServer>).Values)
                    {
                        player.OnDisconnect();
                    }
                    return true;
                }
                object[] args = new object[] { goToMainMenu, waitTime, forceSafeMode };
                __instance.StartCoroutine(DelayedShutdown("RebootSystem", __instance, args));
                return false;
            }
            return true;
        }
    }

    private static bool ignoreSd = false;
    [HarmonyPatch(typeof(Terminal), "PlayerShutdown")]
    class PlayerShutdownPatch
    {
        static bool Prefix(Terminal __instance)
        {
            if (Networking.IsSinglePlayer() && DataUtils.hosting == true)
            {
                if (!ignoreSd)
                {
                    DataUtils.webHookHandler?.SendEmbedAsync("Server Info", "Server shutdown in 30 seconds!");
                    ignoreSd = true;
                }
                else
                {
                    ignoreSd = false;
                    FieldInfo fieldInfo = AccessTools.Field(typeof(ServerListener), "players");
                    foreach (var player in (fieldInfo.GetValue(ServerListener.Singleton) as ConcurrentDictionary<string, PlayerServer>).Values)
                    {
                        player.OnDisconnect();
                    }
                    return true;
                }
                object[] args = new object[] { };
                __instance.StartCoroutine(DelayedShutdown("PlayerShutdown", __instance, args));
                return false;
            }
            return true;
        }
    }

    private static IEnumerator DelayedShutdown(string method, Terminal __instance, object[] args)
    {
        int delay = 35;
        AdminMessageHandler.AdminMsgToClients("", AdminMessage.MsgType.SERVER_RESTART, true, delay - 5);
        yield return new WaitForSeconds(delay);
        var call = AccessTools.Method(typeof(Terminal), method);
        yield return (IEnumerator)call.Invoke(__instance, args);
    }
}