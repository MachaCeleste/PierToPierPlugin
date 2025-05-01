using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PierToPierPlugin
{
    internal class HeadlessHandler
    {
        public static HeadlessHandler Singleton;

        private float bootDelay = 10f;
        private Thread consoleThread;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public HeadlessHandler()
        {
            HeadlessHandler.Singleton = this;
        }

        public IEnumerator StartHeadless()
        {
            yield return new WaitForSeconds(bootDelay);
            yield return null;
            RedGlobal.Singleton.OnNetworkSpawn();
            while (!RedGlobal.Singleton.gameLoaded)
            {
                yield return null;
            }
            Plugin.Logger.LogInfo("Headles mode online");
        }

        public void SetupConsole()
        {
            AllocConsole();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));

            Application.logMessageReceived += (condition, stackTrace, type) =>
            {
                Console.WriteLine($"[{type}] {condition}");
            };
            Plugin.Logger.LogEvent += (sender, e) =>
            {
                Console.WriteLine(e.ToStringLine());
            };
            Plugin.Logger.LogInfo("Console window initialized.");
        }

        public async void StartConsoleListener()
        {
            consoleThread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        Console.Out.Flush();
                        Console.Write(">");
                        string input = Console.ReadLine();
                        if (!string.IsNullOrEmpty(input))
                        {
                            string[] args = input.Trim().ToLower().Split(new char[] { ' ' });
                            if (args.Length > 0)
                            {
                                FieldInfo fieldInfo = AccessTools.Field(typeof(ServerListener), "players");
                                var players = (fieldInfo.GetValue(ServerListener.Singleton) as ConcurrentDictionary<string, PlayerServer>).Values;
                                switch (args[0])
                                {
                                    case "shutdown":
                                        int delay = 30;
                                        if (args.Length > 1 && int.TryParse(args[1], out int res) && res >= 10)
                                            delay = res;
                                        Plugin.Logger.LogInfo("Shutdown command received, Exiting...");
                                        DataUtils.webHookHandler?.SendEmbedAsync("Server Info", $"Server shutdown in {delay} seconds!");
                                        Plugin.Logger.LogInfo("Sending players shutdown notice...");
                                        AdminMessageHandler.AdminMsgToClients("", AdminMessage.MsgType.SERVER_RESTART, true, delay);
                                        Plugin.Logger.LogInfo("Waiting...");
                                        await Task.Delay(delay * 1000);
                                        foreach (var player in players)
                                        {
                                            player.OnDisconnect();
                                        }
                                        Plugin.Logger.LogInfo("Clients disconnected, safe to exit now.");
                                        break;
                                    case "numplayers":
                                        Plugin.Logger.LogInfo(ServerListener.Singleton.GetNumPlayers());
                                        break;
                                    case "players":
                                        var n = 0;
                                        foreach (var player in players)
                                        {
                                            Plugin.Logger.LogInfo($"[{n}]{DataUtils.playerList[player.playerID]} - {player.playerID}");
                                            n++;
                                        }
                                        break;
                                    case "help":
                                    case "-h":
                                    case "--help":
                                        Plugin.Logger.LogInfo($"--------------------- Help ---------------------\n" +
                                                              $"  numplayers : Displays a count of players currently online\n" +
                                                              $"     players : Displays info on all players currently online\n" +
                                                              $"    shutdown : Shutsdown the server, takes argument for how many" +
                                                              $"               seconds to warn players before shutdown (default 30s)\n" +
                                                              $"        help : Displays this help screen");
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger.LogError($"Console listener error: {ex}");
                    }
                }
            })
            {
                IsBackground = true
            };
            consoleThread.Start();
        }
    }
}