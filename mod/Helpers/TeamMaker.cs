using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static NetPackagePartyData;

namespace mod
{
    internal class TeamMaker
    {
        internal static Dictionary<string, Team> Teams { get; } = new Dictionary<string, Team>();
        internal class Team
        {
            internal Vector3 spawn;
            internal string id;
            internal class Member
            {
                internal int entityId = -1;
                internal string nick;
                internal string pId;

                internal ClientInfo ClientInfo()
                {
                    return ConnectionManager.Instance.Clients.ForEntityId(entityId);
                }

                internal EntityPlayer EntityPlayer()
                {
                    return GameManager.Instance.World.Players.dict[entityId];
                }
            }

            internal readonly List<Member> members = new List<Member>();
            internal Member leader;

            internal string name = "A team of faggots";

            internal void AddMember(Member player)
            {
                if (members.Contains(player)) return;
                members.Add(player);
            }

            internal void RemoveMember(Member player)
            {
                foreach (Member member in members)
                {
                    if (member.pId == player.pId)
                    {
                        members.Remove(member);
                        return;
                    }
                }
            }

            internal Team(string id)
            {
                this.id = id;
                leader = new Member();
            }

            internal string GetMembers()
            {
                StringBuilder sb = new StringBuilder();

                foreach (Member member in members)
                {
                    sb.Append(string.Format("[FFDC00]{0}[DDDDDD], ", member.nick));
                }

                string ret = sb.ToString();

                return ret.Remove(ret.Length - 2, 2);
            }
        }



        internal static Team GetPlayerTeam(string pId)
        {
            foreach (Team team in Teams.Values)
            {
                foreach (Team.Member member in team.members)
                {

                    if (member.pId == pId)
                    {
                        Log.Out(string.Format("Team for {0} is {1}", member.nick, team.id));

                        return team;
                    }
                }
            }

            return null;
        }

        internal static bool AddTeam(string id, List<ClientInfo> playersToAdd = null)
        {
            if (Teams.ContainsKey(id))
            {
                return false;
            }

            Teams.Add(id, new Team(id));

            if (playersToAdd == null) return true;

            foreach (ClientInfo player in playersToAdd)
            {
                Team.Member member = new Team.Member
                {
                    entityId = player.entityId,
                    nick = player.playerName,
                    pId = player.playerId
                };

                AddPlayerToTeam(member, id);
            }

            return true;
        }

        /*intrnal static void MovePlayerToTeam(ClientInfo player, string team)
        {
            bool b1 = false, b2 = false;
            foreach (KeyValuePair<string, HashSet<int>> t in Teams)
            {
                if (t.Key == team)
                {
                    b1 = AddPlayerToTeam(player, team);
                } else
                {
                    b2 = RemovePlayerFromTeam(player, team);
                }

                if (b1 && b2) break;
            }
        }*/

        internal static bool AddPlayerToTeam(Team.Member player, string id)
        {
            try { 
                if (Teams[id] == null) return false;

                World world = GameManager.Instance.World;
                Team team = Teams[id];

                EntityPlayer leaderEntity;
                // Means that this player will be leader
                if (team.leader.entityId == -1)
                {
                    Log.Out("[MOD] Making part from " + player.nick);

                    leaderEntity = world.Players.dict[player.entityId];
                    // Must leave previous party, otherwise bugs will occur
                    if (leaderEntity.IsInParty())
                        leaderEntity.LeaveParty();
                    leaderEntity.CreateParty();

                    player.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(leaderEntity.Party, player.entityId, PartyActions.AutoJoin, false));
                    Teams[id].AddMember(player);
                    Teams[id].leader = player;
                    return true;
                }

                // We need it to access party
                leaderEntity = team.leader.EntityPlayer();
                
                // Must leave previous party, otherwise bugs will occur
                if (player.EntityPlayer().IsInParty())
                {
                    player.EntityPlayer().Party.RemovePlayer(player.EntityPlayer());
                    player.EntityPlayer().LeaveParty();

                    Log.Out("[MOD] Leaving party " + player.nick);

                    player.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(player.EntityPlayer().Party, player.entityId, NetPackagePartyData.PartyActions.LeaveParty, true));
                }

                Teams[id].AddMember(player);
                leaderEntity.Party.AddPlayer(player.EntityPlayer());

                foreach (Team.Member member in Teams[id].members)
                {
                    Log.Out("[MOD] Resend from "+ Teams[id].leader.nick + " to " + member.nick);

                    member.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(leaderEntity.Party, member.entityId, PartyActions.AutoJoin, false));
                }

