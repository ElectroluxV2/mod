using System;
using System.Collections.Generic;
using UnityEngine;
using static mod.Helpers.Map;

namespace mod.Helpers
{
    public enum ModState
    {
        IN_GAME,
        IN_LOBBY,
        RECONNECTING_TO_GAME,
        RECONNECTING_TO_LOBBY,
        START_GAME
    }

    internal class Map
    {
        internal class Spawn
        {
            public Vector3 location;
            public bool occupied;

            public Spawn(Vector3 location)
            {
                this.location = location;
                occupied = false;
            }
        }

        internal Dictionary<string, List<Spawn>> spawns = new Dictionary<string, List<Spawn>>();
        internal string name;

        internal Map(string name = null)
        {
            this.name = name;
        }

        internal void AddSpawn(string group, Vector3 location)
        {
            if (!spawns.ContainsKey(group))
            {
                spawns.Add(group, new List<Spawn>());
            }

            spawns[group].Add(new Spawn(location));
        }

        internal Vector3 GetFreeSpawn(string group)
        {
            if (!spawns.ContainsKey(group))
            {
                return Vector3.zero;
            }

            foreach (Spawn spawn in spawns[group])
            {
                if (spawn.occupied) continue;

                Log.Out(string.Format("Free spawn {0}", spawn.location.ToString()));

                spawn.occupied = true;
                return spawn.location;
            }

            return Vector3.zero;
        }

        internal void FreeSpawns()
        {
            foreach (string group in spawns.Keys)
            {
                foreach (Spawn spawn in spawns[group])
                {
                    spawn.occupied = false;
                }
            }
        }
    }

    class VariableContainer
    {

        public static Dictionary<string, int> TeamDeaths = new Dictionary<string, int>();

        private static Dictionary<string, ModState> PlayerState = new Dictionary<string, ModState>();

        private static Dictionary<string, string> PlayerLastTeam { get; } = new Dictionary<string, string>();

        internal static string selectedMap = "null";
        
        public static ModState GetPlayerState(string pId)
        {
            if (PlayerState.ContainsKey(pId)) {
                return PlayerState[pId];
            }
            else
            {
                return ModState.IN_GAME;
            }
        }

        public static void SetPlayerState(string pId, ModState state)
        {
            if (!PlayerState.ContainsKey(pId))
            {
               PlayerState.Add(pId, state);
            } else
            {
                PlayerState[pId] = state;
            }
        }

        public static void SetPlayerLastTeam(string pId, string team)
        {
            if (!PlayerLastTeam.ContainsKey(pId))
            {
                PlayerLastTeam.Add(pId, team);
            } else
            {
                PlayerLastTeam[pId] = team;
            }
        }

        public static string GetPlayerLastTeam(string pId)
        {
            if (PlayerLastTeam.ContainsKey(pId))
            {
                return PlayerLastTeam[pId];
            }
            else
            {
                return "TODO_DEFAULT_PLAYER_TEAM";
            }
        }

        private static Dictionary<string, string> PlayerClass = new Dictionary<string, string>();

        public static string GetPlayerClass(string pId)
        {
            if (PlayerClass.ContainsKey(pId))
            {
                return PlayerClass[pId];
            }
            else
            {
                return "tank";
            }
        }

        public static void SetPlayerClass(string pId, string className)
        {
            if (!PlayerClass.ContainsKey(pId))
            {
                PlayerClass.Add(pId, className);
            }
            else
            {
                PlayerClass[pId] = className;
            }
        }

        private static Vector3 LobbyPosition = Vector3.zero;

        public static void SetLobbyPosition(Vector3 newPosition, bool save = true)
        {
            LobbyPosition = newPosition;

            if (!save) return;

            Configs.Save();
        }

        public static void SetLobbyPosition(Vector3i newPosition, bool save = true)
        {
            SetLobbyPosition(newPosition.ToVector3(), save);
        }

        public static Vector3 GetLobbyPosition()
        {
            return LobbyPosition;
        }

        public static Vector3i GetLobbyPositionI()
        {
            return new Vector3i(LobbyPosition.x, LobbyPosition.y, LobbyPosition.z);
        }

        private static Dictionary<string, Map> Maps = new Dictionary<string, Map>();

        public static Map GetMap(string name)
        {
            return Maps[name];
        }

        public static bool MapExists(string name)
        {
            return Maps.ContainsKey(name);
        }

        public static void AddMap(Map map)
        {
            Maps.Add(map.name, map);
        }

        public static void SetMap(Map map)
        {
            if (MapExists(map.name))
            {
                Maps[map.name] = map;
            }
            else
            {
                AddMap(map);
            }
        }

        public static List<Map> ListMaps()
        {
            List<Map> ret = new List<Map>();

            foreach (Map m in Maps.Values)
            {
                ret.Add(m);
            }

            return ret;// new List<Map>(Maps.Values);
        }

        internal static void FreeMapSpawns()
        {
            foreach (Map map in Maps.Values)
            {
                foreach (List<Spawn> list in map.spawns.Values)
                {
                    foreach (Spawn spawn in list)
                    {
                        spawn.occupied = false;
                    }
                }
            }
        }
    }
}
