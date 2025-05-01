using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using PierToPierPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Util;

[HarmonyPatch]
public class PlayerHelperServerPatch
{
    [HarmonyPatch(typeof(PlayerHelperServer), "UserLoginServerRpc")]
    class UserLoginServerRpcPatch
    {
        static bool Prefix(ref byte[] ticketBinary, ref ulong steamID, ref int clientVersion, ref PlayerServer player, ref bool __result)
        {
            MessageClient messageClient = new MessageClient(IdClient.DisconnectMessageClientRpc);
            Type gameConfigType = AccessTools.TypeByName("Util.GameConfig");
            FieldInfo versionClientField = gameConfigType?.GetField("VersionClient", BindingFlags.Public | BindingFlags.Static);
            if (clientVersion != (int)versionClientField.GetValue(null))
            {
                messageClient.AddString("<color=red>A new game update is available.</color>\n\nIt's necessary that you update the game to be able to connect to the server.\nIf the update is not ready please restart your Steam client.");
                player.SendData(messageClient);
                __result = false;
                return false;
            }
            if (!new Regex("^[a-zA-Z0-9]*$").IsMatch(steamID.ToString()))
            {
                messageClient.AddString("Error: Invalid data.");
                player.SendData(messageClient);
                __result = false;
                return false;
            }
            var endPoint = DataUtils.GetIpFromClient(player);
            if (DataUtils.hosting == true && endPoint.Address.ToString() != "127.0.0.1")
            {
                if (ticketBinary == null || steamID == 0)
                {
                    DataUtils.adminWebHookHandler?.SendEmbedAsync("Invalid Data Warning", $"Player {DataUtils.link}{steamID} attempted to join with bad steam auth or ID.");
                    messageClient.AddString("Error: Steam token null or invalid steam ID.\nPlease restart your Steam client and try again after a few minutes.");
                    player.SendData(messageClient);
                    __result = false;
                    return false;
                }
                var kvp = DataUtils.CheckSteamAuth(ticketBinary, steamID);
                if (kvp.Key == false)
                {
                    DataUtils.adminWebHookHandler?.SendEmbedAsync("Steam Auth Warning", $"Player {DataUtils.link}{steamID} attempted to join Error: {kvp.Value}");
                    messageClient.AddString($"Error: {kvp.Value}.\nPlease restart your Steam client and try again after a few minutes.");
                    player.SendData(messageClient);
                    __result = false;
                    return false;
                }
                if (endPoint != null)
                {
                    var ban = DataUtils.database.banList.Values.FirstOrDefault(x => x.ipAddress == endPoint.Address.ToString());
                    if (ban != null)
                    {
                        DataUtils.adminWebHookHandler?.SendEmbedAsync("IP Ban Warning", $"Player {DataUtils.link}{steamID} attempted to join on an IP that was previously banned!");
                        messageClient.AddString(ban.reason);
                        player.SendData(messageClient);
                        __result = false;
                        return false;
                    }
                }
                string playerId = Computer.Md5Sum(steamID.ToString());
                if (DataUtils.database.banList.ContainsKey(playerId))
                {
                    DataUtils.adminWebHookHandler?.SendEmbedAsync("Ban Warning", $"Player {DataUtils.link}{steamID} attempted to join but was previously banned!");
                    string reason = DataUtils.database.banList[playerId].reason ?? string.Empty;
                    messageClient.AddString(reason);
                    player.SendData(messageClient);
                    __result = false;
                    return false;
                }
                if (DataUtils.database.useWhiteList && !DataUtils.database.whiteList.Contains(steamID))
                {
                    DataUtils.adminWebHookHandler?.SendEmbedAsync("Whitelist Warning", $"Player {DataUtils.link}{steamID} attemted to join but is not whitelisted!");
                    messageClient.AddString("This server has whitelisting setup and your player account is not listed on that list.\nContact your server admin to resolve the issue.");
                    player.SendData(messageClient);
                    __result = false;
                    return false;
                }
                if (DataUtils.database.warnList.ContainsKey(playerId))
                {
                    List<string> reasons = new List<string>();
                    foreach (Ban warn in DataUtils.database.warnList[playerId])
                    {
                        if (!warn.wasIssued)
                        {
                            reasons.Add(warn.reason);
                            warn.wasIssued = true;
                        }
                    }
                    DataUtils.SaveDatabase();
                    MessageClient warnClient = new MessageClient(IdClient.SendWarningUserClientRpc);
                    warnClient.AddByte(GCompressor.Zip(JsonConvert.SerializeObject(reasons)));
                    player.SendData(warnClient);
                }
            }
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerHelperServer), "UserLogin")]
    class UserLoginPatch
    {
        static void Prefix(PlayerHelperServer __instance)
        {
            if (Networking.IsSinglePlayer() && DataUtils.hosting == true)
            {
                FieldInfo playerField = AccessTools.Field(typeof(HelperServer), "player");
                if (playerField != null)
                {
                    var player = playerField.GetValue(__instance) as PlayerServer;
                    if (player != null)
                    {
                        DataUtils.playerList.TryGetValue(player.playerID, out ulong steamID);
                        FieldInfo roleField = AccessTools.Field(typeof(PlayerHelperServer), "role");
                        roleField?.SetValue(__instance, DataUtils.GetRole(player.playerID));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerHelperServer), "RestoreHomeUser")]
    class RestoreHomeUserPatch
    {
        static bool Prefix(PlayerServer __instance, ref PlayerComputer pc, ref string username)
        {
            FileSystem.Carpeta lastFolder = pc.GetFileSystem().GetLastFolder("/home/" + username + "/Desktop", username, false);
            List<string> list = new List<string>
            {
                "FileExplorer",
                "Terminal",
                "Map",
                "Mail",
                "Browser",
                "Notepad",
                "Manual",
                "CodeEditor",
                "Chat"
            };
            for (int i = 0; i < list.Count; i++)
            {
                FileSystem.Archivo archivo = new FileSystem.Archivo(list[i], username, false);
                string text = list[i].Replace(" ", "") + ".exe";
                archivo.SetComando(text);
                archivo.SetSymlink("/usr/bin/" + text);
                lastFolder.AddFile(archivo);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerHelperServer), "EnableAdminServerRpc")]
    class EnableAdminServerRpcPatch
    {
        static bool Prefix(PlayerHelperServer __instance, ref bool enable)
        {
            FieldInfo playerField = AccessTools.Field(typeof(PlayerHelperServer), "player");
            var player = playerField.GetValue(__instance) as PlayerServer;
            FieldInfo roleField = AccessTools.Field(typeof(PlayerHelperServer), "role");
            var setRole = Util.Roles.EVERYONE;
            if (enable)
            {
                roleField.SetValue(__instance, setRole);
            }
            if (!enable)
            {
                setRole = DataUtils.GetRole(player.playerID);
                roleField.SetValue(__instance, setRole);
            }
            DataUtils.UpdateChatUser(player.playerID, player.chatHelper.GetNickName(), setRole);
            return false;
        }
    }
}