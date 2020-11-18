using System;
using System.Collections.Generic;
using System.Text;
using mod.Commands;
using mod.Helpers;
using UnityEngine;
using static mod.TeamMaker;

namespace mod
{
    class Events
    {
        private static readonly int DEATHS_TO_LOOSE = 16;
        private static List<Vector3> refubrishedCords = new List<Vector3>();

        internal static void PlayerSpawnedInWorld(ClientInfo player, RespawnType respawnReason, Vector3i _pos)
        {
            string pId = player.playerId;
            ModState playerState = VariableContainer.GetPlayerState(pId);
            World world = GameManager.Instance.World;

            if (respawnReason.Equals(RespawnType.Died))
            {
                if (playerState.Equals(ModState.RECONNECTING_TO_GAME) || playerState.Equals(ModState.IN_GAME) || playerState.Equals(ModState.START_GAME))
                {

                    if (playerState.Equals(ModState.RECONNECTING_TO_GAME))
                    {
                        Team.Member member = new Team.Member
                        {
                            entityId = player.entityId,
                            nick = player.playerName,
                            pId = pId
                        };

                        VariableContainer.SetPlayerState(pId, ModState.IN_GAME);
                        TeamMaker.AddPlayerToTeam(member, VariableContainer.GetPlayerLastTeam(pId));
                    }

                    // Has no items, teleport to team spawn and give items
                    Map map = VariableContainer.GetMap(VariableContainer.selectedMap);
                    Vector3 spawn = TeamMaker.GetPlayerTeam(pId).spawn;

                    Log.Out(string.Format("Spawn for {0} is {1}", player.playerName, spawn.ToString()));

                    // Find random spor around spawn
                    Vector3 destination = Vector3.zero;
                    //if (!world.GetRandomSpawnPositionMinMaxToPosition(spawn, 0, 2, 2, false, out destination, true))
                   // {
                        destination = spawn;
                    //}

                    player.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));

                    // ReGen
                    // Rebuild terrain around spawn 
                    if (!refubrishedCords.Contains(spawn))
                    {
                        // But only once
                        refubrishedCords.Add(spawn);

                        PrefabInstance prefab = GameManager.Instance.World.GetPOIAtPosition(spawn);

                        int num = World.toChunkXZ((int)spawn.x) - 1;
                        int num2 = World.toChunkXZ((int)spawn.z) - 1;
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
                                ChatManager.Message(player, string.Format("[FF4136]Failed regenerating chunk at position [FF851B]{0}[FF4136]/[FF851B]{1}", WorldChunkCache.extractX(key) << 4, WorldChunkCache.extractZ(key) << 4));
                            }
                        }

                        world.m_ChunkManager.ResendChunksToClients(hashSetLong);

                        if (prefab != null)
                        {
                            prefab.Reset(world);
                        }
                    }

                    // Give items
                    ClassManager.ApplyClass(player);

