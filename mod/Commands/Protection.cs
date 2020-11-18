using mod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace mod.Commands
{
    class Protection
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            World world = GameManager.Instance.World;
            EntityPlayer entityPlayer = world.Players.dict[sender.entityId];
            Vector3i blockPosition = entityPlayer.GetBlockPosition();
            Chunk chunk = (Chunk) GameManager.Instance.World.GetChunkFromWorldPos(blockPosition);

            // Just show current state
            if (arguments.Count == 0)
            {
                if (chunk.IsAnyTraderArea())
                {
                    ChatManager.Message(sender, string.Format(
                        "[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [01FF70]Protected",
                        blockPosition.x,
                        blockPosition.z
                    ));
                }
                else
                {
                    ChatManager.Message(sender, string.Format(
                        "[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [FF4136]Unprotected",
                        blockPosition.x,
                        blockPosition.z
                    ));
                }
                return false;
            }

            bool enable = true;
            try
            {
                enable = bool.Parse(arguments[1]);
            }
            catch (Exception) { }

            //int x = l.x & 0xF;
            //int z = l.z & 0xF;

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    chunk.SetTraderArea(x, z, enable);
                }
            }

            if (enable)
                ChatManager.Message(sender, string.Format(
                    "[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [01FF70]Enabled",
                    blockPosition.x,
                    blockPosition.z
                ));
            else
                ChatManager.Message(sender, string.Format(
                    "[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [FF4136]Disabled",
                    blockPosition.x,
                    blockPosition.z
                ));

            foreach (ClientInfo clientInfo in ConnectionManager.Instance.Clients.List)
            {
                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChunk>().Setup(chunk, true));
            }

            return false;
        }
    }
}
