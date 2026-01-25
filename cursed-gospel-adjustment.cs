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
                datItem.tbl[60].flag = 4;
                datItem.tbl[60].price = 5000u;
                datItem.tbl[60].skillid = 95;
                datItem.tbl[60].use = 1;
                datSkill.tbl[95].capacity = 4;
                datSkill.tbl[95].skillattr = 15;
                datNormalSkill.tbl[95].koukatype = 1;
                datNormalSkill.tbl[95].program = 14;
                datNormalSkill.tbl[95].targetcntmax = 1;
                datNormalSkill.tbl[95].targetcntmin = 1;
                datNormalSkill.tbl[95].targettype = 3;

                // Add the Tome of Rebirth, which allows you to respec the Demi-Fiend.
                // I have no idea if this will even work.
                /*
                datItem.tbl[59].flag = 4;
                datItem.tbl[59].price = 50000u;
                datItem.tbl[59].skillid = 175;
                datItem.tbl[59].use = 1;
                datSkill.tbl[175].capacity = 4;
                datSkill.tbl[175].skillattr = 15;
                datNormalSkill.tbl[175].koukatype = 1;
                datNormalSkill.tbl[175].program = 14;
                datNormalSkill.tbl[175].targetcntmax = 1;
                datNormalSkill.tbl[175].targetcntmin = 1;
                datNormalSkill.tbl[175].targettype = 3;
                */
            }
        }

        [HarmonyPatch(typeof(fclShopCalc), nameof(fclShopCalc.shpCreateItemList))]
        private class PatchShopAddCursedGospel
        {
            public static void Postfix(ref fclDataShop_t pData)
            {
                // Add the Cursed Gospel to Asakusa, Manikin Collector and Tower of Kagatsuchi Shops.
                if (pData.Place >= 4 && pData.Place <= 6)
                { pData.BuyItemList[pData.BuyItemCnt++] = 60; }
            }
        }

        [HarmonyPatch(typeof(datItemName), nameof(datItemName.Get))]
        private class PatchGetItemName
        {
            public static void Postfix(ref int id, ref string __result)
            {
                // If the item ID is correct, rename the item.
                // It's unused normally, so this is fine.
                //if (id == 59)
                //    { __result = "Tome of Rebirth"; }

                if (id == 60)
                    { __result = "Cursed Gospel"; }
            }
        }

        [HarmonyPatch(typeof(datItemHelp_msg), nameof(datItemHelp_msg.Get))]
        private class PatchGetItemHelpMsg
        {
            public static void Postfix(ref int id, ref string __result)
            {
                // If the item ID is correct, change the help message.
                //if (id == 59)
                //    { __result = "Resets the Demi-Fiend's stats \nthen levels him back up."; }

                // If the item ID is correct, change the help message.
                if (id == 60)
                    { __result = "Demi-fiend earns enough EXP \nto level up but loses one level."; }
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datExecSkill))]
        private class PatchItemEffects
        {
            private static bool Prefix(int nskill)
            {
                switch (nskill)
                {
                    // If this Skill is from the Tome of Rebirth item.
                    // If it isn't, return and run the original function.
                    /*
                    case 175:
                        {
                            // Grab Demi-Fiend and check his Level.
                            datUnitWork_t work = dds3GlobalWork.DDS3_GBWK.unitwork[0];

                            // If the Demi-Fiend's Stats are maxed out, don't bother with the respec.
                            if ((EnableIntStat && datCalc.datGetBaseParam(work, 0) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 1) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 2) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 3) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 4) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 5) >= MAXSTATS) || (datCalc.datGetBaseParam(work, 0) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 1) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 2) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 3) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 4) >= MAXSTATS &&
                            datCalc.datGetBaseParam(work, 5) >= MAXSTATS))
                            { return false; }

                            // If he's above Level 1.
                            if (work.level > 1)
                            {
                                // Save his previous level for Stat Counting.
                                int prevLevel = work.level;

                                // Set his level directly to 1.
                                work.level = 1;

                                // Create a list of Stat IDs, then remove 1 if EnableIntStat is false.
                                List<int> statlist = new List<int> { 0, 1, 2, 3, 4, 5 };
                                if (!EnableIntStat)
                                { statlist.Remove(1); }

                                // Grab current Stat Points earned.
                                int ptCnt = 0;
                                for (int i = 0; i < statlist.Count; i++)
                                {
                                    sbyte heartParam = rstCalcCore.cmbGetHeartsParam((sbyte)dds3GlobalWork.DDS3_GBWK.heartsequip, (sbyte)statlist[i]);
                                    ptCnt += work.param[statlist[i]] - 4 - heartParam;
                                    work.param[statlist[i]] = (sbyte)(4 + heartParam);
                                }

                                // If the current Stat Points earned is over 0 and Demi-Fiend's original Level is over 1.
                                if (ptCnt > 0 && prevLevel > 1)
                                { ptCnt -= prevLevel * POINTS_PER_LEVEL; }

                                // If you have Bonus Points from Incenses, make sure to saves those for the Level Up process.
                                // IncensePoints = ptCnt > 0 ? ptCnt : 0;

                                // Fix HP/MP calculations.
                                work.maxhp = (ushort)datCalc.datGetMaxHp(work);
                                work.maxmp = (ushort)datCalc.datGetMaxMp(work);
                                work.hp = (ushort)Math.Clamp(work.hp, 0u, work.maxhp);
                                work.mp = (ushort)Math.Clamp(work.mp, 0u, work.maxmp);
                                
                                // Set Demi-Fiend's object to the changed unit.
                                dds3GlobalWork.DDS3_GBWK.unitwork[0] = work;
                                return false;
                            }
                            break;
                        }
                    */
                    case 95:
                        {
                            // Grab Demi-Fiend and check his Level.
                            datUnitWork_t work = dds3GlobalWork.DDS3_GBWK.unitwork[0];

                            // If his Level is over 1.
                            if (work.level > 1)
                            {
                                // Set EXP needed to exactly 1 before the next level.
                                work.exp = rstCalcCore.GetNextExpDisp(work, 0) - 1;

                                // Create a list of Stat IDs, then remove 1 if EnableIntStat is false.
                                List<int> statlist = new List<int> { 0, 1, 2, 3, 4, 5 };
                                if (!EnableIntStat)
                                { statlist.Remove(1); }

                                // Iterate through Stats and reduce them at random a number of times equal to the current Stat points per level.
                                // If EnableStatScaling is false, it's just 1 point.
                                int changes = 1 * (EnableStatScaling ? POINTS_PER_LEVEL : 1);
                                while (changes > 0 && statlist.Count > 0)
                                {
                                    // Randomize the Stat ID.
                                    int statID = statlist[(int)dds3KernelCore.dds3GetRandIntA((uint)statlist.Count)];

                                    // If the Base Stat is over 1, decrement it and the change count.
                                    if (work.param[statID] - tblHearts.fclHeartsTbl[dds3GlobalWork.DDS3_GBWK.heartsequip].GrowParamTbl[statID] > 1)
                                    {
                                        work.param[statID]--;
                                        changes--;
                                    }

                                    // Remove anything that can't be reduced from being potentially chosen again.
                                    else
                                    { statlist.Remove(statID); }
                                }

                                // Fix HP/MP calculations.
                                work.maxhp = (ushort)datCalc.datGetMaxHp(work);
                                work.maxmp = (ushort)datCalc.datGetMaxMp(work);
                                work.hp = (ushort)Math.Clamp(work.hp, 0u, work.maxhp);
                                work.mp = (ushort)Math.Clamp(work.mp, 0u, work.maxmp);

                                // Set Demi-Fiend's object to the changed unit.
                                dds3GlobalWork.DDS3_GBWK.unitwork[0] = work;
                                return false;
                            }
                            break;
                        }
                }
            return true;
            }
        }
    }
}