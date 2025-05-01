using NetworkMessages;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Util;

namespace PierToPierPlugin
{
    public class ValidationHandler : MonoBehaviour
    {
        public bool? isValid = null;
        private byte[] ticketBinary;
        private ulong steamID;
        private TcpClient client;
        private NetworkStream stream;
        private bool disconnected;

        //bool isValid = false;
        //Task.Run(async () =>
        //{
        //    ValidationHandler validation = new ValidationHandler();
        //    validation.Init(_ticketBinary, _steamID);
        //    await validation.ValidateAsync();
        //    isValid = (bool)validation.isValid;
        //}).Wait();
        //if (!isValid)
        //{
        //    messageClient.AddString("Error: Steam auth session failed.\nPlease restart your Steam client and try again after a few minutes.");
        //    player.SendData(messageClient);
        //    __result = false;
        //}

        public void Init(byte[] ticketBinary, ulong steamID)
        {
            this.ticketBinary = ticketBinary;
            this.steamID = steamID;
        }

        public async Task<bool> ValidateAsync()
        {
            bool result = await ConnectServerAsync();
            return result;
        }

        private async Task<bool> ConnectServerAsync()
        {
            try
            {
                bool connected = await ConnectToServerAsync();
                if (connected)
                {
                    MessageServer messageServer = new MessageServer(IdServer.UserLoginServerRpc);
                    messageServer.AddByte(this.ticketBinary);
                    messageServer.AddULong(this.steamID);
                    messageServer.AddBool(false);
                    messageServer.AddInt(Util.GameConfig.VersionClient);
                    await SendDataAsync(messageServer);
                }
                else
                {
                    InvalidClient();
                }
                return connected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection error: {ex.Message}");
                InvalidClient();
                return false;
            }
        }

        private async Task<bool> ConnectToServerAsync()
        {
            string host = GetHost();
            this.client = new TcpClient();
            try
            {
                await client.ConnectAsync(host, 21200);
                this.stream = client.GetStream();
                Task.Run(() => ReceiveDataAsync());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while connecting: {ex.Message}");
                return false;
            }
        }

        private string GetHost()
        {
            string host = "127.0.0.1";
            if (!Networking.IsSinglePlayer())
            {
                PlayerClient.ServerMode serverMode = PlayerClient.Singleton.serverMode;
                if (serverMode != PlayerClient.ServerMode.PUBLIC)
                {
                    host = serverMode == PlayerClient.ServerMode.NIGHTLY
                        ? PlayerClient.Singleton.nightlyAddress
                        : PlayerClient.Singleton.publicAddress;
                }
            }
            return host;
        }

        private async Task ReceiveDataAsync()
        {
            try
            {
                byte[] buffer = new byte[100000];
                string accumulatedData = "";
                int count;
                while ((count = await this.stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    accumulatedData += Encoding.UTF8.GetString(buffer, 0, count);
                    int newlineIndex;
                    while ((newlineIndex = accumulatedData.IndexOf('\n')) != -1)
                    {
                        string message = accumulatedData.Substring(0, newlineIndex);
                        accumulatedData = accumulatedData.Substring(newlineIndex + 1);
                        MessageClient m = JsonConvert.DeserializeObject<MessageClient>(message);
                        ProcessReceivedData(m);
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"IOException during ReceiveDataAsync: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            if (disconnected) return;
            Debug.Log("Closing validation client...");
            stream?.Close();
            client?.Close();
            disconnected = true;
        }

        private async Task SendDataAsync(MessageServer message)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message) + "\n");
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SendDataAsync error: {ex.Message}");
            }
        }

        private void ProcessReceivedData(MessageClient message)
        {
            switch (message.ID)
            {
                case IdClient.PlayerLoadClientRpc:
                    UnityThread.executeInUpdate(() => ValidClient());
                    break;

                case IdClient.DisconnectMessageClientRpc:
                    UnityThread.executeInUpdate(() => InvalidClient());
                    break;
            }
        }

        private void ValidClient()
        {
            Disconnect();
            isValid = true;
        }

        private void InvalidClient()
        {
            Disconnect();
            isValid = false;
        }
    }
}