                return true;

            } catch (Exception e)
            {
                Log.Error(string.Format("Error in AddPlayerToTeam: {0}", e.Message));
                Log.Error(e.StackTrace);
                Log.Exception(e);
                return false;
            }
        }

        internal static void CleanUp()
        {
            foreach (string id in Teams.Keys)
            {
                foreach (Team.Member member in Teams[id].members)
                {
                    if (member.EntityPlayer().IsInParty())
                    {
                        member.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(member.EntityPlayer().Party, member.entityId, NetPackagePartyData.PartyActions.KickFromParty, true));
                        member.EntityPlayer().LeaveParty();
                    }
                }
            }

            Teams.Clear();
        }

        /*internal static bool RemovePlayerFromTeam(Team.Member player)
        {
            foreach (string id in Teams.Keys)
            {
                RemovePlayerFromTeam(player, id);
            }
            return true;
        }*/

        internal static void RemovePlayerAfterDisconnect(string pId, string teamId)
        {
            if (Teams[teamId] == null) return;

            int entityId = 0;
            Team.Member toRemove = null;

            foreach (Team.Member member in Teams[teamId].members)
            {
                if (member.pId == pId)
                {
                    entityId = member.entityId;
                    toRemove = member;
                    break;
                }
            }

            if (toRemove == null) return;

            Teams[teamId].RemoveMember(toRemove);

            foreach (Team.Member member in Teams[teamId].members)
            {
                member.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(member.EntityPlayer().Party, entityId, NetPackagePartyData.PartyActions.Disconnected, member.EntityPlayer().Party.MemberList.Count == 0));
            }
        }

        /*internal static bool RemovePlayerFromTeam(Team.Member player, string id)
        {
            if (Teams[id] == null) return false;

            try
            {
                if (player.EntityPlayer() != null)
                {
                    if (player.EntityPlayer().IsInParty())
                    {
                        player.EntityPlayer().Party.RemovePlayer(player.EntityPlayer());
                        player.EntityPlayer().LeaveParty();

                        Party party = player.EntityPlayer().Party;
                        player.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(party, player.entityId, NetPackagePartyData.PartyActions.LeaveParty, party.MemberList.Count == 0));
                    }
                }

                foreach (Team.Member member in Teams[id].members)
                {
                    member.ClientInfo().SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>().Setup(member.EntityPlayer().Party, player.entityId, NetPackagePartyData.PartyActions.LeaveParty, member.EntityPlayer().Party.MemberList.Count == 0));
                }

                Teams[id].RemoveMember(player);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(string.Format("Error in RemovePlayerFromTeam: {0}", e.Message));
                Log.Error(e.StackTrace);
                Log.Exception(e);
                return false;
            }
        }*/

        internal static void SplitPlayers(List<ClientInfo> list, int teamsCount = 2)
        {
            // Shuffle list
            list = list.OrderBy(a => Guid.NewGuid()).ToList();

            try
            {
                Teams.Clear();

                int i = 0;
                IEnumerable<IGrouping<int, ClientInfo>> groupsOfPlayers = list.GroupBy(p => i++ % teamsCount);

                foreach (IGrouping<int, ClientInfo> group in groupsOfPlayers)
                {
                    AddTeam(string.Format("Team #{0}", group.Key), new List<ClientInfo>(group.ToList()));
                    Log.Out(string.Format("Added team {0}", string.Format("Team #{0}", group.Key)));
                }
            }
            catch (Exception e)
            {
                Log.Error(string.Format("Error in SplitPlayers: {0}", e.Message));
                Log.Error(e.StackTrace);
                Log.Exception(e);
            } 
        }

        /*public static void AddEnemyMarker(string teamName, Vector3 pos)
        {
            if (!Teams.ContainsKey(teamName)) return;

            foreach (int member in Teams[teamName])
            {
                EntityPlayer memberEntity = GameManager.Instance.World.Players.dict[member];
                ClientInfo memberClientInfo = ConnectionManager.Instance.Clients.ForEntityId(member);

                memberClientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageQuestGotoPoint>().Setup(
                    memberEntity.belongsPlayerId,
                    QuestTags.none,
                    0,
                    NetPackageQuestGotoPoint.QuestGotoTypes.Closest,
                    1,
                    (int) pos.x,
                    (int) pos.z
                ));
                //memberClientInfo.SendPackage(NetPackageManager.GetPackage<NetPackagePart>().Setup(member, new Vector2i((int)pos.x, (int)pos.z)));
            }
        }*/
    }
}
