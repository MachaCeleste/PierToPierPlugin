using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using PierToPierPlugin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static AdminTool;

public class AdminCommandPatch
{
    public static void AdminActionServerRpc(PlayerServer playerServer, MessageServer m)
    {
        try
        {
            switch ((AdminAction)m.GetInt())
            {
                case AdminAction.SEARCH:
                    SearchPlayerServerRpc(playerServer, m.GetInt(), m.GetInt(), m.GetString());
                    break;
                //case AdminAction.SHOW_MUTED:
                //    ShowMutesServerRpc(playerServer, m.GetInt());
                //    break;
                case AdminAction.SHOW_BANNED:
                    ShowBansServerRpc(playerServer, m.GetInt());
                    break;
                case AdminAction.LOCK_CTF:
                    LockCTFServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.BAN_PLAYER:
                    BanPlayerServerRpc(playerServer, m.GetInt(), m.GetString(), m.GetString());
                    break;
                case AdminAction.UNBAN_PLAYER:
                    UnbanPlayerServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.SHOW_CTFS:
                    ShowCTFsServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.UNLOCK_CTF:
                    UnlockCTFServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.PLAYERS_ONLINE:
                    PlayersOnlineServerRpc(playerServer, m.GetInt());
                    break;
                case AdminAction.THREADS_INFO:
                    ShowThreadsServerRpc(playerServer, m.GetInt());
                    break;
                case AdminAction.SEND_MSG_CLIENTS:
                    SendToClientsServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.FIX_BANK_PAYMENTS:
                    FixBankPaymentsServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.WARN_PLAYER:
                    WarnPlayerServerRpc(playerServer, m.GetInt(), m.GetString(), m.GetString());
                    break;
                case AdminAction.SHOW_WARNS:
                    ShowWarnsServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.RESET_BANK_ACCOUNT:
                    ResetBankServerRpc(playerServer, m.GetInt(), m.GetString(), m.GetString());
                    break;
                case AdminAction.SHOW_BANK_TRANSACTIONS:
                    ShowBankTransactionsServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
                case AdminAction.DELETE_NETWORK:
                    DeleteNetworkServerRpc(playerServer, m.GetInt(), m.GetString());
                    break;
            }
        }
        catch (Exception message)
        {
            Debug.Log(message);
        }
    }

    private static void SearchPlayerServerRpc(PlayerServer playerServer, int searchType, int windowId, string data)
    {
        string output = "Player not found";
        string playerId = string.Empty;
        bool status = false;
        PlayerServer player;
        switch ((SearchType)searchType)
        {
            case SearchType.CHAT_NAME:
                playerId = Database.Singleton.GetPlayerID(data);
                if (string.IsNullOrEmpty(playerId)) break;
                if (ServerListener.Singleton.IsConnected(playerId))
                    status = true;
                break;
            case SearchType.PLAYERID:
                playerId = data;
                ulong.TryParse(playerId, out ulong steamId);
                if (steamId != 0)
                    playerId = Computer.Md5Sum(data);
                if (ServerListener.Singleton.IsConnected(data))
                    status = true;
                break;
            case SearchType.COMPUTERID:
                //player = ServerListener.Singleton.GetPlayerFromComputer(data); // Why is this missing
                //if (player == null)
                //{
                //    output = "Player must be online to search using computer ID";
                //    break;
                //}
                //playerId = player.GetPlayerID();
                output = "This no longer works... Thanks Kuro";
                status = true;
                break;
        }
        if (!string.IsNullOrEmpty(playerId))
        {
            string nicname = Database.Singleton.GetNickName(playerId);
            output = $"PlayerID:\n{playerId}\n" +
                    $"Nickname:\n" + (string.IsNullOrEmpty(nicname) ? "[Not Assigned]" : nicname) + "\n" +
                    $"Role:\n{DataUtils.GetRole(playerId).ToString()}\n" +
                    $"Status:\n" + (status ? "ONLINE" : "OFFLINE") + "\n" +
                    $"Last connected:\n{Database.Singleton.GetConnTime(playerId)}\n" +
                    $"{Database.Singleton.GetPlayerInfoMods(playerId)}";
            if (DataUtils.database.banList.ContainsKey(playerId))
            {
                var ban = DataUtils.database.banList[playerId];
                output += $"\nBan Date:\n{ban.banDate}\n" +
                        $"Admin:\n{ban.admin}\n" +
                        $"IP Address:\n{ban.ipAddress}\n" +
                        $"Reason:\n{ban.reason}";
            }
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    }

    //private static void ShowMutesServerRpc(PlayerServer playerServer, int windowId)
    //{
    //    var output = GlobalChat.Singleton.GetMutedUsers();
    //    AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    //}

    private static void ShowBansServerRpc(PlayerServer playerServer, int windowId)
    {
        string output = "No bans found.";
        if (DataUtils.database.banList.Count > 0)
        {
            foreach (var ban in DataUtils.database.banList)
            {
                output += $"Player ID: {ban.Key}\n";
            }
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    }

    private static void LockCTFServerRpc(PlayerServer playerServer, int windowId, string ctfName)
    {
        CTF_Event ctfEvent = Database.Singleton.GetEvent(ctfName);
        if (ctfEvent == null)
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, "CTF Mission not found");
            return;
        }
        ctfEvent.LockMission(true);
    }

