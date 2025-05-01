using NetworkMessages;
using Newtonsoft.Json;
using Util;

namespace PierToPierPlugin
{
    public static class AdminMessageHandler
    {
        public static void AdminMsgToClients(string message, AdminMessage.MsgType msgType, bool canClose = true, int restartTime = -1)
        {
            if (Networking.IsSinglePlayer() && DataUtils.hosting == true)
            {
                AdminMessage adminMessage = new AdminMessage(message, msgType, canClose, restartTime);
                byte[] value = GCompressor.Zip(JsonConvert.SerializeObject(adminMessage));
                MessageClient messageClient = new MessageClient(IdClient.AdminMsgClientRpc);
                messageClient.AddByte(value);
                ServerListener.Singleton.SendToPlayers(messageClient);
            }
        }

        public static void SendTextClient(PlayerServer player, int windowId, string message)
        {
            byte[] data = GCompressor.Zip(message);
            MessageClient messageClient = new MessageClient(IdClient.SendTextClientRpc);
            messageClient.AddByte(data);
            messageClient.AddInt(windowId);
            player.SendData(messageClient);
        }
    }
}