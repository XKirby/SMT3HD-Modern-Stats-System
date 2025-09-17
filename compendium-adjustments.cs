using HarmonyLib;
using Il2Cpp;
using Il2Cppfacility_H;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppnewdata_H;
using MelonLoader;

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {
        // NOTE: Most of the functionality in this file was from the Compendium Discounts mod by Matthiew Purple.
        // I made some adjustments to it as well as changed the overall formula to account for Int and some new Stat Scaling.
        // Technically there's already a function for the price called "fclEncyc.GetPriceForSummon()", but it goes unused in the vanilla game. Not sure why.

        public const float limitDiscount = 50f; // What the discount scales towards the closer you get to 100% compendium completion
        public const float finalDiscount = 50f; // Discount once the compendium is completed

        public static short currentRecord;
        private static short listWindowCursorPos = 0;
        private static Il2CppStructArray<short> pelemList;

        private class CompendiumPriceHelper
        {
            public static int GetPrice(fclencyceelem_t pelem)
            {
                // If the unit is null, return nothing.
                if (pelem == null)
                    { return 0; }

                // Summoning price formula with applied discount
                return (int)((double)GetBasePrice(pelem) * (double)GetDiscountFactor(limitDiscount, finalDiscount));
            }

            public static int GetBasePrice(fclencyceelem_t pelem)
            {

                // If the unit is null, return nothing.
                if (pelem == null)
                    { return 0; }

                // Grab unit's stats
                int exp = (int)pelem.exp;
                int lvl = (int)pelem.level;
                int str = (int)pelem.param[0] + (int)pelem.mitamaparam[0];
                int intStat = (EnableIntStat ? (int)pelem.param[1] + (int)pelem.mitamaparam[1] : 0);
                int mag = (int)pelem.param[2] + (int)pelem.mitamaparam[2];
                int vit = (int)pelem.param[3] + (int)pelem.mitamaparam[3];
                int agi = (int)pelem.param[4] + (int)pelem.mitamaparam[4];
                int luc = (int)pelem.param[5] + (int)pelem.mitamaparam[5];

                // Return the following result
                return (int)(Math.Pow((double)(str + mag + vit + agi + luc + intStat) / (EnableStatScaling ? (EnableIntStat ? (double)STATS_SCALING + (double)STATS_SCALING / 6d : (double)STATS_SCALING) : 1d), 2) * 5d);
            }

            // Discount calculator
            public static float GetDiscountFactor(float limitDiscount, float finalDiscount)
            {
                int compendiumProgress = fclEncyc.fclEncycGetRatio2();
                float discountFactor;

                if (compendiumProgress < 100)
                {
                    discountFactor = 1 - (compendiumProgress * limitDiscount) / (100f * 100f);
                }
                else
                {
                    discountFactor = 1 - (compendiumProgress * finalDiscount) / (100f * 100f);
                    discountFactor *= 2; // To counteract the 50% discount of the vanilla game when the compendium is completed
                }

                return discountFactor;
            }
        }

        [HarmonyPatch(typeof(ModernStatsSystem), nameof(ModernStatsSystem.OnInitializeMelon))]
        private class PatchOnInitializeCompendium
        {
            private static void Postfix()
            {
                // If enabled, alter the Mitama fusion bonuses.
                if (EnableIntStat)
                {
                    // Mitama Bonuses
                    fclCombineTable.fclSpiritParamUpTbl[0].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(3 + 1).ToArray();
                    fclCombineTable.fclSpiritParamUpTbl[1].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                    fclCombineTable.fclSpiritParamUpTbl[2].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                    fclCombineTable.fclSpiritParamUpTbl[3].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(2 + 1).ToArray();
                }
            }
        }

        [HarmonyPatch(typeof(fclEncyc), nameof(fclEncyc.GetDevilParam))]
        private class PatchGetCompendiumDemonParam
        {
            // Returns a Compendium Demon's stat, making sure to cap it appropriately.
            private static bool Prefix(out int __result, fclencyceelem_t pelem, int type)
            {
                __result = pelem.param[type] + pelem.mitamaparam[type] < MAXSTATS ? pelem.param[type] + pelem.mitamaparam[type] : MAXSTATS;
                return false;
            }
        }

        [HarmonyPatch(typeof(frFont), nameof(frFont.frReplaceLocalizeText))]
        private class PatchCompendiumConfirmText
        {
            public static void Postfix(ref string __result)
            {
                // Grab the current record unit and set its price for this particular menu segment.
                fclencyceelem_t pelem = dds3GlobalWork.DDS3_GBWK.encyc_record.pelem[currentRecord];
                int macca = CompendiumPriceHelper.GetPrice(pelem);

                // Replace Mido's text to display the correct price when enough macca
                if (__result.Contains("<SP7><FO1>It will cost <CO4>") && __result.Contains("Are you okay with that?"))
                    { __result = "<SP7><FO1>It will cost <CO4>" + macca + " Macca. <CO0>Are you okay with that?"; }

                // Replace Mido's text to display the correct price when not enough macca
                else if (__result.Contains("<SP7><FO1>It will cost <CO4>") && __result.Contains("But it seems you don't have enough."))
                    { __result = "<SP7><FO1>It will cost <CO4>" + macca + " Macca... <CO0>But it seems you don't have enough."; }
            }
        }

        [HarmonyPatch(typeof(lstListWindow), nameof(lstListWindow.lstMoveCursor))]
        private class PatchMoveListWindowCursor
        {
            // Grabs the current List Window's Cursor Position.
            // Needed for later.
            private static void Postfix(lstListWindow_t pListWindow, sbyte Dir)
                { listWindowCursorPos = (short)pListWindow.CursorInfo.CursorPos.Index; }
        }

        [HarmonyPatch(typeof(fclEncyc), nameof(fclEncyc.CalcPhaseReadTop))]
        private class PatchSortListWindow
        {
            // Grabs the current Sorting Method.
            // Needed for later.
            private static void Postfix(fclEncyc.instance_tag pinst)
            {
                if (pinst.sort == 0)
                    { pelemList = pinst.praceindex; }
                else
                    { pelemList = pinst.plevelindex; }
            }
        }

        [HarmonyPatch(typeof(CounterCtr), nameof(CounterCtr.Set))]
        private class PatchPriceCounterCtrSet
        {
            public static void Prefix(ref int val, ref CounterCtr __instance)
            {
                if (__instance.transform.GetParent().name.Contains("dlistsum_row0"))
                {
                    // Grab the current list object index.
                    int id = int.Parse(__instance.transform.GetParent().name.Replace("dlistsum_row", ""));

                    // Offset the original id with where the compendium list menu lines up.
                    short index = (short)(id - 1 + listWindowCursorPos);

                    if (pelemList == null)
                        { val = 0; return; }

                    // Grab the Compendium's Demon ID list
                    Il2CppStructArray<short> demonIDlist = pelemList;

                    // If the index overshoots the list, return 0.
                    if (index >= demonIDlist.Length)
                        { val = 0; return; }

                    // Grab the (hopefully) correct unit out of the list.
                    fclencyceelem_t pelem = dds3GlobalWork.DDS3_GBWK.encyc_record.pelem[demonIDlist[index]];

                    // If the unit isn't null.
                    if (pelem == null)
                        { val = 0; return; }

                    // Apply discount on displayed prices
                    val = CompendiumPriceHelper.GetPrice(pelem);
                }
            }
        }

        [HarmonyPatch(typeof(fclEncyc), nameof(fclEncyc.PrepSummon))]
        private class PatchPrepSummon
        {
            public static void Prefix(ref fclEncyc.readmainwork_tag pwork)
            {
                // Remember the compendium record's ID (for another function later)
                currentRecord = pwork.recindex;
            }

            public static void Postfix(ref fclEncyc.readmainwork_tag pwork, ref int __result)
            {
                // Get the unit about to be summoned
                fclencyceelem_t pelem = dds3GlobalWork.DDS3_GBWK.encyc_record.pelem[pwork.recindex];

                // Get discounted price for that summon
                pwork.mak = CompendiumPriceHelper.GetPrice(pelem);

                // If enough macca post-discount but not pre-discount (and stock not full and not already in stock and something idk)
                if (__result == 0 && dds3GlobalWork.DDS3_GBWK.maka >= pwork.mak && datCalc.datCheckStockFull() == 0 && datCalc.datSearchDevilStock(pelem.id) == -1 && pwork.flags == 80)
                {
                    pwork.flags = (ushort)(pwork.flags | 1);
                    __result = 1;
                }
            }
        }

        [HarmonyPatch(typeof(fclMisc), nameof(fclMisc.fclScriptStart))]
        private class PatchCompendiumPrice
        {
            public static void Prefix(ref int StartNo)
            {
                if (StartNo == 18)
                {
                    // Get the unit about to be summoned
                    var pelem = dds3GlobalWork.DDS3_GBWK.encyc_record.pelem[currentRecord];

                    // Summoning price formula with applied discount
                    int price = CompendiumPriceHelper.GetPrice(pelem);

                    // If enough macca post-discount but not pre-discount (and stock not full and not already in stock and something idk)
                    if (dds3GlobalWork.DDS3_GBWK.maka >= price && datCalc.datCheckStockFull() == 0 && datCalc.datSearchDevilStock(pelem.id) == -1)
                    {
                        StartNo = 17;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(fclCombineCalcCore), nameof(fclCombineCalcCore.cmbCalcParamPowerUp))]
        private class PatchMitamaPowerUp
        {
            private static bool Prefix(out sbyte __result, ushort MitamaID, datUnitWork_t pStock)
            {
                // Return value. You get the drill by this point.
                __result = 0;

                // Check the Mitama ID.
                int mitama = MitamaID -= 40;
                if (mitama < 0 || mitama >= 4)
                { return false; }

                // If everything's capped, return.
                if (EnableIntStat && pStock.param[0] + pStock.mitamaparam[0] >= MAXSTATS &&
                    pStock.param[1] + pStock.mitamaparam[1] >= MAXSTATS &&
                    pStock.param[2] + pStock.mitamaparam[2] >= MAXSTATS &&
                    pStock.param[3] + pStock.mitamaparam[3] >= MAXSTATS &&
                    pStock.param[4] + pStock.mitamaparam[4] >= MAXSTATS &&
                    pStock.param[5] + pStock.mitamaparam[5] >= MAXSTATS)
                { return false; }

                // If everything's capped and Int is disabled, return.
                // Yes I needed two checks, don't ask please.
                else if (pStock.param[0] + pStock.mitamaparam[0] >= MAXSTATS &&
                    pStock.param[2] + pStock.mitamaparam[2] >= MAXSTATS &&
                    pStock.param[3] + pStock.mitamaparam[3] >= MAXSTATS &&
                    pStock.param[4] + pStock.mitamaparam[4] >= MAXSTATS &&
                    pStock.param[5] + pStock.mitamaparam[5] >= MAXSTATS)
                { return false; }

                // Unseeded random number generator.
                System.Random rng = new();

                // Pull a random stat from whatever the Mitama's upgradable stat pool is.
                ushort paramID = fclCombineTable.fclSpiritParamUpTbl[mitama].ParamType[rng.Next(fclCombineTable.fclSpiritParamUpTbl[mitama].ParamType.Length)];

                // If it's somehow below zero, just return here and don't continue.
                if (paramID < 0)
                { return false; }

                // If it's within the proper range
                if (paramID < pStock.param.Length && paramID < pStock.mitamaparam.Length)
                {
                    // Check the chance of the stat upgrading and if it's less than 1, set it to 1.
                    int paramNewValue = (pStock.param[paramID] * fclCombineTable.fclSpiritParamUpTbl[mitama].UpRate) / 100 - pStock.param[paramID];
                    if (paramNewValue <= 0)
                    { paramNewValue = 1; }

                    // Make sure it doesn't overwrite previous Mitama Bonuses.
                    paramNewValue += pStock.mitamaparam[paramID];

                    // If it's under or equal to the maximum, set the Mitama Bonus.
                    if (pStock.param[paramID] + paramNewValue <= MAXSTATS)
                    {
                        pStock.mitamaparam[paramID] = (sbyte)paramNewValue;
                        __result = 1;
                    }
                }
                return false;
            }
        }
    }
}
