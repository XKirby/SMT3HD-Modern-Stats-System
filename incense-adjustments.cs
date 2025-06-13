using Il2Cpp;
using Il2Cppnewdata_H;
using MelonLoader;
using HarmonyLib;
using Il2Cppfacility_H;

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {
        [HarmonyPatch(typeof(cmpMisc), nameof(cmpMisc.cmpUseItemKou))]
        private class PatchIncense
        {
            private static bool Prefix(ushort ItemID, datUnitWork_t pStock)
            {
                // Checks the currently used item's ID and make sure it's the Stat Incense items.
                if (ItemID > 0x25 && ItemID < 0x2c)
                {
                    // Set the Stat ID relative to the current Incense.
                    int statID = ItemID - 0x26;

                    // Increases the target's stat if it isn't above the maximum, then recalculates HP/MP and heals them.
                    if (rstCalcCore.cmbGetParamBase(ref pStock, statID) < MAXSTATS)
                    {
                        pStock.param[statID]++;
                        rstcalc.rstSetMaxHpMp(0, ref pStock);
                        pStock.hp = pStock.maxhp;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(datItemName), nameof(datItemName.Get))]
        private class PatchIntIncenseName
        {
            private static void Postfix(ref int id, ref string __result)
            {
                // If this is the Int Incense, write its name properly.
                if (id == 0x27)
                    { __result = "Intelligence Incense"; }
            }
        }

        [HarmonyPatch(typeof(datItemHelp_msg), nameof(datItemHelp_msg.Get))]
        private class PatchIntIncenseHelp
        {
            private static void Postfix(ref int id, ref string __result)
            {
                // If this is the Int Incense, write its help message properly.
                if (id == 0x27)
                { __result = "Raises Intelligence by 1 and full HP recovery for one ally."; }
            }
        }

        [HarmonyPatch(typeof(fclShopCalc), nameof(fclShopCalc.shpCreateItemList))]
        private class PatchLastShopIncenseStorage
        {
            private static void Postfix(ref fclDataShop_t pData)
            {
                // If the current shop is the final shop in the game.
                if(pData.Place == 6)
                {
                    // Loop through the Incense items and add them to the shop.
                    for (int i = 0; i < 6; i++)
                    {
                        if (!EnableIntStat && i == 0)
                            { continue; }
                        pData.BuyItemList[pData.BuyItemCnt] = (byte)(0x26 + i);
                        pData.BuyItemCnt++;
                    }
                }
            }
        }
    }
}
