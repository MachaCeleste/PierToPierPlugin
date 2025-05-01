using PierToPierPlugin;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using Util;
using System.Collections.Concurrent;
using System.Reflection;

[HarmonyPatch]
public class InternalBashPatch
{
    [HarmonyPatch(typeof(InternalBash), "ShutDownGameOver")]
    class ShutDownGameOver
    {
        private static bool ignoreGo = false;
        static bool Prefix(InternalBash __instance, bool isStrike, HelperGameOver helperGameOver, ref IEnumerator __result)
        {
            if (Networking.IsSinglePlayer() && DataUtils.hosting == true)
            {
                if (isStrike) return true;
                if (!ignoreGo)
                {
                    DataUtils.webHookHandler?.SendEmbedAsync("Server Info", "Server shutdown in 30 seconds!");
                    ignoreGo = true;
                }
                else
                {
                    ignoreGo = false;
                    FieldInfo fieldInfo = AccessTools.Field(typeof(ServerListener), "players");
                    foreach (var player in (fieldInfo.GetValue(ServerListener.Singleton) as ConcurrentDictionary<string, PlayerServer>).Values)
                    {
                        player.OnDisconnect();
                    }
                    return true;
                }
                object[] args = new object[] { isStrike, helperGameOver };
                __result = DelayedGameOver("ShutDownGameOver", __instance, args);
                return false;
            }
            return true;
        }
    }

    private static IEnumerator DelayedGameOver(string method, InternalBash __instance, object[] args)
    {
        int delay = 35;
        AdminMessageHandler.AdminMsgToClients("", AdminMessage.MsgType.SERVER_RESTART, true, delay - 5);
        yield return new WaitForSeconds(delay);
        var call = AccessTools.Method(typeof(InternalBash), method);
        yield return (IEnumerator)call.Invoke(__instance, args);
    }
}