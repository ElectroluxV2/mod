using mod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NetPackagePartyData;

namespace mod.Commands
{
    class GParty
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            World world = GameManager.Instance.World;

            try
            {
                EntityPlayer leaderPlayer = world.Players.dict[sender.entityId];
                ClientInfo leaderPlayerCI = ConnectionManager.Instance.Clients.ForEntityId(leaderPlayer.entityId);

                leaderPlayer.CreateParty();
                leaderPlayerCI.SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(leaderPlayer.Party, sender.entityId, PartyActions.AutoJoin, false));
                leaderPlayerCI.SendPackage(NetPackageManager.GetPackage<NetPackageEntityAnimationData>().Setup(leaderPlayer.entityId, new List<AnimParamData>() { }));

                foreach (EntityPlayer entityPlayer in world.Players.list)
                {
                    if (entityPlayer.entityId == leaderPlayer.entityId) continue;

                    ClientInfo entityPlayerCI = ConnectionManager.Instance.Clients.ForEntityId(entityPlayer.entityId);


                    leaderPlayer.Party.AddPlayer(entityPlayer);

                    foreach (EntityPlayer entityPlayerSub in world.Players.list)
                    {
                        entityPlayerCI = ConnectionManager.Instance.Clients.ForEntityId(entityPlayerSub.entityId);
                        entityPlayerCI.SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(leaderPlayer.Party, entityPlayerCI.entityId, PartyActions.AutoJoin, false));
                    }
                }

            }
            catch (Exception e)
            {
                Log.Error(string.Format("Error in AddPlayerToTeam: {0}", e.Message));
                Log.Error(e.StackTrace);
                Log.Exception(e);
            }

            

            return false;
        }
    }
}
