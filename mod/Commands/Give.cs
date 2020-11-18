using mod.Helpers;
using System.Collections.Generic;
using UnityEngine;
using XMLData;

namespace mod.Commands
{
    class Give
    {
        public static bool Execute(ClientInfo sender, List<string> arguments)
        {
            /// give gunMGT1AK47 modGunScopeSmall
            World world = GameManager.Instance.World;
            EntityPlayer entityPlayer = world.Players.dict[sender.entityId];

            if (arguments.Count < 1)
            {
                ChatManager.Message(sender, "[DDDDDD]Usage: [FFDC00]/give itemId [...mods]");
                return false;
            }


            if (arguments[0] == "ak")
            {
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("buff buffShocked", true));
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("exhausted 100", true));
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("food 100", true));
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("water 100", true));
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("water 100", true));

                List<string> m = new List<string>() {
                    "modGunLaserSight",
                    "modGunFlashlight",
                    "modGunScopeLarge",
                    "modGunTriggerGroupAutomatic",
                    "modGunBipod",
                    "modGunMagazineExtender",
                };



                ItemValue AKitem = new ItemValue(ItemClass.GetItem("gunMGT1AK47", true).type, 6, 6, true, m.ToArray(), 1)
                {
                    SelectedAmmoTypeIndex = 1,
                    Meta = 3000
                };

                AKitem.CosmeticMods[0] = new ItemValue(ItemClass.GetItem("modDyeRed", true).type, 6, 6, true, m.ToArray(), 1);
                AKitem.UseTimes = -99999;

                var AKentityItem = (EntityItem)EntityFactory.CreateEntity(new EntityCreationData
                {
                    entityClass = EntityClass.FromString("item"),
                    id = EntityFactory.nextEntityID++,
                    itemStack = new ItemStack(AKitem, 1),
                    pos = world.Players.dict[sender.entityId].position,
                    rot = new Vector3(20f, 0f, 20f),
                    lifetime = 60f,
                    belongsPlayerId = sender.entityId
                });

                world.SpawnEntityInWorld(AKentityItem);
                sender.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(AKentityItem.entityId, sender.entityId));
                world.RemoveEntity(AKentityItem.entityId, EnumRemoveEntityReason.Despawned);

                return false;
            }

            List<string> mods = new List<string>();

            if (arguments.Count > 1)
            {
                arguments.ForEach(arg => {
                    if (arg == arguments[0]) return;
                    mods.Add(arg.Trim());
                });
            }

            ItemValue itemValue = new ItemValue(ItemClass.GetItem(arguments[0].Trim(), true).type, 6, 6, true, mods.ToArray(), 1)
            {
            };

            var entityItem = (EntityItem) EntityFactory.CreateEntity(new EntityCreationData
            {
                entityClass = EntityClass.FromString("item"),
                id = EntityFactory.nextEntityID++,
                itemStack = new ItemStack(itemValue, 1),
                pos = world.Players.dict[sender.entityId].position,
                rot = new Vector3(20f, 0f, 20f),
                lifetime = 60f,
                belongsPlayerId = sender.entityId
            });

            if (entityItem == null)
            {
                ChatManager.Message(sender, "[DDDDDD]You are [FFDC00]wrong");
                return false;
            }

            world.SpawnEntityInWorld(entityItem);
            sender.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entityItem.entityId, sender.entityId));
            world.RemoveEntity(entityItem.entityId, EnumRemoveEntityReason.Despawned);


            //GameManager.Instance.ItemReloadServer(sender.entityId);
            return false;
        }
    }
}
