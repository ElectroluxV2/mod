using mod.Helpers;
using System.Collections.Generic;

namespace mod.Commands
{
    class SelectClass
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            if (arguments.Count < 1)
            {
                ChatManager.Message(sender, "[FF3333]Tank [33FFFF]/ [FF3333]Soldier [33FFFF]/ [FF3333]Sniper [33FFFF]/ [FF3333]Medic ");
                return false;
            }

            string className = arguments[0].Trim();

            if (!ClassManager.ValidClasses.Contains(className))
            {
                ChatManager.Message(sender, "[FF3333]Tank [33FFFF]/ [FF3333]Soldier [33FFFF]/ [FF3333]Sniper [33FFFF]/ [FF3333]Medic ");
                return false;
            }

            VariableContainer.SetPlayerClass(sender.playerId, className);
            ChatManager.Message(sender, string.Format("[FF3333]Selected: [33FFFF]{0} ", className));
            return false;
        }
    }
}
