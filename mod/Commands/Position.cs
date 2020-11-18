using mod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace mod.Commands
{
    class Position
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            World world = GameManager.Instance.World;
            EntityPlayer entityPlayer = world.Players.dict[sender.entityId];

            Vector3 playerLocation = entityPlayer.getBellyPosition();
            string xyz = string.Format("X: [FFDC00]{0}[DDDDDD], Y: [FFDC00]{1}[DDDDDD], Z: [FFDC00]{2}",
                playerLocation.x,
                playerLocation.y,
                playerLocation.z
            );

            // Prints current position
            if (arguments.Count == 0)
            {              
                string msg = string.Format("[DDDDDD]Player: [FFDC00]{0}[DDDDDD], {1}",
                    sender.playerName.Trim(),
                    xyz
                );

                ChatManager.Message(sender, msg);
                return false;
            }
            // Lobby or map
            else if (arguments.Count >= 1)
            {
                if (arguments[0] == "lobby")
                {
                    VariableContainer.SetLobbyPosition(playerLocation);

                    string msg = string.Format("[DDDDDD]New pos for: [FFDC00]Lobby[DDDDDD], {0}", xyz);

                    ChatManager.Message(sender, msg);
                    return false;
                }

                // Must be map id
                string mapId = arguments[0].Trim();

                if (!VariableContainer.MapExists(mapId))
                {
                    VariableContainer.AddMap(new Map(mapId));
                    ChatManager.Message(sender, "[DDDDDD]Created new Map: [FFDC00]" + mapId);
                }

                Map map = VariableContainer.GetMap(mapId);

                string spawnFor = "team";

                if (arguments.Count >= 2)
                {
                    spawnFor = arguments[1].Trim();
                }

                if (arguments.Count >= 3)
                {
                    if (arguments[2].Trim() == "-d")
                    {
                        if (!map.spawns.ContainsKey(spawnFor))
                        {
                            ChatManager.Message(sender, string.Format("[DDDDDD]Group [FFDC00]{0} [DDDDDD]does not exist", spawnFor));
                            return false;
                        }

                        // Delete latest
                        int index = map.spawns[spawnFor].Count - 1;

                        if (arguments.Count == 4)
                        {
                            try
                            {
                                index = int.Parse(arguments[3].Trim());
                            } catch { }
                        }

                        map.spawns[spawnFor].RemoveAt(index);
                        if (map.spawns[spawnFor].Count == 0)
                        {
                            map.spawns.Remove(spawnFor);
                            ChatManager.Message(sender, string.Format("[DDDDDD]Group [FFDC00]{0}[DDDDDD] at map [FFDC00]{1}[DDDDDD] has been deleted", spawnFor, mapId));
                        }
                        else
                        {
                            ChatManager.Message(sender, string.Format("[DDDDDD]Removed spawn for group [FFDC00]{0}[DDDDDD] at map [FFDC00]{1}", spawnFor, mapId));
                        }
                    }
                    else
                    {
                        ChatManager.Message(sender, "[DDDDDD]Usage: /pos mapId spawnGroup [FFDC00]-d [index]");
                        return false;
                    }
                }
                else
                {
                    // Add spawn for group
                    map.AddSpawn(spawnFor, playerLocation);
                    ChatManager.Message(sender, string.Format("[DDDDDD]New pos for: [FFDC00]{0} ({1})[DDDDDD], {2}", mapId, spawnFor, xyz));

                }

                // Save
                VariableContainer.SetMap(map);
                Configs.Save();
                return false;
            }
            else
            {
                ChatManager.Message(sender, "[DDDDDD]Usage: [FFDC00]/pos [lobby/mapId] [spawnGroup] [-d [index]]");
                return false;
            }
        }
    }
}
