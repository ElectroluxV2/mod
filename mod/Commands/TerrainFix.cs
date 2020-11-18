using mod.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace mod.Commands
{
    class TerrainFix
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            World world = GameManager.Instance.World;
            EntityPlayer entityPlayer = world.Players.dict[sender.entityId];
            Vector3 position = entityPlayer.position;
            PrefabInstance prefab = world.GetPOIAtPosition(position);

            // If no arguments just show POI info
            if (arguments.Count == 0)
            {
                if (prefab == null)
                {
                    ChatManager.Message(sender, "[FF0000]No POI found!");
                    
                }
                else
                {
                    ChatManager.Message(sender, string.Format("[FFCC00]POI: [DDDDDD]{0}", prefab.name));

                }
                return false;
            }

            // Fix everything around player
            int num = World.toChunkXZ((int)position.x) - 1;
            int num2 = World.toChunkXZ((int)position.z) - 1;
            int num3 = num + 2;
            int num4 = num2 + 2;

            HashSetLong hashSetLong = new HashSetLong();
            for (int k = num; k <= num3; k++)
            {
                for (int l2 = num2; l2 <= num4; l2++)
                {
                    hashSetLong.Add(WorldChunkCache.MakeChunkKey(k, l2));
                }
            }

            ChunkCluster chunkCache = world.ChunkCache;
            ChunkProviderGenerateWorld chunkProviderGenerateWorld = world.ChunkCache.ChunkProvider as ChunkProviderGenerateWorld;

            foreach (long key in hashSetLong)
            {
                if (!chunkProviderGenerateWorld.GenerateSingleChunk(chunkCache, key, true))
                {
                    ChatManager.Message(sender, string.Format("Failed regenerating chunk at position {0}/{1}", WorldChunkCache.extractX(key) << 4, WorldChunkCache.extractZ(key) << 4));
                }
            }

            world.m_ChunkManager.ResendChunksToClients(hashSetLong);

            if (prefab != null)
            {
                prefab.Reset(world);
            }

            ChatManager.Message(sender, "[44FF44]Reseted");
            return false;
        }
    }
}