    private static void BanPlayerServerRpc(PlayerServer playerServer, int windowId, string playerId, string reason)
    {
        ulong.TryParse(playerId, out ulong steamID);
        if (steamID != 0)
            playerId = Computer.Md5Sum(playerId);
        string fullReason = $"This game account has been banned\n\n<b>[Reason: {reason}]</b>\n\nPossible ban appeal via:\n<b>Discord: {DataUtils.database.appealLink}";
        DataUtils.database.banList.TryGetValue(playerId, out var result);
        if (result != null)
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"Player {playerId} is already banned.");
            return;
        }
        string ipAddress = string.Empty;
        var player = ServerListener.Singleton.GetPlayer(playerId);
        if (player != null)
        {
            if (player.playerHelper.IsGameAdmin())
            {
                AdminMessageHandler.SendTextClient(playerServer, windowId, $"You can not ban {playerId}, they are an admin!");
                return;
            }
            var endPoint = DataUtils.GetIpFromClient(player);
            if (endPoint != null)
                ipAddress = endPoint.Address.ToString();
            player.OnDisconnect();
        }
        Ban ban = new Ban()
        {
            ipAddress = ipAddress,
            admin = DataUtils.playerList[playerServer.playerID],
            banDate = DateTime.UtcNow,
            reason = fullReason,
            wasIssued = true
        };
        DataUtils.database.banList.Add(playerId, ban);
        DataUtils.SaveDatabase();
        string text = $"Player {playerId} was banned.\nReason:\n{reason}";
        AdminMessageHandler.SendTextClient(playerServer, windowId, text);
        DataUtils.adminWebHookHandler?.SendEmbedAsync("Server Info", text);
    }

    private static void UnbanPlayerServerRpc(PlayerServer playerServer, int windowId, string playerId)
    {
        DataUtils.database.banList.TryGetValue(playerId, out var result);
        if (result == null)
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"Player {playerId} is not banned.");
            return;
        }
        DataUtils.database.banList.Remove(playerId);
        DataUtils.SaveDatabase();
        string text = $"Player {playerId} was unbanned.";
        AdminMessageHandler.SendTextClient(playerServer, windowId, text);
        DataUtils.adminWebHookHandler?.SendEmbedAsync("Server Info", text);
    }

    private static void ShowCTFsServerRpc(PlayerServer playerServer, int windowId, string playerId)
    {
        ulong.TryParse(playerId, out ulong steamId);
        if (steamId != 0)
            playerId = Computer.Md5Sum(playerId);
        string output = "User has no CTF events.";
        var ctfEvents = Database.Singleton.GetEvents(playerId);
        if (ctfEvents.Count > 0)
        {
            output = "";
            foreach (var ctfEvent in ctfEvents)
            {
                output += $"Name: {ctfEvent.GetEventName()}\n" +
                        $"Description:\n{ctfEvent.GetDescription()}\n" +
                        $"Creator: {ctfEvent.GetCreatorName()}\n" +
                        $"PlayerID: {ctfEvent.GetOwnerPlayerID()}\n" +
                        $"Template: {ctfEvent.GetPublicAddress()}\n" +
                        $"Difficulty: {ctfEvent.GetDifficultRanking()}\n" +
                        $"Score: {ctfEvent.GetPlayerScore()}\n" +
                        $"Quality: {ctfEvent.GetQualityRanking()}\n" +
                        $"Votes: {ctfEvent.GetNumVotes()}\n" +
                        $"Published: {ctfEvent.IsPublished()}\n" +
                        $"Locked: {ctfEvent.IsLocked()}\n" +
                        $"Mail:\n{ctfEvent.GetMailContent()}\n" +
                        $"-------\n";
            }
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    }

    private static void UnlockCTFServerRpc(PlayerServer playerServer, int windowId, string ctfName)
    {
        CTF_Event ctfEvent = Database.Singleton.GetEvent(ctfName);
        if (ctfEvent == null)
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, "CTF Mission not found");
            return;
        }
        ctfEvent.LockMission(false);
    }

    private static void PlayersOnlineServerRpc(PlayerServer playerServer, int windowId)
    {
        string text = $"Players Online: {ServerListener.Singleton.GetNumPlayers()}\n";
        foreach (var playerSteam in DataUtils.playerList)
        {
            PlayerServer player = ServerListener.Singleton.GetPlayer(playerSteam.Key);
            if (player != null)
            {
                string nickname = Database.Singleton.GetNickName(playerSteam.Key);
                text += $"Steam ID:\n{playerSteam.Value}\n" +
                        $"Player ID:\n{playerSteam.Key}\n" +
                        $"Role:\n{DataUtils.GetRole(player.playerID).ToString()}\n" +
                        //$"Nickname:\n" + (string.IsNullOrEmpty(nickname) ? "[Not Defined]" : nickname) + $"\n" +
                        //$"Muted:\n" + (string.IsNullOrEmpty(nickname) ? "N/A" : GlobalChat.Singleton.IsUserMuted(Database.Singleton.GetPlayerID(nickname))) + $"\n" +
                        $"Computer ID:\n{player.GetComputerID()}\n" +
                        $"-------\n";
            }
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, text);
    }

    private static void ShowThreadsServerRpc(PlayerServer playerServer, int windowId)
    {
        FieldInfo fieldInfo = AccessTools.Field(typeof(ServerListener), "players");
        var players = fieldInfo.GetValue(ServerListener.Singleton) as ConcurrentDictionary<string, PlayerServer>;
        string output = "";
        foreach (var player in players)
        {
            output += $"Player ID: {player.Key}\n";
            var greyScriptHelper = player.Value.greyScriptHelper;
            var scripts = greyScriptHelper.helperScript;
            if (scripts.Count == 0)
            {
                output += "No scripts running.\n";
            }
            else
            {
                foreach (var script in scripts)
                {
                    var pid = script.Value.PID;
                    output += $"PID: {pid}\n";
                }
            }
            output += "-------\n";
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    }

    private static void SendToClientsServerRpc(PlayerServer playerServer, int windowId, string message)
    {
        AdminMessageHandler.AdminMsgToClients(message, AdminMessage.MsgType.CUSTOM);
        AdminMessageHandler.SendTextClient(playerServer, windowId, "Message sent.");
    }

    private static void FixBankPaymentsServerRpc(PlayerServer playerServer, int windowId, string playerId)
    {
        ulong.TryParse(playerId, out ulong steamId);
        if (steamId != 0)
            playerId = Computer.Md5Sum(playerId);
        BankSubs.Singleton.RemovePayments(playerId);
        string bankAccount = Database.Singleton.GetPlayerBankAccount(playerId);
        BankSubs.Singleton.AddPayment(playerId, BankSubs.PaymentID.BANK, 50, bankAccount, "");
        AdminMessageHandler.SendTextClient(playerServer, windowId, $"Player {playerId} all payments removed.");
    }

    private static void WarnPlayerServerRpc(PlayerServer playerServer, int windowId, string playerId, string reason)
    {
        ulong.TryParse(playerId, out ulong steamId);
        if (steamId != 0)
            playerId = Computer.Md5Sum(playerId);
        string ipAddress = string.Empty;
        var player = ServerListener.Singleton.GetPlayer(playerId);
        if (player != null)
        {
            if (player.playerHelper.IsGameAdmin())
            {
                AdminMessageHandler.SendTextClient(playerServer, windowId, $"You can not ban {playerId}, they are an admin!");
                return;
            }
            var endPoint = DataUtils.GetIpFromClient(playerServer);
            if (endPoint != null)
                ipAddress = endPoint.Address.ToString();
            MessageClient messageClient = new MessageClient(IdClient.SendWarningUserClientRpc);
            messageClient.AddByte(GCompressor.Zip(JsonConvert.SerializeObject(new List<string> { reason })));
            player.SendData(messageClient);
        }
        Ban warn = new Ban()
        {
            ipAddress = ipAddress,
            admin = DataUtils.playerList[playerServer.playerID],
            banDate = DateTime.UtcNow,
            reason = reason,
            wasIssued = player != null
        };
        DataUtils.database.warnList.TryGetValue(playerId, out var result);
        if (result == null)
            DataUtils.database.warnList.Add(playerId, new List<Ban> { warn });
        else
            result.Add(warn);
        DataUtils.SaveDatabase();
        string text = $"Player {playerId} was warned.";
        AdminMessageHandler.SendTextClient(playerServer, windowId, text);
        DataUtils.adminWebHookHandler?.SendEmbedAsync("Server Info", text);
    }

    private static void ShowWarnsServerRpc(PlayerServer playerServer, int windowId, string playerId)
    {
        ulong.TryParse(playerId, out var steamId);
        if (steamId != 0)
            playerId = Computer.Md5Sum(playerId);
        string output = $"Player {playerId} has no warnings.";
        if (DataUtils.database.warnList.ContainsKey(playerId))
        {
            output = $"Player ID: {playerId}\nWarnings:";
            foreach (Ban warn in DataUtils.database.warnList[playerId])
                output += $"\nIP Address:\n{warn.ipAddress}\n" +
                        $"Warning Date:\n{warn.banDate}\n" +
                        $"Admin:\n{warn.admin}\n" +
                        $"IP Address:\n{warn.ipAddress}\n" +
                        $"Reason:\n{warn.reason}" +
                        $"-------\n";
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    }

    private static void ResetBankServerRpc(PlayerServer playerServer, int windowId, string playerId, string amount)
    {
        ulong.TryParse(playerId, out var steamId);
        if (steamId != 0)
            playerId = Computer.Md5Sum(playerId);
        string bankAccount = Database.Singleton.GetPlayerBankAccount(playerId);
        if (string.IsNullOrEmpty(bankAccount))
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"Player {playerId} does not have a bank account.");
            return;
        }
        BankAccount playerBank = Database.Singleton.GetBankAccount(bankAccount, out string error);
        if (!string.IsNullOrEmpty(error))
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"Error getting player bank: {error}");
            return;
        }
        float.TryParse(amount, out var money);
        if (money == 0)
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"{amount} is not a valid number.");
            return;
        }
        playerBank.SetMoney(money);
        Database.Singleton.SyncBankAccount(playerBank);
        AdminMessageHandler.SendTextClient(playerServer, windowId, $"Player {playerId} bank account was reset to ${amount}.");
    }

    private static void ShowBankTransactionsServerRpc(PlayerServer playerServer, int windowId, string playerId)
    {
        ulong.TryParse(playerId, out ulong steamId);
        if (steamId != 0)
            playerId = Computer.Md5Sum(playerId);
        string bankAccount = Database.Singleton.GetPlayerBankAccount(playerId);
        if (string.IsNullOrEmpty(bankAccount))
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"Player {playerId} does not have a bank account.");
            return;
        }
        BankAccount playerBank = Database.Singleton.GetBankAccount(bankAccount, out string error);
        if (!string.IsNullOrEmpty(error))
        {
            AdminMessageHandler.SendTextClient(playerServer, windowId, $"Error getting player bank: {error}");
            return;
        }
        string output = "No transactions";
        var xfers = playerBank.GetTransacciones();
        if (xfers.Count > 0)
        {
            output = $"Listing transactions for {bankAccount}:\n";
            foreach (var xfer in xfers)
            {
                output += $"Account: {xfer.cuenta}\n" +
                        $"Amount: {xfer.cantidad}\n" +
                        $"Reason: {xfer.motivo}\n" +
                        $"Date: {xfer.fecha}\n" +
                        $"-------\n";
            }
        }
        AdminMessageHandler.SendTextClient(playerServer, windowId, output);
    }

    private static void DeleteNetworkServerRpc(PlayerServer playerServer, int windowId, string ipAddress)
    {
        Database.Singleton.DeleteNetwork(ipAddress);
        AdminMessageHandler.SendTextClient(playerServer, windowId, $"Network {ipAddress} deleted!");
    }
}