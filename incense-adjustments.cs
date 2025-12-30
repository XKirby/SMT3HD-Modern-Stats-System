using Il2Cpp;
using Il2Cppnewdata_H;
using MelonLoader;
using HarmonyLib;
using Il2Cppfacility_H;

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {

        [HarmonyPatch(typeof(ModernStatsSystem), nameof(ModernStatsSystem.OnInitializeMelon))]
        private class PatchOnInitializeIncense
        {
            private static void Postfix()
            {
                // Set the Intelligence Incense to be usable.
                datItem.tbl[0x27].use = 1;

                // Change the Incense Buy Prices.
                datItem.tbl[0x26].price = 7500u;
                datItem.tbl[0x27].price = 7500u;
                datItem.tbl[0x28].price = 7500u;
                datItem.tbl[0x29].price = 7500u;
                datItem.tbl[0x2a].price = 7500u;
                datItem.tbl[0x2b].price = 7500u;

                // If Enabled, add the Int Incense to the Lucky Ticket Prizes.
                if (EnableIntStat)
                {
                    // Lucky Ticket Item Box Prizes
                    for (int i = 0; i < 7; i++)
                    {
                        // Change the Incense Items and add the Int Incense.
                        if (i > 1)
                        {
                            fclJunkShopTable.fclShopItemBoxTbl[1][i].ItemID = (byte)(0x26 + i - 1);
                            fclJunkShopTable.fclShopItemBoxTbl[1][i].Rate = 10;
                        }
                        // Adjusting the Balm's drop rate because I believe it's necessary.
                        else
                            { fclJunkShopTable.fclShopItemBoxTbl[1][i].Rate = 40; }
                    }
                }
            }
        }
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
                    if (datCalc.datGetBaseParam(pStock, statID) < MAXSTATS)
                    {
                        pStock.param[statID]++;
                        pStock.maxhp = (ushort)datCalc.datGetMaxHp(pStock);
                        pStock.maxmp = (ushort)datCalc.datGetMaxMp(pStock);
                        if (pStock.hp > 0)
                        { pStock.hp = pStock.maxhp; }
                        pStock.mp = (ushort)Math.Clamp(pStock.mp, 0u, pStock.maxmp);
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
                    { __result = "Raises Intelligence by 1 \nand full HP recovery \nfor one ally."; }
            }
        }

        [HarmonyPatch(typeof(fclShopCalc), nameof(fclShopCalc.shpCalcItemPrice))]
        private class PatchShopIncensePrice
        {
            private static bool Prefix(ref int __result, int Index, sbyte Mode)
            {
                // Basic Result
                __result = 0;

                // Grab the Item ID
                int ItemID = fclShopCalc.shpGetItemID(Index, Mode);

                // If the Mode is 1.
                if(Mode == 1)
                {
                    if(ItemID < datItem.tbl.Length)
                    {
                        // If the item is an Incense, adjust the Sell Price.
                        if (ItemID > 0x25 && ItemID < 0x2c)
                        { __result = 2000; }

                        // Set the Sell Price.
                        __result = (int)(datItem.tbl[ItemID].price >> 1);

                        // Return.
                        return false;
                    }
                }
                
                // Return original function otherwise.
                return true;
            }
        }

        [HarmonyPatch(typeof(fclShopCalc), nameof(fclShopCalc.shpCreateItemList))]
        private class PatchFinalShopAddIncense
        {
            private static void Postfix(ref fclDataShop_t pData)
            {
                // If the current shop is the final shop in the game.
                if(pData.Place == 6)
                {
                    // Loop through the Incense items and add them to the shop.
                    for (int i = 0; i < 6; i++)
                    {
                        if (!EnableIntStat && i == 1)
                            { continue; }
                        pData.BuyItemList[pData.BuyItemCnt++] = (byte)(0x26 + i);
                    }
                }
            }
        }
    }
}