                    if (VariableContainer.GetPlayerState(pId).Equals(ModState.START_GAME))
                    {
                        VariableContainer.SetPlayerState(pId, ModState.IN_GAME);
                    } else
                    {
                        HandleDiedInGame(player);
                    }
                    return;
                }
                else
                {
                    VariableContainer.SetPlayerState(pId, ModState.IN_LOBBY);
                    player.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(VariableContainer.GetLobbyPosition(), null, false));
                    return;
                }
            }

            if (respawnReason.Equals(RespawnType.Teleport)) return;

            if (VariableContainer.GetPlayerState(pId).Equals(ModState.RECONNECTING_TO_GAME))
            {
                // Have to kill reconected player
                Log.Out("Killing bc of reconnect: " + player.playerName);
                world.Players.dict[player.entityId].DamageEntity(new DamageSource(EnumDamageSource.Internal, EnumDamageTypes.Suicide), 99999, false, 1f);
                return;
            }

            if (VariableContainer.selectedMap == "null")
            {
                VariableContainer.SetPlayerState(pId, ModState.IN_LOBBY);
                player.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(VariableContainer.GetLobbyPosition(), null, false));
            }
        }

        private static void HandleDiedInGame(ClientInfo player)
        {
            Map map = VariableContainer.GetMap(VariableContainer.selectedMap);
            Team team = TeamMaker.GetPlayerTeam(player.playerId);

            if (!VariableContainer.TeamDeaths.ContainsKey(team.id))
            {
                VariableContainer.TeamDeaths.Add(team.id, 0);
            }

            VariableContainer.TeamDeaths[team.id]++;

            foreach (Team.Member member in team.members)
            {
                member.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, member.entityId, "[FF3333]" + team.id + " Deaths to loose: " + (DEATHS_TO_LOOSE - VariableContainer.TeamDeaths[team.id]), null, false, null));
            }

            if (VariableContainer.TeamDeaths[team.id] < DEATHS_TO_LOOSE)
            {
                return;
            }

            VariableContainer.selectedMap = "none";
            refubrishedCords.Clear();
            VariableContainer.FreeMapSpawns();

            string msg = string.Format("[FFAAAA]Team of {0} [FFAAAA]has failed miserable.", team.GetMembers());
            foreach (ClientInfo pcli in ConnectionManager.Instance.Clients.List)
            {
                if (pcli == null) continue;

                string pId = pcli.playerId;
                VariableContainer.SetPlayerState(pId, ModState.IN_LOBBY);
                pcli.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(VariableContainer.GetLobbyPosition(), null, false));
                pcli.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, pcli.entityId, msg, null, false, null));
            }

            TeamMaker.CleanUp();
        }

        internal static void PlayerDisconnected(ClientInfo player, bool arg2)
        {
            string pId = player.playerId;

            string teamId = TeamMaker.GetPlayerTeam(pId).id;
            if (teamId != null)
            {
                VariableContainer.SetPlayerState(pId, ModState.RECONNECTING_TO_GAME);
                VariableContainer.SetPlayerLastTeam(pId, teamId);
                TeamMaker.RemovePlayerAfterDisconnect(player.playerId, teamId);
            }
            else
            {
                VariableContainer.SetPlayerState(pId, ModState.RECONNECTING_TO_LOBBY);
            }
        }

        public static bool ChatMessage(ClientInfo clientInfo, EChatType _type, int _senderId, string message, string mainName, bool _localizeMain, List<int> _recipientEntityIds)
        {
            // We make sure there is an actual message and a client, and also ignore the message if it's from the server.
            if (!string.IsNullOrEmpty(message) && clientInfo != null && mainName != null)
            {
                //We check to see if the message starts with a /
                if (message.Trim().StartsWith("/"))
                {
                    return ChatCommands.Handle(message, clientInfo);

                    /**
                    //we then remove that / to get the rest of the message.
                    string[] cmd = message.Trim().Remove(0, 1).Split(' ');

                    if (cmd[0] == "end")
                    {
                        foreach (ClientInfo pcli in ConnectionManager.Instance.Clients.List)
                        {
                            if (pcli == null) continue;

                            Configs.selectedMap = "none";

                            foreach (string key in Events.TeamDeaths.Keys)
                            {
                                Events.TeamDeaths[key] = 0;
                            }

                            pcli.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(Configs.LobbyPosition, null, false));
                            pcli.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, pcli.entityId, "[FF3333] END", null, false, null));
                        }
                        return false;
                    }

                    if (Configs.selectedMap != "none") return true;

                    if (cmd[0] == "zombie")
                    {
                        try
                        {
                            World world = GameManager.Instance.World;
                            EntityPlayer player = world.Players.dict[clientInfo.entityId];

                            HashSet<int> classIds = new HashSet<int>();
                            foreach (var g in EntityGroups.list.Dict)
                            {
                                //Log.Out(string.Format("Key = {0}\n\n", g.Key));
                                foreach (var gg in g.Value)
                                {
                                    classIds.Add(gg.entityClassId);

                                    //Log.Out(string.Format("prob = {0}", gg.prob));
                                    //Log.Out(string.Format("reqMin = {0}", gg.reqMin));
                                    //Log.Out(string.Format("reqMax = {0}", gg.reqMax));
                                }
                            }

                            foreach (int id in classIds)
                            {
                                //Log.Out(string.Format("entityClassId = {0}", id));
                                int et = id;//EntityClass.FromString("animalZombieVultureRadiated");

                                Vector3 vector;
                                if (!world.GetRandomSpawnPositionMinMaxToPosition(player.position, 1, 10, 1, false, out vector))
                                {
                                    Log.Out("Cant find spot wtf?");
                                    return false;
                                }

                                Entity entity = EntityFactory.CreateEntity(et, vector);
                                //if (entity.GetType() != typeof(EntityEnemy)) continue;

                                EntityEnemy entityEnemy = (EntityEnemy)entity;

                                world.SpawnEntityInWorld(entityEnemy);
                                entityEnemy.SetSpawnerSource(EnumSpawnerSource.Dynamic);
                                entityEnemy.IsHordeZombie = false;
                                entityEnemy.IsBloodMoon = false;
                                entityEnemy.bIsChunkObserver = true;
                                entityEnemy.timeStayAfterDeath /= 3;
                                entityEnemy.lootDropProb = 0;
                            }




                        }
                        catch (Exception e)
                        {
                            Log.Error(string.Format("Error in z cmd: {0}", e.Message));
                            Log.Error(e.StackTrace);
                            Log.Exception(e);
                        }
                    }
                    else if (cmd[0] == "class")
                    {
                        if (cmd.Length < 2)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF3333]Tank / Soldier / Scout ", null, false, null));
                            return false;
                        }

                        List<string> classes = new List<string>() { "tank", "soldier", "scout" };

                        string className = cmd[1].Trim().ToLower();

                        if (!classes.Contains(className))
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF3333]Tank / Soldier / Scout ", null, false, null));
                            return false;
                        }

                        string pId = clientInfo.playerId;

                        if (!Configs.PlayerClasses.ContainsKey(pId))
                        {
                            Configs.PlayerClasses.Add(pId, "");
                        }

                        Configs.PlayerClasses[pId] = className;
                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[FF3333]Selected: {0} ", className), null, false, null));
                    }
                    else if (cmd[0] == "die")
                    {
                        World world = GameManager.Instance.World;
                        foreach (EntityPlayer entityPlayer in world.Players.list)
                        {
                            if (!entityPlayer.IsSpawned()) continue;
                            if (entityPlayer.IsDead()) continue;
                            if (entityPlayer == null) continue;

                            entityPlayer.DamageEntity(new DamageSource(EnumDamageSource.Internal, EnumDamageTypes.Suicide), 99999, false, 1f);



                            /*EntityCreationData entityCreationData = new EntityCreationData()
                            {
                                clientEntityId = entityPlayer.clientEntityId,
                                belongsPlayerId = entityPlayer.belongsPlayerId,
                                entityClass = EntityClass.FromString("playerMale"),
                                id = EntityFactory.nextEntityID++,
                            };


                            EntityPlayerLocal entityPlayerLocal = EntityFactory.CreateEntity(entityCreationData) as EntityPlayerLocal;


                            //EntityPlayerLocal entityPlayerLocal = BuildLocalPlayer(ci);

                            /*if (entityPlayerLocal == null)
                            {
                                Log.Out("nah");
                                continue;
                            }

                            entityPlayerLocal.clientEntityId = entityPlayer.clientEntityId;
                            entityPlayerLocal.belongsPlayerId = entityPlayer.belongsPlayerId;
                            entityPlayerLocal.inventory = entityPlayer.inventory;
                            entityPlayerLocal.bag = entityPlayer.bag;
                            entityPlayerLocal.equipment = entityPlayer.equipment;
                            entityPlayerLocal.persistentPlayerData = GameManager.Instance.GetPersistentPlayerList().GetPlayerDataFromEntityID(entityPlayer.entityId);

                            if (entityPlayerLocal == null)
                            {
                                Log.Out("nah");
                                continue;
                            }

                            //EntityPlayerLocal entityPlayerLocal = entityPlayer.GetAttachedPlayerLocal();

                            //Log.Out(entityPlayerLocal.ToString());

                            //entityPlayerLocal.inventory.Clear();
                            //entityPlayerLocal.inventory.AddItem(itemStack);

                            //entityPlayerLocal.bag.Clear();


                            //world.SpawnEntityInWorld(entityItem);


                            //ci.SendPackage(NetPackageManager.GetPackage<NetPackagePlayerInventory>().Setup(entityPlayerLocal, true, true, false));
                            //ci.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityItem.entityId, entityPlayer.entityId));

                            //world.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Despawned);
                        }
                    }
                    else if (cmd[0] == "tp")
                    {
                        if (cmd.Length < 2)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[85144b]You ugly bitch  [FF4136]/tp who or what", null, false, null));
                            return false;
                        }

                        string what = cmd[1].Trim();

                        Vector3 destination = new Vector3();

                        bool found = false;
                        foreach (EntityPlayer player in GameManager.Instance.World.Players.list)
                        {
                            ClientInfo client = ConnectionManager.Instance.Clients.ForEntityId(player.entityId);
                            if (client.playerName.Contains(what))
                            {
                                destination = player.position;
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                            return false;
                        }

                        foreach (Configs.Map map in Configs.Maps.Values)
                        {
                            if (map.name.ToLower().Contains(what.ToLower()))
                            {
                                string type = "team";
                                if (cmd.Length >= 3)
                                {
                                    type = cmd[2].Trim().ToLower();
                                }

                                int index = 0;
                                if (cmd.Length >= 4)
                                {
                                    index = int.Parse(cmd[3].Trim());
                                }

                                if (type == "team" && map.spawnsForTeams.Count > 0)
                                {
                                    destination = map.spawnsForTeams[index];
                                }
                                else
                                {
                                    destination = map.spawnsForEnemies[index];
                                }

                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                            return false;
                        }

                        if (cmd.Length == 3)
                        {
                            World world = GameManager.Instance.World;

                            int x = int.Parse(cmd[1].Trim());
                            int z = int.Parse(cmd[2].Trim());

                            int y = 128;
                            if (world.IsChunkAreaLoaded(x, 0, z))
                            {
                                Chunk c = (Chunk)world.GetChunkFromWorldPos(x, 0, z);
                                y = c.GetTerrainHeight(x & 0xF, z & 0xF) + 1;
                            }

                            destination = new Vector3(x, y, z);

                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                            return false;
                        }

                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[85144b]You ugly bitch  [FF4136]I can't find Your dreamed up hero!", null, false, null));
                        return false;
                    }
                    else if (cmd[0] == "gparty")
                    {

                        World world = GameManager.Instance.World;

                        try
                        {
                            EntityPlayer leaderPlayer = world.Players.dict[clientInfo.entityId];
                            ClientInfo leaderPlayerCI = ConnectionManager.Instance.Clients.ForEntityId(leaderPlayer.entityId);

                            leaderPlayer.CreateParty();
                            leaderPlayerCI.SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(leaderPlayer.Party, clientInfo.entityId, PartyActions.AutoJoin, false));

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
                    }
                    else if (cmd[0] == "poi")
                    {
                        World world = GameManager.Instance.World;
                        Vector3 position = world.Players.dict[clientInfo.entityId].position;
                        PrefabInstance pi = GameManager.Instance.World.GetPOIAtPosition(position);

                        if (cmd.Length == 1)
                        {
                            if (pi == null)
                            {
                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF0000]No POI found!", null, false, null));
                                return false;
                            }

                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FFCC00]POI: [DDDDDD]" + pi.name, null, false, null));
                            return false;
                        }

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
                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("Failed regenerating chunk at position {0}/{1}", WorldChunkCache.extractX(key) << 4, WorldChunkCache.extractZ(key) << 4), null, false, null));
                            }
                        }

                        world.m_ChunkManager.ResendChunksToClients(hashSetLong);

                        if (pi != null)
                        {
                            pi.Reset(world);
                        }

                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[44FF44]Reseted", null, false, null));
                        return false;
                    }
                    else if (cmd[0] == "prot")
                    {
                        Vector3i l = GameManager.Instance.World.Players.dict[clientInfo.entityId].GetBlockPosition();
                        Chunk c = (Chunk)GameManager.Instance.World.GetChunkFromWorldPos(l);

                        if (cmd.Length < 2)
                        {
                            if (c.IsAnyTraderArea())
                            {
                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [01FF70]Protected", l.x, l.z), null, false, null));
                            }
                            else
                            {
                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [FF4136]Unprotected", l.x, l.z), null, false, null));

                            }

                            return false;
                        }

                        bool enable = true;
                        try
                        {
                            enable = bool.Parse(cmd[1]);
                        }
                        catch (Exception) { }

                        for (int x = 0; x < 16; x++)
                        {
                            for (int z = 0; z < 16; z++)
                            {
                                c.SetTraderArea(x, z, enable);
                            }
                        }

                        //int x = l.x & 0xF;
                        //int z = l.z & 0xF;

                        if (enable)
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [01FF70]Enabled", l.x, l.z), null, false, null));
                        else
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[F012BE]Pos [FFDC00]X:[F012BE] {0}, [FFDC00]Z:[F012BE] {1}, [FF4136]Disabled", l.x, l.z), null, false, null));

                        //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[4444FF]Chunk X: {0}, Z: {1}", c.GetWorldPos().x, c.GetWorldPos().z), null, false, null));

                        //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("[44FF44]Done X: {0}, Z: {1}, Rel: {2}, {3}", c.GetBlockWorldPosX(x), c.GetBlockWorldPosZ(z), x, z), null, false, null));
                        foreach (var cl in ConnectionManager.Instance.Clients.List)
                        {
                            cl.SendPackage(NetPackageManager.GetPackage<NetPackageChunk>().Setup(c, true));
                        }
                    }
                    else if (cmd[0] == "start")
                    {
                        World world = GameManager.Instance.World;
                        HashSet<int> playersToSplit = new HashSet<int>();
                        foreach (EntityPlayer entityPlayer in world.Players.list)
                        {
                            playersToSplit.Add(entityPlayer.entityId);
                        }

                        int count = 2;

                        try
                        {
                            count = int.Parse(cmd[1]);
                        }
                        catch (Exception) { }

                        TeamMaker.SplitPlayers(playersToSplit, count);

                        Configs.selectedMap = cmd[1].Trim();

                        Configs.Map map = Configs.Maps[Configs.selectedMap];

                        int tIndex = 0;
                        foreach (string teamName in TeamMaker.teams.Keys)
                        {
                            HashSet<int> team = TeamMaker.teams[teamName];

                            Vector3 destination = map.spawnsForTeams[tIndex++];
                            PrefabInstance pi = GameManager.Instance.World.GetPOIAtPosition(destination);

                            //foreach (int member in team)
                            //{
                            //TeamMaker.AddEnemyMarker(teamName, destination);
                            //}

                            if (cmd.Length == 1)
                            {
                                if (pi == null)
                                {
                                    clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF0000]No POI found!", null, false, null));
                                    return false;
                                }

                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FFCC00]POI: [DDDDDD]" + pi.name, null, false, null));
                                return false;
                            }

                            int num = World.toChunkXZ((int)destination.x) - 1;
                            int num2 = World.toChunkXZ((int)destination.z) - 1;
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
                                    //clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, string.Format("Failed regenerating chunk at position {0}/{1}", WorldChunkCache.extractX(key) << 4, WorldChunkCache.extractZ(key) << 4), null, false, null));
                                }
                            }

                            world.m_ChunkManager.ResendChunksToClients(hashSetLong);

                            if (pi != null)
                            {
                                pi.Reset(world);
                            }

                            foreach (int player in team)
                            {
                                ClientInfo ci = ConnectionManager.Instance.Clients.ForEntityId(player);

                                string pId = clientInfo.playerId;

                                string className = "tank";

                                if (Configs.PlayerClasses.ContainsKey(pId))
                                {
                                    className = Configs.PlayerClasses[pId];
                                }

                                Configs.GiveClassItems(ci, className);

                                ci.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(destination, null, false));
                            }
                        }
                    }
                    else if (cmd[0] == "eq")
                    {
                        World world = GameManager.Instance.World;
                        //LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(world.GetEntity(clientInfo.entityId) as EntityPlayerLocal);

                        EntityPlayer player = world.Players.dict[clientInfo.entityId];

                        GameManager.Instance.TEAccessClient(player.clientEntityId, player.serverPos, -1, player.entityId);

                        /*EntityTrader entityTrader = null;
                        
                        foreach (Entity e in GameManager.Instance.World.Entities.list)
                        {
                            if (e.GetType() == typeof(EntityTrader))
                            {
                                entityTrader = (EntityTrader)e;
                                break;
                            }
                        }

                        uiforPlayer.xui.Trader.TraderTileEntity = entityTrader.TileEntityTrader;
                        uiforPlayer.xui.Trader.Trader = entityTrader.TraderData;

                        GameManager.Instance.traderManager.TraderInventoryRequested(entityTrader.TraderData, clientInfo.entityId);

                        uiforPlayer.windowManager.CloseAllOpenWindows(null, false);
                        uiforPlayer.windowManager.Open("trader", true, false, true);

                        //TraderData trader = new TraderData();
                        //trader.TraderInfo.Init();
                        //trader.TraderInfo.UseOpenHours = false;
                        //trader.TraderInfo.AllowBuy = true;
                        //trader.TraderInfo.AllowSell = false;
                        //ItemValue itemValue = ItemClass.GetItem("gunAK47", true);
                        //ItemStack itemStack = new ItemStack(itemValue, 1);
                        //trader.AddToPrimaryInventory(itemStack, true);

                        /** Object reference not set to an instance of an object. START *
                        //GameManager.Instance.traderManager.TraderInventoryRequested(trader, clientInfo.entityId);
                        /** Object reference not set to an instance of an object. STOP *

                        /** I (as player) am at trader protected area during such operation (POI teleporter) *
                        //foreach (Entity e in GameManager.Instance.World.Entities.list)
                        //{
                        //    if (e.GetType() == typeof(EntityTrader))
                        //    {
                        //        TraderData traderData = ((EntityTrader)e).TraderData;

                        /** Nothing happens, no errors *
                        //        GameManager.Instance.traderManager.TraderInventoryRequested(traderData, clientInfo.entityId);

                        /** Have tried to found packet for such situation but this is the only one that makes any sense to me *
                        //        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageNPCQuestList>().Setup(e.entityId, clientInfo.entityId));
                        //        break;
                        //    }
                        //}

                        //GameManager.Instance.World.GetEntity()
                        //EntityTrader entityTrader = new EntityTrader();
                        //GameManager.Instance.World.SpawnEntityInWorld(entityTrader);
                        /*GameManager.Instance.World.SpawnEntityInWorld(entityTrader);
                        TraderData trader = new TraderData();
                        trader.TraderInfo.Init();
                        trader.TraderInfo.UseOpenHours = false;
                        trader.
                        ItemValue itemValue = ItemClass.GetItem("gunAK47", true);
                        ItemStack itemStack = new ItemStack(itemValue, 1);
                        trader.AddToPrimaryInventory(itemStack, true);
                        //GameManager.Instance.traderManager.TraderInventoryRequested(entityTrader.TraderData, clientInfo.entityId);

                        /*trader.AvailableMoney = 0;
                        trader.PrimaryInventory.Clear();

                        ItemValue itemValue = ItemClass.GetItem("gunAK47", true);
                        SdtdConsole.Instance.Output(string.Format("itemValue: {0}", itemValue == null));
                        Log.Out(itemValue.ToString());
                        ItemStack itemStack = new ItemStack(itemValue, 1);
                        SdtdConsole.Instance.Output(string.Format("ItemStack: {0}", itemStack == null));

                        //trader.PrimaryInventory.Add(itemStack);
                        trader.AddToPrimaryInventory(itemStack, true);*
                        //trader.TraderInfo.AllowBuy = true;
                        //trader.TraderInfo.AllowBuy = false;
                        //trader.TraderInfo.UseOpenHours = false;

                        // clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageQuestGotoPoint>().Setup(clientInfo.playerId, ));
                        //foreach (EntityPlayerLocal p in GameManager.Instance.World.GetLocalPlayers())
                        //{
                        //    Log.Out(string.Format("LID: {0} == IID: {1}", p.belongsPlayerId, clientInfo.playerId));
                        //    GameManager.ShowTooltip(p, "Tomasz Hajto przejechał babe na pasach i nie siedzi.");
                        //}
                    }
                    else if (cmd[0] == "pos")
                    {
                        if (cmd.Length < 2)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FFFF33]Ussage /pos (mapID/lobby/get) [enemy/team] [del index].", null, false, null));
                            return false;
                        }

                        Vector3i l = GameManager.Instance.World.Players.dict[clientInfo.entityId].GetBlockPosition();
                        string currentPos = string.Format("X: {0}, Y: {1}, Z: {2}", l.x, l.y, l.z);

                        if (cmd[1] == "get")
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[85144b]You stupid cunt: [FF4136]" + currentPos, null, false, null));
                        }
                        else if (cmd[1] == "lobby")
                        {
                            Configs.LobbyPosition = new Vector3(l.x, l.y, l.z);
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[DDDDDD]New lobby position: [FFCC00]" + currentPos, null, false, null));
                        }
                        else
                        {
                            string type = "team";
                            if (cmd.Length >= 3)
                            {
                                type = cmd[2];
                            }

                            type = type.ToLower().Trim();
                            string mapID = cmd[1].ToLower().Trim();

                            if (cmd.Length > 3)
                            {
                                int index = int.Parse(cmd[3].Trim());

                                if (!Configs.Maps.ContainsKey(mapID))
                                {
                                    clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[DDDDDD]Missing map: [FFCC00]" + mapID, null, false, null));
                                    return false;
                                }


                                Vector3 ll;
                                if (type == "team")
                                {
                                    ll = Configs.Maps[mapID].spawnsForTeams[index];
                                    Configs.Maps[mapID].spawnsForTeams.RemoveAt(index);
                                }
                                else
                                {
                                    ll = Configs.Maps[mapID].spawnsForTeams[index];
                                    Configs.Maps[mapID].spawnsForEnemies.RemoveAt(index);
                                }


                                currentPos = string.Format("X: {0}, Y: {1}, Z: {2}", ll.x, ll.y, ll.z);

                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[DDDDDD]Removed spawn ([FFCC00]" + type + "[DDDDDD]) for map [FFCC00]" + mapID + "[DDDDDD] with position [FFCC00]" + currentPos, null, false, null));
                                Configs.Save();
                                return false;
                            }

                            if (!Configs.Maps.ContainsKey(mapID))
                            {
                                clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[DDDDDD]New map: [FFCC00]" + mapID, null, false, null));
                                Configs.Maps.Add(mapID, new Configs.Map(mapID));
                            }

                            if (type == "team")
                            {
                                Configs.Maps[mapID].spawnsForTeams.Add(l.ToVector3());
                            }
                            else
                            {
                                Configs.Maps[mapID].spawnsForEnemies.Add(l.ToVector3());
                            }

                            Configs.Save();
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[DDDDDD]Added spawn ([FFCC00]" + type + "[DDDDDD]) for map [FFCC00]" + mapID + "[DDDDDD] with position [FFCC00]" + currentPos, null, false, null));
                        }
                    }
                    else if (cmd[0] == "give")
                    {
                        if (cmd.Length < 2)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF3333] To few args.", null, false, null));
                            return false;
                        }

                        ItemValue itemValue = new ItemValue(ItemClass.GetItem(cmd[1].Trim().ToLower(), true).type, 6, 6, true, null, 1);
                        World world = GameManager.Instance.World;
                        var entityItem = (EntityItem)EntityFactory.CreateEntity(new EntityCreationData
                        {
                            entityClass = EntityClass.FromString("item"),
                            id = EntityFactory.nextEntityID++,
                            itemStack = new ItemStack(itemValue, 1),
                            pos = world.Players.dict[clientInfo.entityId].position,
                            rot = new Vector3(20f, 0f, 20f),
                            lifetime = 60f,
                            belongsPlayerId = clientInfo.entityId
                        });

                        if (entityItem == null)
                        {
                            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF3333] You are wrong :c", null, false, null));
                            return false;
                        }

                        world.SpawnEntityInWorld(entityItem);
                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityItem.entityId, clientInfo.entityId));
                        world.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Despawned);
                    }
                    else
                    {
                        clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(EChatType.Whisper, _senderId, "[FF3333] Command unknown.", null, false, null));
                    }
                    return false;*/
                }
            }
            // Returning true allows other listeners to process this message.
            return true;
        }

    }
}
