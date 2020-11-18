using mod.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using static mod.Helpers.Map;

namespace mod
{
    static class Configs
    {
        private static readonly string ConfigFilePath = string.Format("{0}/Mods/Mod/config.xml", Directory.GetCurrentDirectory());
        private static readonly string ConfigPath = string.Format("{0}/Mods/Mod", Directory.GetCurrentDirectory());

        public static void Load()
        {
            PatchTools.ApplyPatches();

            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
                Log.Out(string.Format("[MOD] Created directory {0}", ConfigPath));
            }

            if (!Utils.FileExists(ConfigFilePath))
            {
                WriteXml();
                Log.Out(string.Format("[MOD] Created config.xml {0}", ConfigFilePath));
            }

            Log.Out("[MOD] Config load initialized");
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.Load(ConfigFilePath);

                XmlNode s = xmlDoc.GetElementsByTagName("LobbyPosition")[0];

                Vector3 v = Vector3.zero;

                v.x = float.Parse(s.ChildNodes[0].InnerText);
                v.y = float.Parse(s.ChildNodes[1].InnerText);
                v.z = float.Parse(s.ChildNodes[2].InnerText);

                VariableContainer.SetLobbyPosition(v, false);
                Log.Out("[MOD] Loaded Lobby position: " + v.ToString());

                XmlNode maps = xmlDoc.GetElementsByTagName("Maps")[0];
                foreach (XmlNode mapNode in maps.ChildNodes)
                {
                    Map map = new Map
                    {
                        name = mapNode.ChildNodes[0].InnerText
                    };

                    foreach (XmlNode spawn in mapNode.ChildNodes[1])
                    {
                        Vector3 l = Vector3.zero;
                        l.x = float.Parse(spawn.ChildNodes[1].InnerText);
                        l.y = float.Parse(spawn.ChildNodes[2].InnerText);
                        l.z = float.Parse(spawn.ChildNodes[3].InnerText);

                        string spawnFor = spawn.ChildNodes[0].InnerText.Trim().ToLower();

                        map.AddSpawn(spawnFor, l);
                    }

                    VariableContainer.AddMap(map);
                    Log.Out("[MOD] Loaded Map: " + map.name);
                }
            }
            catch (Exception e)
            {
                Log.Error(string.Format("[MOD] Failed loading {0}: {1}", ConfigFilePath, e.Message));
                return;
            }

            Log.Out("[MOD] Config load complete");
        }

        public static void Save()
        {
            WriteXml();
        }


        static void WriteXml()
        {
            using (StreamWriter sw = new StreamWriter(ConfigFilePath))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<Mod>");
                sw.WriteLine("  <LobbyPosition>");

                Vector3 v = VariableContainer.GetLobbyPosition();

                sw.WriteLine(string.Format("    <X>{0}</X>", v.x));
                sw.WriteLine(string.Format("    <Y>{0}</Y>", v.y));
                sw.WriteLine(string.Format("    <Z>{0}</Z>", v.z));

                sw.WriteLine("  </LobbyPosition>");

                sw.WriteLine("  <Maps>");

                foreach (Map map in VariableContainer.ListMaps())
                {
                    sw.WriteLine("    <Map>");
                    sw.WriteLine(string.Format("      <Name>{0}</Name>", map.name));
                    sw.WriteLine("      <Spawns>");

                    foreach (KeyValuePair<string, List<Spawn>> pair in map.spawns.ToList())
                    {
                        foreach (Spawn spawn in pair.Value)
                        {
                            sw.WriteLine("        <Spawn>");

                            sw.WriteLine(string.Format("          <Type>{0}</Type>", pair.Key));
                            sw.WriteLine(string.Format("          <X>{0}</X>", spawn.location.x));
                            sw.WriteLine(string.Format("          <Y>{0}</Y>", spawn.location.y));
                            sw.WriteLine(string.Format("          <Z>{0}</Z>", spawn.location.z));
                            sw.WriteLine("        </Spawn>");
                        }
                    }

                    sw.WriteLine("      </Spawns>");
                    sw.WriteLine("    </Map>");
                }

                sw.WriteLine("  </Maps>");

                sw.WriteLine("</Mod>");
                sw.Flush();
                sw.Close();
            }
        }
    }
}
