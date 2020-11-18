
using mod.Helpers;
using System.Collections.Generic;

namespace mod.Commands
{
    class ChatCommands
    {
        public static bool Handle(string rawMessage, ClientInfo clientInfo)
        {
            // Clear and split message into parts, later arguments
            rawMessage = rawMessage.Trim().ToLower().Remove(0, 1);

            List<string> arguments = new List<string>(rawMessage.Split(' '));

            string command = arguments[0];

            arguments.RemoveAt(0);

            switch (command)
            {
                case "start":
                    return Commands.Start.Execute(clientInfo, arguments);
                case "class":
                    return Commands.SelectClass.Execute(clientInfo, arguments);
                case "teleport":
                case "tp":
                    return Commands.TeleportAssist.Execute(clientInfo, arguments);
                case "gparty":
                    return Commands.GParty.Execute(clientInfo, arguments);
                case "terrainfix":
                case "poi":
                case "tf":
                    return Commands.TerrainFix.Execute(clientInfo, arguments);
                case "prot":
                case "protection":
                    return Commands.Protection.Execute(clientInfo, arguments);
                case "give":
                    return Commands.Give.Execute(clientInfo, arguments);
                case "pos":
                case "position":
                    return Commands.Position.Execute(clientInfo, arguments);
                default:
                    return UnknownCommand(clientInfo);
            }
        }

        private static bool UnknownCommand(ClientInfo clientInfo)
        {
            ChatManager.Message(clientInfo, "[FF4136]Unknown command");
            return false;
        }
    }
}
