using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace mod
{
    class Injections
    {
        public static bool DamageResponse_Prefix(EntityAlive __instance, DamageResponse _dmResponse)
        {
            try
            {
                //Log.Out("DamageResponse_Prefix");
                return false;
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Injections.DamageResponse_Prefix: {0}", e.Message));
            }
            return true;
        }

        public static void IsWithinTraderArea_Postfix(bool __result)
        {
            //Log.Out("IsWithinTraderArea_Postfix: " + __result);
            __result = true;
            //Log.Out("IsWithinTraderArea_Postfix: " + __result);
        }

        public static bool ChangeBlocks_Prefix(GameManager __instance, string persistentPlayerId, List<BlockChangeInfo> _blocksToChange)
        {
            //Log.Out("ChangeBlocks_Prefix");

            /*try
            {
                World world = __instance.World;

                if (__instance == null || _blocksToChange == null || string.IsNullOrEmpty(persistentPlayerId))
                {
                    return false;
                }


                for (int i = 0; i < _blocksToChange.Count; i++)
                {
                    BlockChangeInfo newBlockInfo = _blocksToChange[i]; //new block info
                    BlockValue blockValue = world.GetBlock(newBlockInfo.pos); //old block value

                   

                    if (newBlockInfo != null && newBlockInfo.bChangeBlockValue) //new block value
                    {

                        if (blockValue.type == BlockValue.Air.type) //old block was air
                        {
                            return true;
                        }

                        world.SetBlockRPC(newBlockInfo.clrIdx, newBlockInfo.pos, blockValue);
                        return false;
                    } 
                }
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Injections.ChangeBlocks_Prefix: {0}", e.Message));
            }*/
            return true;
        }

        public static bool ExplosionServer_Prefix(Vector3 _worldPos, int _playerId)
        {
            try
            {
                return false;
            }
            catch (Exception e)
            {
                Log.Out(string.Format("[SERVERTOOLS] Error in Injections.ExplosionServer_Prefix: {0}", e.Message));
            }
            return true;
        }

        public static bool OnEntityCollidedWithBlock_Prefix(Block __instance, WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
        {
            //Log.Out("OnEntityCollidedWithBlock_Prefix");
            return true;
        }
    }
}
