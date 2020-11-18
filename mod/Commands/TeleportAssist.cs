using mod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace mod.Commands
{
    class TeleportAssist
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            World world = GameManager.Instance.World;
            if (arguments.Count < 1)
            {
                ChatManager.Message(sender, "[85144b]You ugly bitch  [FF4136]/tp who or what");
                return false;
            }

            string what = arguments[0].Trim();
            Vector3 destination = Vector3.zero;

            // Search in players
            bool found = false;
            foreach (ClientInfo clientInfo in ConnectionManager.Instance.Clients.List)
            {
                if (clientInfo.playerName == null) continue;
                if (!clientInfo.playerName.Contains(what)) continue;

                EntityPlayer entityPlayer = world.Players.dict[clientInfo.entityId];
                destination = entityPlayer.position;
                found = true;
                break;
            }

            if (found)
            {
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                return false;
            }

            // Chceck all maps
            foreach (Map map in VariableContainer.ListMaps())
            {
                if (!map.name.Contains(what)) continue;

                string spawnFor = "team";

                if (arguments.Count >= 2)
                {
                    spawnFor = arguments[1].Trim();
                }

                if (!map.spawns.ContainsKey(spawnFor))
                {
                    ChatManager.Message(sender, string.Format("[DDDDDD]Group [FFDC00]{0} [DDDDDD]does not exist", spawnFor));
                    return false;
                }

                // Get latest
                int index = map.spawns[spawnFor].Count - 1;

                if (arguments.Count == 3)
                {
                    try
                    {
                        index = int.Parse(arguments[2].Trim());
                    }
                    catch { }
                }

                destination = map.spawns[spawnFor][index].location;
                found = true;
                break;
            }

            if (found)
            {
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                return false;
            }

            if (arguments.Count == 2)
            {
                int x = int.Parse(arguments[0].Trim());
                int z = int.Parse(arguments[1].Trim());

                int y = 128;
                if (world.IsChunkAreaLoaded(x, 0, z))
                {
                    Chunk c = (Chunk) world.GetChunkFromWorldPos(x, 0, z);
                    y = c.GetTerrainHeight(x & 0xF, z & 0xF) + 1;
                }

                destination = new Vector3(x, y, z);

                sender.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                return false;
            }

            ChatManager.Message(sender, "[85144b]You ugly bitch  [FF4136]I can't find Your dreamed up hero!");
            return false;
        }
    }
}
