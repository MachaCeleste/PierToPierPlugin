using BepInEx;
using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;

namespace PierToPierPlugin
{
    internal class DataUtils
    {
        public static string host;
        public static string server;
        public static int port = 21200;
        public static bool hosting;
        public static string hostPortString;
        public static int hostPort = 21200;
        public static P2PDatabase database;
        public static WebhookHandler webHookHandler;
        public static WebhookHandler adminWebHookHandler;
        public static Dictionary<string, ulong> playerList = new Dictionary<string, ulong>();
        public static readonly string link = "http://steamcommunity.com/profiles/";

        internal static string dataFilePath = Path.Combine(Paths.ConfigPath, "P2PDatabase.json");

        public static void LoadDatabase()
        {
            if (!File.Exists(dataFilePath))
            {
                Plugin.Logger.LogWarning("No database found");
                database = new P2PDatabase();
                SaveDatabase();
                return;
            }
            string json = File.ReadAllText(dataFilePath);
            database = JsonConvert.DeserializeObject<P2PDatabase>(json);
            Plugin.Logger.LogInfo("Database loaded");
        }

        public static void SaveDatabase()
        {
            string json = JsonConvert.SerializeObject(database, Formatting.Indented);
            File.WriteAllText(dataFilePath, json);
            Plugin.Logger.LogInfo("Database saved");
        }

        public static void LogChat(string message, string nickName, string playerId, string ipServer, int port)
        {
            var dir = Path.Combine(Application.dataPath, "logs");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            DateTime timeNow = DateTime.Now;
            string logFilePath = Path.Combine(dir, $"log{timeNow.ToString("yyyyMMdd")}.log");
            string data = $"{timeNow.ToString("yyyyMMddHHmmss")}-{nickName}:{playerId}@{ipServer}:{port}> {message}";
            if (File.Exists(logFilePath))
                data = Environment.NewLine + data;
            File.AppendAllText(logFilePath, data);
        }

        public static IPEndPoint GetIpFromClient(PlayerServer player)
        {
            FieldInfo fieldInfo = AccessTools.Field(typeof(PlayerServer), "client");
            if (fieldInfo != null)
            {
                var client = fieldInfo.GetValue(player) as TcpClient;
                if (client != null)
                {
                    var endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    return endPoint;
                }
            }
            return null;
        }

        public static bool TryParseHost(string host, out string ip, out int port, out string error)
        {
            ip = null;
            port = 21200;
            error = null;
            if (string.IsNullOrWhiteSpace(host))
            {
                error = "Input cannot be empty";
                return false;
            }
            string[] parts = host.Split(':');
            if (parts.Length == 1)
            {
                if (IPAddress.TryParse(parts[0], out _))
                {
                    ip = parts[0];
                    return true;
                }
                error = "IP invalid";
            }
            else if (parts.Length == 2)
            {
                if (!IPAddress.TryParse(parts[0], out _))
                {
                    error = "IP invalid";
                    return false;
                }
                ip = parts[0];
                if (int.TryParse(parts[1], out int parsedPort))
                {
                    if (parsedPort < 0 || parsedPort > 65535)
                    {
                        error = $"Port out of range 0-65535 {parsedPort}";
                        return false;
                    }
                    port = parsedPort;
                    return true;
                }
                error = "Port invalid";
                return false;
            }
            error = "Input invalid";
            return false;
        }

        public static Util.Roles GetRole(string playerId)
        {
            Util.Roles role = Util.Roles.EVERYONE;
            if (database.moderators.FirstOrDefault(x => Computer.Md5Sum(x.ToString()) == playerId) != 0)
                role = Util.Roles.MODERATOR;
            if (database.admins.FirstOrDefault(x => Computer.Md5Sum(x.ToString()) == playerId) != 0)
                role = Util.Roles.ADMIN;
            if (ServerListener.Singleton.IsConnected(playerId))
            {
                var player = ServerListener.Singleton.GetPlayer(playerId);
                if (player != null)
                {
                    if (GetIpFromClient(player)?.Address.ToString() == "127.0.0.1")
                        role = Util.Roles.SUPERADMIN;
                }
            }
            return role;
        }

        public static void UpdateChatUser(string ownerClientID, string nickName, Util.Roles role)
        {
            FieldInfo chatsField = AccessTools.Field(typeof(GlobalChat), "currentChats");
            var currentChats = chatsField.GetValue(GlobalChat.Singleton) as ConcurrentDictionary<string, List<GlobalChat.UserChat>>;
            foreach (var channel in currentChats)
            {
                var user = channel.Value.FirstOrDefault(x => x.ownerPlayerID == ownerClientID);
                if (user != null)
                {
                    user.nickName = nickName;
                    user.role = role;
                }
                byte[] value = GCompressor.Zip(JsonConvert.SerializeObject(channel.Value));
                MessageClient messageClient = new MessageClient(IdClient.SendChatUsersClientRpc);
                messageClient.AddByte(value);
                messageClient.AddString(channel.Key);
                ServerListener.Singleton.SendToPlayers(messageClient);
            }
        }

        public static KeyValuePair<bool, string> CheckSteamAuth(byte[] ticketBinary, ulong steamId)
        {
            switch (SteamUser.BeginAuthSession(ticketBinary, steamId))
            {
                case BeginAuthResult.OK:
                    return new KeyValuePair<bool, string>(true, string.Empty);
                case BeginAuthResult.InvalidTicket:
                    return new KeyValuePair<bool, string>(false, "Steam auth session failed");
                case BeginAuthResult.DuplicateRequest:
                    return new KeyValuePair<bool, string>(false, "Steam auth duplicate request");
                case BeginAuthResult.InvalidVersion:
                    return new KeyValuePair<bool, string>(false, "Steam auth invalid version");
                case BeginAuthResult.GameMismatch:
                    return new KeyValuePair<bool, string>(false, "Steam auth game mismatch");
                case BeginAuthResult.ExpiredTicket:
                    return new KeyValuePair<bool, string>(false, "Steam auth session expired");
                default:
                    return new KeyValuePair<bool, string>(false, string.Empty);
            }
        }
    }

    internal class P2PDatabase
    {
        public List<ulong> admins { get; set; }
        public List<ulong> moderators { get; set; }
        public bool logChat { get; set; }
        public bool useWhiteList { get; set; }
        public List<ulong> whiteList { get; set; }
        public Dictionary<string, Ban> banList { get; set; }
        public Dictionary<string, List<Ban>> warnList { get; set; }
        public string appealLink { get; set; }
        public string webHookUrl { get; set; }
        public string adminWebHookUrl { get; set; }

        public P2PDatabase()
        {
            admins = new List<ulong>();
            moderators = new List<ulong>();
            whiteList = new List<ulong>();
            banList = new Dictionary<string, Ban>();
            warnList = new Dictionary<string, List<Ban>>();
            appealLink = string.Empty;
            webHookUrl = string.Empty;
            adminWebHookUrl = string.Empty;
        }
    }

    internal class Ban
    {
        public string ipAddress { get; set; }
        public ulong admin { get; set; }
        public DateTime banDate { get; set; }
        public string reason { get; set; }
        public bool wasIssued { get; set; }
    }
}