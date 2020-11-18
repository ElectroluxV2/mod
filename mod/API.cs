
using mod.Helpers;

namespace mod
{
    public class API : IModApi
    {
        public void InitMod()
        {
            Configs.Load();

            ModEvents.ChatMessage.RegisterHandler(Events.ChatMessage);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(Events.PlayerSpawnedInWorld);
            ModEvents.PlayerDisconnected.RegisterHandler(Events.PlayerDisconnected);
        }
    }
}
