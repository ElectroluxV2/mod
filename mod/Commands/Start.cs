using mod.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using static mod.TeamMaker;

namespace mod.Commands
{
    class Start
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            World world = GameManager.Instance.World;

            if (arguments.Count < 2)
            {
                ChatManager.Message(sender, "[85144b]You ugly bitch [FF4136]/start mapId spawnGroup (teamCount)");
                return false;
            }

            string mapId = arguments[0].Trim();

            if (!VariableContainer.MapExists(mapId))
            {
                ChatManager.Message(sender, string.Format("[FF4136]Map [FF851B]{0} [FF4136]does not exist", mapId));
                return false;
            }

            int teamCount = 2;

            try
            {
                teamCount = int.Parse(arguments[2]);
            }
            catch (Exception) { }

            string spawnGroup = arguments[1].Trim();
            Map map = VariableContainer.GetMap(mapId);
            if (!map.spawns.ContainsKey(spawnGroup))
            {
                ChatManager.Message(sender, string.Format("[FF4136]Group [FF851B]{0}[FF4136] does not exist at [FF851B]{1}", spawnGroup, mapId));
                return false;
            }

            if (map.spawns[spawnGroup].Count < teamCount)
            {
                ChatManager.Message(sender, string.Format("[FF4136]Group [FF851B]{0}[FF4136] ([FF851B]{1}[FF4136] spawns) is to small for [FF851B]{2}[FF4136] teams",
                    spawnGroup,
                    map.spawns[spawnGroup].Count,
                    teamCount
                ));
                return false;
            }

            List<ClientInfo> playersToSplit = new List<ClientInfo>();
            foreach (EntityPlayer entityPlayer in world.Players.list)
            {
                ClientInfo client = ConnectionManager.Instance.Clients.ForEntityId(entityPlayer.entityId);
                playersToSplit.Add(client);
            }

            TeamMaker.SplitPlayers(playersToSplit, teamCount);

            VariableContainer.selectedMap = mapId;

            foreach (string teamName in TeamMaker.Teams.Keys)
            {
                Vector3 spawn = map.GetFreeSpawn(spawnGroup);
                TeamMaker.Teams[teamName].spawn = spawn;

                // Regenreate map after player spawn
                foreach (Team.Member player in TeamMaker.Teams[teamName].members)
                {
                    // Respawn event will give items
                    VariableContainer.SetPlayerState(player.pId, ModState.START_GAME);

                    // Kill player
                    Log.Out("Killing bc of start: " + player.nick);
                    player.EntityPlayer().DamageEntity(new DamageSource(EnumDamageSource.Internal, EnumDamageTypes.Suicide), 99999, false, 1f);
                }
            }
            return false;
        }
    }
}
