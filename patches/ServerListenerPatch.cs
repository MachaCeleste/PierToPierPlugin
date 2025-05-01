using HarmonyLib;
using NetworkMessages;
using Newtonsoft.Json;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PierToPierPlugin;

[HarmonyPatch]
public class ServerListenerPatch
{
    [HarmonyPatch(typeof(ServerListener), "ListenConnections")]
    class ListenConnectionsPatch
    {
        static bool Prefix(ServerListener __instance, ref Task __result)
        {
            exiting = false;
            if (DataUtils.hosting == true)
            {
                MyListenConnections(__instance);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ServerListener), "OnDestroy")]
    class OnDestroyPatch
    {
        static bool Prefix(ServerListener __instance)
        {
            if (DataUtils.hosting == true)
            {
                exiting = true;
                if (server != null)
                {
                    server.Stop();
                }
                return false;
            }
            return true;
        }
    }

    private static TcpListener server;
    private static bool exiting;

    private static async Task MyListenConnections(ServerListener __instance)
    {
        try
        {
            IPAddress localaddr = IPAddress.Parse("0.0.0.0");
            server = new TcpListener(localaddr, DataUtils.hostPort);
            server.Start();
            Debug.Log(">> Server online <<");
            RedGlobal.Singleton.gameLoaded = true;
            while (!exiting)
            {
                try
                {
                    bool shouldValidate = true;
                    TcpClient tcpClient = await server.AcceptTcpClientAsync();
                    tcpClient.Client.SetSocketKeepAliveValues(45000, 2000);
                    string ipClient = "unknown";
                    if (shouldValidate)
                    {
                        MyValidateClientAsync(tcpClient, ipClient);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.Message.Contains("Too many open files"))
                    {
                        throw;
                    }
                    if (ex.ErrorCode == 10057)
                    {
                        Debug.LogError("Socket not connected: connection interrupted.");
                    }
                    else
                    {
                        Debug.LogError("SocketException: " + ex);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Debug.Log("Socket disposed");
                }
                catch (Exception ex3)
                {
                    Debug.LogError("Unknown exception: " + ex3);
                }
            }
        }
        catch (SocketException ex4)
        {
            Debug.LogError("SocketException: " + ex4);
        }
        finally
        {
            Debug.Log("Game process finished");
            server.Stop();
            if (!exiting)
            {
                await Task.Delay(60000);
                var method = AccessTools.Method(typeof(ServerListener), "OnStartServer");
                method.Invoke(__instance, null);
            }
        }
    }

    private static async Task MyValidateClientAsync(TcpClient client, string ipClient)
    {
        PlayerServer playerServer = new PlayerServer(client);
        try
        {
            byte[] buffer = new byte[100000];
            Task<int> readTask = client.GetStream().ReadAsync(buffer, 0, buffer.Length);
            Task delayTask = Task.Delay(TimeSpan.FromSeconds(5.0));
            if (await Task.WhenAny(readTask, delayTask) == delayTask)
            {
                Debug.LogWarning("Login auth timeout");
                playerServer.OnDisconnect();
                return;
            }
            int count = await readTask;
            string @string = Encoding.UTF8.GetString(buffer, 0, count);
            if (MyIsValidClient(@string, playerServer, out var delayDisconnect))
            {
                new Thread((ThreadStart)delegate
                {
                    try
                    {
                        playerServer.ReceiveData();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Unhandled exception in ReceiveData: " + ex);
                    }
                    finally
                    {
                        ServerListener.Singleton.RemovePlayer(playerServer.playerID);
                    }
                }).Start();
            }
            else
            {
                if (delayDisconnect)
                {
                    await Task.Delay(10000);
                }
                Debug.LogWarning("Player failed to login. Connection closed");
                playerServer.OnDisconnect();
            }
        }
        catch (Exception message)
        {
            Debug.LogError(message);
            playerServer.OnDisconnect();
        }
    }

    private static bool MyIsValidClient(string data, PlayerServer playerServer, out bool delayDisconnect)
    {
        delayDisconnect = false;
        int num = data.IndexOf('\n');
        if (num != -1)
        {
            MessageServer messageServer = JsonConvert.DeserializeObject<MessageServer>(data.Substring(0, num));
            ulong @uLong = messageServer.GetULong();
            bool @bool = messageServer.GetBool();
            if (PlayerHelperServer.UserLoginServerRpc(messageServer.GetByte(), @uLong, messageServer.GetInt(), playerServer))
            {
                PlayerComputer pc = playerServer.Logged(@uLong);
                playerServer.playerHelper.UserLogin(@bool, pc);
                return true;
            }
            delayDisconnect = true;
        }
        return false;
    }
}