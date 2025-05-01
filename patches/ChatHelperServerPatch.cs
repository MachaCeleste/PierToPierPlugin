using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using PierToPierPlugin;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

[HarmonyPatch]
public class ChatHelperServerPatch
{
    private static string botName = "GreyChat";
    private static char cmdPrefix = '@';

    [HarmonyPatch(typeof(ChatHelperServer), "SendChatMSgServerRpc")]
    class SendChatMSgServerRpcPatch
    {
        static void Postfix(ChatHelperServer __instance, ref string message, ref string ipServer, ref int puerto, ref int windowPID)
        {
            string _message = Regex.Replace(message, "<.*?>", "");
            if (string.IsNullOrEmpty(_message) || string.IsNullOrEmpty(__instance.GetNickName()))
                return;
            FieldInfo playerField = AccessTools.Field(typeof(ChatHelperServer), "player");
            var player = playerField.GetValue(__instance) as PlayerServer;
            if (DataUtils.database.logChat)
                DataUtils.LogChat(message, player.chatHelper.GetNickName(), player.playerID, ipServer, puerto);
            if (_message.IndexOf(cmdPrefix) == 0 && _message.Length > 1)
            {
                _message = _message.Substring(1);
                string command = _message;
                string[] args = new string[0];
                if (_message.Contains(" "))
                {
                    string[] parts = _message.Split(new char[] { ' ' }, 2);
                    args = parts[1].Split(new char[] { ' ' });
                    command = parts[0];
                }
                switch (command.Trim().ToLower())
                {
                    case "hide":
                        if (args.Length < 1)
                        {
                            SendBotMessage("This command requires an argument.", ipServer, puerto, true, player.playerID);
                            return;
                        }
                        if (DataUtils.GetRole(player.playerID) >= Util.Roles.MODERATOR)
                        {
                            player.playerHelper.EnableAdminServerRpc(args[0].Trim().ToLower() == "true" ? true : false);
                        }
                        break;
                    case "help":
                        string help = $"\n" +
                                    $"<color=#f5a8b8>Help</color>\n" +
                                    $"Prefix: {cmdPrefix}\n" +
                                    $"Commands:\n" +
                                    $"->help:\n" +
                                    $"    Shows this help message";
                        if (DataUtils.GetRole(player.playerID) >= Util.Roles.MODERATOR)
                        {
                            help += $"\n" +
                                    $"->hide:\n" +
                                    $"    Hides your admin/moderator status\n";
                        }
                        SendBotMessage(help, ipServer, puerto, true, player.playerID);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void SendBotMessage(string message, string ipServer, int puerto, bool ephemeral, string playerID = "")
        {
            byte[] value = GCompressor.Zip(JsonConvert.SerializeObject(new PlayerUtilsChat.ChatMessage(message, botName, ipServer, puerto, Util.Roles.SUPERADMIN)));
            MessageClient messageClient = new MessageClient(IdClient.SendChatMsgClientRpc);
            messageClient.AddByte(value);
            if (ephemeral)
                ServerListener.Singleton.SendToPlayer(messageClient, playerID);
            else if (ipServer.Equals("unknown"))
                ServerListener.Singleton.SendToPlayers(messageClient);
            else
            {
                List<GlobalChat.UserChat> clientIds = GlobalChat.Singleton.AddChat(ipServer, puerto, Computer.Md5Sum(botName), botName, Util.Roles.SUPERADMIN);
                ServerListener.Singleton.SendToPlayers(messageClient, clientIds);
            }
        }
    }
}