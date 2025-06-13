using HarmonyLib;
using Il2Cpp;
using Il2Cppfacility_H;
using Il2Cppnewdata_H;
using MelonLoader;

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {
        [HarmonyPatch(typeof(ModernStatsSystem), nameof(ModernStatsSystem.OnInitializeMelon))]
        private class PatchOnInitializeCursedGospel
        {
            private static void Postfix()
            {
                // Add the Cursed Gospel from Matthiew Purple's mod.
                // Unfortunately I had to completely copy it and adjust it for the Stat adjustments.
                // I couldn't figure out how else to implement the fix to it.
                datItem.tbl[60].flag = 0;
                datItem.tbl[60].price = 100000u;
                datItem.tbl[60].skillid = 95;
                datItem.tbl[60].use = 1;
                datSkill.tbl[95].capacity = 4;
                datSkill.tbl[95].skillattr = 15;
                datNormalSkill.tbl[95].koukatype = 1;
                datNormalSkill.tbl[95].program = 14u;
                datNormalSkill.tbl[95].targetcntmax = 1;
                datNormalSkill.tbl[95].targetcntmin = 1;
                datNormalSkill.tbl[95].targettype = 3;
            }
        }

        [HarmonyPatch(typeof(fclShopCalc), nameof(fclShopCalc.shpCreateItemList))]
        private class PatchFinalShopAddCursedGospel
        {
            public static void Postfix(ref fclDataShop_t pData)
            {
                if (pData.Place == 6 && dds3GlobalWork.DDS3_GBWK.item[60] == 0)
                {
                    pData.BuyItemList[pData.BuyItemCnt++] = 60;
                }
            }
        }

        [HarmonyPatch(typeof(datItemName), nameof(datItemName.Get))]
        private class PatchGetItemName
        {
            public static void Postfix(ref int id, ref string __result)
            {
                if (id == 60)
                    { __result = "Cursed Gospel"; }
            }
        }

        [HarmonyPatch(typeof(datItemHelp_msg), nameof(datItemHelp_msg.Get))]
        private class PatchGetItemHelpMsg
        {
            public static void Postfix(ref int id, ref string __result)
            {
                if (id == 60)
                    { __result = "Demi-fiend earns enough EXP \nto level up but loses one level. \nReusable."; }
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datExecSkill))]
        private class PatchCursedGospelEffect
        {
            private static void Postfix(ref int nskill)
            {
                // If this Skill is from the Cursed Gospel item mod.
                // If it isn't, return and run the original function.
                if (nskill != 95)
                    { return; }

                // Grab Demi-Fiend and check his Level.
                // If it's 1 or lower, return and run the original function.
                datUnitWork_t work = dds3GlobalWork.DDS3_GBWK.unitwork[0];
                if (work.level <= 1)
                    { return; }
                
                // Lower Level by 1.
                work.level--;

                // Set EXP needed to exactly 1 before the next level.
                work.exp = rstCalcCore.GetNextExpDisp(work, 0) - 1;

                // Create a list of Stat IDs, then remove 1 if EnableIntStat is false.
                List<int> statlist = new List<int> { 0, 1, 2, 3, 4, 5 };
                if (!EnableIntStat)
                    { statlist.Remove(1); }

                // Iterate through Stats and reduce them at random a number of times equal to the current Stat points per level.
                // If EnableStatScaling is false, it's just 1 point.
                int changes = 1 * (EnableStatScaling ? POINTS_PER_LEVEL : 1);
                System.Random rng = new();
                while (changes > 0 && statlist.Count > 0)
                {
                    // Randomize the Stat ID.
                    int statID = statlist[rng.Next(statlist.Count)];

                    // If the Base Stat is over 1, decrement it and the change count.
                    if (work.param[statID] - tblHearts.fclHeartsTbl[dds3GlobalWork.DDS3_GBWK.heartsequip].GrowParamTbl[statID] > 1)
                    {
                        work.param[statID]--;
                        changes--;
                    }
                    else
                        { statlist.Remove(statID); }
                }
            }
        }
    }
}