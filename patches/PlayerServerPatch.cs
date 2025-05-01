using PierToPierPlugin;
using HarmonyLib;
using Util;
using NetworkMessages;
using System;

[HarmonyPatch]
public class PlayerServerPatch
{
    [HarmonyPatch(typeof(PlayerServer), "Logged")]
    class LoggedPatch
    {
        static void Prefix(PlayerServer __instance, ref ulong steamID)
        {
            if (!Networking.IsSinglePlayer() || DataUtils.hosting != true) return;
            string data = $"Player joined! {DataUtils.link + steamID.ToString()}";
            DataUtils.adminWebHookHandler?.SendEmbedAsync("Server Info", data);
            Plugin.Logger.LogInfo(data);
            string playerID = Computer.Md5Sum(steamID.ToString());
            DataUtils.playerList.Add(playerID, steamID);
        }
    }

    [HarmonyPatch(typeof(PlayerServer), "OnDisconnect")]
    class OnDisconnectPatch
    {
        static void Prefix(PlayerServer __instance)
        {
            if (!Networking.IsSinglePlayer() || DataUtils.hosting != true) return;
            if (__instance.playerID == null) return;
            ulong steamID = DataUtils.playerList[__instance.playerID];
            DataUtils.playerList.Remove(__instance.playerID);
            __instance.chatHelper.ExitChat();
            string data = $"Player left! {DataUtils.link + steamID.ToString()}";
            DataUtils.adminWebHookHandler?.SendEmbedAsync("Server Info", data);
            Plugin.Logger.LogInfo(data);
        }
    }

    [HarmonyPatch(typeof(PlayerServer), "ProcessReceivedData")]
    static bool Prefix(PlayerServer __instance, ref MessageServer m)
    {
        if (Networking.IsSinglePlayer() && DataUtils.hosting == true)
        {
            if (__instance.playerHelper.IsGameAdmin())
            {
                try
                {
                    switch (m.ID)
                    {
                        case IdServer.AdminAction:
                            AdminCommandPatch.AdminActionServerRpc(__instance, m);
                            return false;
                        case IdServer.EnableAdminServerRpc:
                            __instance.playerHelper.EnableAdminServerRpc(m.GetBool());
                            return false;
                    }
                }
                catch (Exception message)
                {
                    Plugin.Logger.LogError(message);
                }
            }
        }
        return true;
    }
}