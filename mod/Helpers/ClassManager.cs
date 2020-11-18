using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace mod.Helpers
{
    class ClassManager
    {
        public static List<string> ValidClasses = new List<string>() { "tank", "soldier", "sniper", "medic" };
        public static Dictionary<string, List<EntityItem>> ItemsForClasses = new Dictionary<string, List<EntityItem>>();

        internal static string GetRandomColor()
        {
            List<string> colors = new List<string>() {
                "White",
                "Red",
                "Green",
                "Black",
                "Blue",
                "Brown",
                "Pink",
                "Purple",
                "Yellow",
            };

            return colors.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        }

        internal static EntityItem MakeNormalItem(string id, ClientInfo player, Vector3 pos, int quantity = 1)
        {
            ItemValue itemValue = new ItemValue(ItemClass.GetItem(id, true).type, 6, 6);
            return MakeEntity(itemValue, player.entityId, pos, quantity);
        }

        internal static EntityItem MakeGunItem(string id, ClientInfo player, Vector3 pos, List<string> mods, int preloadedAmmoCount, byte ammoType = 1, string color = "none", int useTimes = -999999)
        {
            ItemValue itemValue = new ItemValue(ItemClass.GetItem(id, true).type, 6, 6, true, mods.ToArray())
            {
                Meta = preloadedAmmoCount,
                SelectedAmmoTypeIndex = ammoType,
                UseTimes = useTimes
            };

            if (color != "none")
            {
                string itemId = string.Format("modDye{0}", color);
                ItemValue colorValue = new ItemValue(ItemClass.GetItem(itemId, true).type, 6, 6);
                itemValue.CosmeticMods[0] = colorValue;
            }

            return MakeEntity(itemValue, player.entityId, pos, 1);
        }

        internal static EntityItem MakeWpnItem(string id, ClientInfo player, Vector3 pos, List<string> mods, string color = "none", int useTimes = -999999)
        {
            ItemValue itemValue = new ItemValue(ItemClass.GetItem(id, true).type, 6, 6, true, mods.ToArray())
            {
                UseTimes = useTimes
            };

            if (color != "none")
            {
                string itemId = string.Format("modDye{0}", color);
                ItemValue colorValue = new ItemValue(ItemClass.GetItem(itemId, true).type, 6, 6);
                itemValue.CosmeticMods[0] = colorValue;
            }

            return MakeEntity(itemValue, player.entityId, pos, 1);
        }

        internal static EntityItem MakeColoredItem(string id, ClientInfo player, Vector3 pos, string color = "none", int useTimes = -999999)
        {
            ItemValue itemValue = new ItemValue(ItemClass.GetItem(id, true).type, 6, 6)
            {
                UseTimes = useTimes
            };

            if (color != "none")
            {
                string itemId = string.Format("modDye{0}", color);
                ItemValue colorValue = new ItemValue(ItemClass.GetItem(itemId, true).type, 6, 6);
                itemValue.CosmeticMods[0] = colorValue;
            }

            return MakeEntity(itemValue, player.entityId, pos, 1);
        }

        internal static EntityItem MakeArmourItem(string id, ClientInfo player, Vector3 pos, List<string> mods, string color = "none", int useTimes = -999999)
        {
            ItemValue itemValue = new ItemValue(ItemClass.GetItem(id, true).type, 6, 6, true, mods.ToArray())
            {
                UseTimes = useTimes
            };

            if (color != "none")
            {
                string itemId = string.Format("modDye{0}", color);
                ItemValue colorValue = new ItemValue(ItemClass.GetItem(itemId, true).type, 6, 6);
                itemValue.CosmeticMods[0] = colorValue;
            }

            return MakeEntity(itemValue, player.entityId, pos, 1);
        }

        internal static EntityItem MakeEntity(ItemValue itemValue, int belongsPlayerId, Vector3 pos, int quantity)
        {
            return (EntityItem) EntityFactory.CreateEntity(new EntityCreationData
            {
                entityClass = EntityClass.FromString("item"),
                id = EntityFactory.nextEntityID++,
                itemStack = new ItemStack(itemValue, quantity),
                pos = pos,
                rot = new Vector3(20f, 0f, 20f),
                lifetime = 60f,
                belongsPlayerId = belongsPlayerId
            });
        }

        public static void ApplyClass(ClientInfo p)
        {
            string pId = p.playerId;
            string className = VariableContainer.GetPlayerClass(pId);

            // Set Buffs
            p.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("buff " + className, true));

            // Disable spawnscreen
            p.SendPackage(NetPackageManager.GetPackage<NetPackageConsoleCmdClient>().Setup("SpawnScreen off", true));

            // Give items
            World world = GameManager.Instance.World;
            Vector3 p2 = world.Players.dict[p.entityId].position;

            List<EntityItem> items;
            switch (className)
            {
                case "medic":
                    List<EntityItem> itemsMedic = new List<EntityItem>
                    {
                        MakeColoredItem("apparelHazmatMask", p, p2, "Black"),
                        MakeColoredItem("apparelHazmatGloves", p, p2, "Black"),
                        MakeColoredItem("apparelHazmatJacket", p, p2, "Black"),
                        MakeColoredItem("apparelHazmatPants", p, p2, "Black"),
                        MakeColoredItem("apparelHazmatBoots", p, p2, "Black"),

                        MakeArmourItem("armorIronChest", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings", "modArmorBandolier" }, "Black"),
                        MakeArmourItem("armorIronLegs", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings", "modArmorBandolier" }, "Black"),

                        MakeGunItem("gunShotgunT2PumpShotgun", p, p2, new List<string>() { "modShotgunSawedOffBarrel", "modGunShotgunTubeExtenderMagazine", "modGunFlashlight" }, 7, 2, "Black"),
                        MakeGunItem("gunBotT2JunkTurret", p, p2, new List<string>() { "modGunBarrelExtender", "modGunCrippleEm", "modGunMagazineExtender" }, 10, 2, "Black"),

                        MakeWpnItem("meleeWpnBatonT2StunBaton", p, p2, new List<string>() { "modMeleeStunBatonRepulsor", "modMeleeErgonomicGrip", "modMeleeWeightedHead" }, "Black"),

                        MakeNormalItem("ammoShotgunShell", p, p2, 300),
                        MakeNormalItem("ammoShotgunSlug", p, p2, 150),
                        MakeNormalItem("ammoShotgunBreachingSlug", p, p2, 150),
                        //MakeNormalItem("ammoJunkTurretAP", p, p2, 10),

                        MakeNormalItem("medicalFirstAidKit", p, p2, 10),
                        MakeNormalItem("trapSpikesScrapIronMaster", p, p2, 20),
                    };
                    items = itemsMedic;
                    break;

                case "soldier":
                    List<EntityItem> itemsSoldier = new List<EntityItem>
                    {
                        MakeArmourItem("armorMilitaryHelmet", p, p2, new List<string>() { "modArmorPlatingBasic", "modArmorImprovedFittings" }, "Black"),
                        MakeArmourItem("armorMilitaryVest", p, p2, new List<string>() { "modArmorPlatingBasic", "modArmorImprovedFittings", "modArmorBandolier" }, "Black"),
                        MakeArmourItem("armorMilitaryGloves", p, p2, new List<string>() { "modArmorPlatingBasic", "modArmorImprovedFittings" }, "Black"),
                        MakeArmourItem("armorMilitaryLegs", p, p2, new List<string>() { "modArmorPlatingBasic", "modArmorImprovedFittings", "modArmorBandolier" }, "Black"),
                        MakeArmourItem("armorMilitaryBoots", p, p2, new List<string>() { "modArmorPlatingBasic", "modArmorImprovedFittings" }, "Black"),

                        MakeGunItem("gunMGT1AK47", p, p2, new List<string>() { "modGunReflexSight", "modGunForegrip", "modGunMuzzleBrake", "modGunMagazineExtender" }, 40, 2, "Black"),
                        MakeGunItem("gunHandgunT2Magnum44", p, p2, new List<string>() { "modGunLaserSight", "modGunBarrelExtender", "modGunCrippleEm" }, 8, 2, "Black"),
                        MakeWpnItem("meleeWpnBladeT1HuntingKnife", p, p2, new List<string>() { "modMeleeSerratedBlade", "modMeleeFortifyingGrip", "modGunMeleeTheHunter" }, "Black"),

                        MakeNormalItem("ammo762mmBulletBall", p, p2, 300),
                        MakeNormalItem("ammo762mmBulletHP", p, p2, 150),
                        MakeNormalItem("ammo762mmBulletAP", p, p2, 150),
                        MakeNormalItem("ammo44MagnumBulletBall", p, p2, 150),

                        MakeNormalItem("thrownAmmoMolotovCocktail", p, p2, 3),
                        MakeNormalItem("thrownGrenade", p, p2, 5),
                    };
                    items = itemsSoldier;
                    break;

                case "sniper":
                    List<EntityItem> itemsSniper = new List<EntityItem>
                    {
                        MakeColoredItem("apparelGhillieSuitHood", p, p2, "Green"),
                        MakeNormalItem("apparelNightvisionGoggles", p, p2),
                        MakeColoredItem("apparelGhillieSuitJacket", p, p2, "Green"),
                        MakeColoredItem("apparelGhillieSuitPants", p, p2, "Green"),

                        MakeArmourItem("armorMilitaryGloves", p, p2, new List<string>() { "modArmorImprovedFittings", "modArmorAdvancedMuffledConnectors" }, "Green"),
                        MakeArmourItem("armorMilitaryStealthBoots", p, p2, new List<string>() { "modArmorImprovedFittings", "modArmorAdvancedMuffledConnectors", "modArmorImpactBracing" }, "Green"),

                        MakeGunItem("gunRifleT3SniperRifle", p, p2, new List<string>() { "modGunScopeLarge", "modGunBipod", "modGunSoundSuppressorSilencer", "modGunMeleeTheHunter" }, 30, 2, "Black"),
                        MakeGunItem("gunHandgunT1Pistol", p, p2, new List<string>() { "modGunSoundSuppressorSilencer", "modGunLaserSight" }, 30, 2, "Black"),
                        MakeWpnItem("meleeWpnBladeT3Machete", p, p2, new List<string>() { "modMeleeSerratedBlade", "modMeleeFortifyingGrip", "modGunMeleeTheHunter" }, "Black"),

                        MakeNormalItem("ammo762mmBulletBall", p, p2, 300),
                        MakeNormalItem("ammo762mmBulletHP", p, p2, 150),
                        MakeNormalItem("ammo762mmBulletAP", p, p2, 150),
                        MakeNormalItem("ammo9mmBulletBall", p, p2, 150),

                        MakeNormalItem("thrownGrenadeContact", p, p2, 5),
                        MakeNormalItem("mineHubcap", p, p2, 3),
                    };
                    items = itemsSniper;
                    break;

                case "tank":
                default:
                    List<EntityItem> itemsTank = new List<EntityItem>
                    {
                        MakeArmourItem("armorSteelHelmet", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings", "modArmorHelmetLight" }, "Black"),
                        MakeArmourItem("armorSteelChest", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings", "modArmorBandolier" }, "Black"),
                        MakeArmourItem("armorSteelGloves", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings", "modArmorBandolier" }, "Black"),
                        MakeArmourItem("armorSteelLegs", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings", "modArmorBandolier" }, "Black"),
                        MakeArmourItem("armorSteelBoots", p, p2, new List<string>() { "modArmorPlatingReinforced", "modArmorCustomizedFittings" }, "Black"),

                        MakeGunItem("gunMGT3M60", p, p2, new List<string>() { "modGunBarrelExtender", "modGunBipod", "modGunDrumMagazineExtender" }, 60, 2, "Black"),
                        MakeGunItem("gunHandgunT1Pistol", p, p2, new List<string>() { "modGunBarrelExtender", "modGunLaserSight", "modGunDrumMagazineExtender" }, 15, 2, "Black"),

                        MakeWpnItem("meleeWpnSledgeT3SteelSledgehammer", p, p2, new List<string>() { "modMeleeWeightedHead", "modMeleeErgonomicGrip", "modMeleeClubBurningShaft" }, "Black"),

                        MakeNormalItem("ammo762mmBulletBall", p, p2, 300),
                        MakeNormalItem("ammo762mmBulletHP", p, p2, 150),
                        MakeNormalItem("ammo762mmBulletAP", p, p2, 150),
                        MakeNormalItem("ammo9mmBulletBall", p, p2, 150),
                        MakeNormalItem("thrownDynamite", p, p2, 2),
                        MakeNormalItem("thrownTimedCharge", p, p2, 3),
                    };
                    items = itemsTank;
                    break;
            }

            foreach (EntityItem item in items)
            {
                if (item == null) continue;
                GiveEntityToPLayer(item, p);
            }
        }

        internal static async void GiveEntityToPLayer(EntityItem entity, ClientInfo player)
        {
            await Task.Run(() => {
                World world = GameManager.Instance.World;
                world.SpawnEntityInWorld(entity);
                player.SendPackage(NetPackageManager.GetPackage<NetPackageEntityCollect>().Setup(entity.entityId, player.entityId));
                world.RemoveEntity(entity.entityId, EnumRemoveEntityReason.Despawned);
            });
        }
    }
}
