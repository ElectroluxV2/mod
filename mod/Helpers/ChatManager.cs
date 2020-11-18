using System.Collections.Generic;

namespace mod.Helpers
{
    class ChatManager
    {
        public static void Message(int to, string message)
        {
            Message(ConnectionManager.Instance.Clients.ForEntityId(to), message);
        }

        public static void Message(ClientInfo to, string message, string mainName = null)
        {
            to.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, to.entityId, message, mainName, false, null));
        }

        public static void Broadcast(string message, string mainName = null)
        {
            List<int> list = new List<int>();
            foreach (EntityPlayer player in GameManager.Instance.World.Players.list)
            {
                list.Add(player.entityId);
            }
            GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, message, mainName, false, list);
        }
    }
}
