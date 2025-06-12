using HarmonyLib;
using Il2Cpp;
using Il2Cppcamp_H;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppnewbattle_H;
using Il2Cppnewdata_H;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(ModernStatsSystem.ModernStatsSystem), "Modern Stats System", "1.0.0", "X Kirby")]
[assembly: MelonGame("アトラス", "smt3hd")]

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {
        // Stat manipulation variables
        private const int MAXSTATS = 100;
        private const int MAXHPMP = 9999;
        private const int POINTS_PER_LEVEL = 2;
        private const bool EnableIntStat = true;
        private const bool EnableStatScaling = true;

        // Stat Bar manipulation variables
        private const string BundlePath = "smt3hd_Data/StreamingAssets/PC/";
        private const float BAR_SCALE_X = (float)(40.0f / MAXSTATS) + (float)(16.0f / MAXSTATS);
        private const float BAR_SEGMENT_X = 18*(float)(40.0f / MAXSTATS) - (float)(14.0f / MAXSTATS / 2);
        private static uint[] pCol = (uint[])Array.CreateInstance(typeof(uint), MAXSTATS);
        private static AssetBundle barData = null;
        private static Texture2D[] barAsset = { null, null, null, null, null, null };
        private static Sprite[] barSprite = { null, null, null, null, null, null };
        private static string[] paramNames = { "Str", "Int", "Mag", "Vit", "Agi", "Luc" };
        private const string barSpriteName = "sstatusbar_base_empty";
        private static string[] StatusBarValues = { "shpnum_current", "shpnum_full", "smpnum_current", "smpnum_full" };
        private static string[] StockBarValues = { "barhp", "barmp" };
        private static string[] AnalyzeBarValues = { "banalyze_hp_known", "banalyze_mp_known" };
        private static string[] PartyBarValues = { "barhp", "barmp" };

        // Menu manipulation variables
        private static bool SettingAsignParam;
        private static bool EvoCheck;

        public override void OnInitializeMelon()
        {
            // Seeded random number generator
            // I had to. I didn't want Int to be completely random for every demon every time you booted the game.
            System.Random rng = new(5318008);

            // Search through all of the demons.
            for (int i = 0; i < datDevilFormat.tbl.Length; i++)
            {
                // If enabled, give the demons Int.
                if (EnableIntStat)
                {
                    // Make sure the offset of stats for Int is positive or negative.
                    int sign;
                    do
                    { sign = rng.Next(-1, 1); }
                    while (sign == 0);

                    // Set up Int.
                    datDevilFormat.tbl[i].param[1] = (sbyte)Math.Clamp(datDevilFormat.tbl[i].param[2] + datDevilFormat.tbl[i].param[2] * sign * 10 / 100, 1, MAXSTATS);
                }
                
                // If enabled, scale each demon's stats by how many points per level is set.
                // Additionally, recalculate HP/MP for anything that isn't a boss or forced encounter.
                if (EnableStatScaling)
                {
                    for (int j = 0; j < datDevilFormat.tbl[i].param.Length; j++)
                        { datDevilFormat.tbl[i].param[j] *= POINTS_PER_LEVEL; }
                    if (i < 254)
                    {
                        datDevilFormat.tbl[i].maxhp = (ushort)((datDevilFormat.tbl[i].param[3] / POINTS_PER_LEVEL + datDevilFormat.tbl[i].level) * 6);
                        datDevilFormat.tbl[i].hp = datDevilFormat.tbl[i].maxhp;
                        datDevilFormat.tbl[i].maxmp = (ushort)((datDevilFormat.tbl[i].param[2] / POINTS_PER_LEVEL + datDevilFormat.tbl[i].level) * 3);
                        datDevilFormat.tbl[i].mp = datDevilFormat.tbl[i].maxmp;
                    }
                }
            }

            // Searche through al of the Magatamas.
            for (int i = 0; i < tblHearts.fclHeartsTbl.Length; i++)
            {

                // If enabled, gives the Magatama Int.
                if (EnableIntStat)
                {
                    // Like above, make sure the bonus is positive or negative.
                    int sign;
                    do
                        { sign = rng.Next(-1, 1); }
                    while (sign == 0);

                    // Set up Int.
                    tblHearts.fclHeartsTbl[i].GrowParamTbl[1] = (sbyte)Math.Clamp(tblHearts.fclHeartsTbl[i].GrowParamTbl[2] + tblHearts.fclHeartsTbl[i].GrowParamTbl[2] * sign * 10 / 100, 1, MAXSTATS);
                    tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[1] = (sbyte)Math.Clamp(tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[2] + tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[2] * sign * 10 / 100, 1, MAXSTATS);
                }

                // If enabled, scale the Magatamas by how many points per level is set.
                if (EnableStatScaling)
                {
                    for (int j = 0; j < tblHearts.fclHeartsTbl[i].GrowParamTbl.Length; j++)
                    {
                        tblHearts.fclHeartsTbl[i].GrowParamTbl[j] *= POINTS_PER_LEVEL;
                        tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[j] *= POINTS_PER_LEVEL;
                    }
                }
            }

            // If enabled, alter the Mitama fusion bonuses.
            if (EnableIntStat)
            {
                fclCombineTable.fclSpiritParamUpTbl[0].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(3 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[1].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[2].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[3].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(2 + 1).ToArray();
            }

            // Gives a head's up to the user if they have the console enabled.
            LoggerInstance.Msg("Modern Stats System Initialized.");
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstChkParamLimitAll))]
        private class PatchChkParamLimitAll
        {
            private static bool Prefix(ref int __result, datUnitWork_t pStock, bool paramSet = true)
            {
                // Return value initialization
                __result = 0;
                
                // If your stats are not capped completely, return.
                if (PatchGetBaseParam.GetParam(pStock, 0) >= MAXSTATS)
                {
                    if (EnableIntStat && PatchGetBaseParam.GetParam(pStock, 1) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 2) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 3) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 4) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 5) < MAXSTATS) { return false; }

                    // If you got to this point, your stats are completely maxed out.
                    // Additionally, if this is true, recalculate your HP/MP.
                    if (paramSet)
                        { rstcalc.rstSetMaxHpMp(0, ref pStock); }

                    // Make sure to return 1 to tell the game your stats are capped.
                    __result = 1;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datAddPlayerParam))]
        private class PatchAddPlayerParam
        {
            private static bool Prefix(int id, int add)
            {
                // This looks through each demon you control and checks if their ID is 0, which is Demi-Fiend.
                // Yes, this means if you *somehow* have Demi-Fiend more than once, it'll add the stat to each of them.
                foreach (datUnitWork_t work in dds3GlobalWork.DDS3_GBWK.unitwork.Where(x => x.id == 0))
                {
                    // Setting a reference variable for rstcalc.rstSetMaxHpMp because otherwise it won't work.
                    datUnitWork_t pStock = work;

                    // Adds to then clamps whatever stat you're adding to.
                    // Note that "add" can be negative.
                    pStock.param[id] += (sbyte)add;
                    if (datCalc.datGetPlayerParam(id) >= MAXSTATS)
                        { pStock.param[id] = MAXSTATS; }
                    if (datCalc.datGetPlayerParam(id) < 1)
                        { pStock.param[id] = 1; }

                    // Recalculate HP/MP then heal them.
                    rstcalc.rstSetMaxHpMp(1, ref pStock);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetBaseParam))]
        private class PatchGetBaseParam
        {
            private static bool Prefix(ref int __result, datUnitWork_t work, int paratype)
            {
                // Just returns the parameter of the given type.
                __result = GetParam(work, paratype);
                return false;
            }
            public static int GetParam(datUnitWork_t work, int paratype)
            {
                // Pulls the parameter for the other function and clamps it between 0 and the new maximum.
                int result = work.param[paratype];
                result = Math.Clamp(result, 0, MAXSTATS);
                return result;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetParam))]
        private class PatchGetParam
        {
            private static bool Prefix(ref int __result, datUnitWork_t work, int paratype)
            {
                // Returns the base stat of the given parameter, plus whatever Mitama bonuses the user has.
                __result = datCalc.datGetBaseParam(work, paratype) + work.mitamaparam[paratype];
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpMisc), nameof(cmpMisc.cmpUseItemKou))]
        private class PatchIncense
        {
            private static bool Prefix(ushort ItemID, datUnitWork_t pStock)
            {
                // Checks the currently used item's ID and make sure it's the Stat Incense items.
                int statID = ItemID - 0x26;
                if (statID > -1 && statID < 6)
                {
                    // Increases the target's stat if it isn't above the maximum, then recalculates HP/MP and heals them.
                    if (rstCalcCore.cmbGetParamBase(ref pStock, statID) < MAXSTATS)
                    {
                        pStock.param[statID]++;
                        rstcalc.rstSetMaxHpMp(1, ref pStock);
                    }
                    return false;
                }
                return true;
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

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetNormalAtkPow))]
        private class PatchGetNormalAtkPow
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                // Result uses the initial formula here.
                __result = (datCalc.datGetParam(work, 0) + work.level) * 2;

                // If Enabled, use a new formula.
                if (EnableStatScaling)
                    { __result = (datCalc.datGetParam(work, 0) * 2 / POINTS_PER_LEVEL) + work.level * 2; }

                // I dunno what "badstatus" actually is besides a bitflag, but if this setup works, your attack power is basically halved.
                if ((work.badstatus & 0xFFF) == 0x40)
                    { __result = __result >> 1; }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetButuriAttack))]
        private class PatchGetPhysicalPow
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                __result = 0;

                // Set up the attacker and defender objects from the form indices.
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(dformindex);

                // Get the Str of the attacker.
                int paramValue = datCalc.datGetParam(attacker, 0);

                // This value changes the damage output a bit.
                int unkval = 48;

                // This eventually becomes the final damage value.
                int finalvalue = 0;

                // Unseeded random number generator
                System.Random rng = new();

                // If enabled, divide the stats by points per level's amount.
                if (EnableStatScaling)
                    { paramValue /= POINTS_PER_LEVEL; }

                // Do some initial math for basic attacks.
                finalvalue = (int)(((datCalc.datGetNormalAtkPow(attacker) * 2) * 1.33f) * 0.8f);

                // If you're not doing a basic attack, then use the Physical Skill formula.
                // Note that "waza" is Skill Power.
                if (nskill != 0)
                {
                    finalvalue = (int)((float)datCalc.datGetNormalAtkPow(attacker) * (float)waza * 2 / 23.2f * 0.8f);
                    unkval = 50;
                }

                // If enabled, that damage output will scale a bit differently.
                if (EnableStatScaling)
                    { unkval = 64; }

                // Use that number and the attacker's level to figure out some damage reduction.
                int reduction = unkval / (attacker.level + 10);

                // If not enabled, the final value is cut down to 60%.
                if (!EnableStatScaling)
                    { finalvalue = (int)((float)finalvalue * 0.6f); }

                // Reduce the final value by the reduction.
                finalvalue = (int)(finalvalue - reduction);
                __result = finalvalue;

                // If the difficulty bit is 3 and Event Bit 0x8a0 is true, multiply damage by 134%.
                if (dds3ConfigMain.cfgGetBit(9) == 3)
                {
                    __result = (int)(finalvalue * 1.34f);
                    if (!EventBit.evtBitCheck(0x8a0))
                    {
                        __result = finalvalue;
                    }
                }

                // If difficulty is 2, it's also multipled by 134%.
                // Otherwise it's just normal.
                else
                {
                    __result = finalvalue;
                    if (dds3ConfigMain.cfgGetBit(9) == 2)
                        { __result = (int)(finalvalue * 1.34); }
                }

                // This multiplies the final result by the attacker's attack buffs and the defender's defense buffs.
                __result = (int)((float)__result * nbCalc.nbGetHojoRitu(sformindex, 4) * nbCalc.nbGetHojoRitu(dformindex, 7));

                // If enabled, introduce some further damage mitigation that never got used and do some extra math to it.
                if (EnableStatScaling)
                    { __result = (int)((float)__result * 255f / (255f + (float)datCalc.datGetDefPow(defender) * ((float)defender.level / 100))); }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMagicAttack))]
        private class PatchGetMagicPow
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                // Set up the attacker/defender objects from the indices.
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);

                // There's a Level Limit for Magic Skills normally.
                int LevelLimit = attacker.level;

                // Magic Skills additionally have a Base Power and Magic Damage Limit.
                int skillLimit = datNormalSkill.tbl[nskill].magiclimit;
                int skillBase = datNormalSkill.tbl[nskill].magicbase;

                // Set up the initial Damage value.
                int damageCalc = (int)((float)waza * (float)attacker.level * 2f / 21 + (float)skillBase);
                
                // If you exceeded the damage limit, scale it back.
                if (damageCalc > skillLimit)
                    { damageCalc = skillLimit; }

                // If enabled, scale it slightly less harshly and make sure it's uncapped.
                if (EnableStatScaling)
                    { damageCalc = (int)((float)waza * (float)attacker.level * 2f / 20f + (float)skillBase); }

                // If not enabled and the Level Limit is over 160, cap it to 160.
                // This means it'll stop scaling past level 160.
                // The maximum level is 255, but really you'll probably not get there without some insane grinding.
                if (LevelLimit > 160 && !EnableStatScaling)
                    { LevelLimit = 160; }

                // Grab the attacker's Mag.
                int param = datCalc.datGetParam(attacker, 2);

                // If Enabled, grab Int instead.
                if (EnableIntStat)
                    { param = datCalc.datGetParam(attacker, 1); }

                // If enabled, perform some new math.
                // Otherwise, use the game's normal formula.
                if (EnableStatScaling)
                {
                    param /= POINTS_PER_LEVEL;
                    damageCalc = (int)((float)damageCalc + ((float)waza + (float)skillBase) * 2 + (float)damageCalc / 100f * ((float)param - ((float)LevelLimit / 5f + 4f)) * 2.5f * 0.8f);
                }
                else
                    { damageCalc = (int)((float)damageCalc + (float)damageCalc / 100f * ((float)param - ((float)LevelLimit / 5f + 4f)) * 2.5f * 0.8f); }
                
                // This second "damageCalc" number is to make sure things don't get out of hand normally.
                int damageCalc2 = damageCalc;

                // If not enabled, use the game's Magic stat capping functions.
                if (!EnableStatScaling)
                {

                    // If your Level is over 100.
                    if (attacker.level > 100)
                    {
                        // Calculate the Skill's Magic Power for 100 levels.
                        int wazaCalc = waza * 200;
                        int levelcheck = 100;

                        // Loop until the above level check is greater or equal to your level.
                        do
                        {
                            // Do some math, then make sure it doesn't exceed the Skill's Power Limit.
                            damageCalc2 = (wazaCalc * attacker.level) / 21 + skillBase;
                            if (damageCalc2 > skillLimit)
                                { damageCalc2 = skillLimit; }

                            // Also make sure it doesn't exceed the Limit Limit.
                            LevelLimit = levelcheck;
                            if (LevelLimit > 160)
                                { LevelLimit = 160; }

                            // Increment the loop.
                            levelcheck++;

                            // Increment the Power scaling.
                            wazaCalc += waza * 2;
                            
                            // Do some math, then if it's more than the old "damageCalc", replace its value.
                            damageCalc2 = (int)((damageCalc2 + (damageCalc2 / 100) * (param - (LevelLimit / 5 + 4)) * 2.5f) * 0.8f);
                            if (damageCalc <= damageCalc2)
                                { damageCalc = damageCalc2; }

                            // Set the new "damageCalc" to the old one.
                            damageCalc2 = damageCalc;
                        }
                        while (levelcheck < attacker.level);
                    }
                }

                // Don't ask me about the flag, I don't know what it does but it's important.
                if ((attacker.flag >> 5 & 1) != 0)
                {

                    // Do some more math.
                    damageCalc = (int)(damageCalc2 * 0.75f + -50 / (attacker.level + 10));
                    
                    // If difficulty is 3 and that Event Bit is true, then multiply the damage by 134%.
                    if (dds3ConfigMain.cfgGetBit(9) == 3)
                    {
                        damageCalc2 = (int)(damageCalc * 1.34f);
                        if (!EventBit.evtBitCheck(0x8a0))
                            { damageCalc2 = damageCalc; }
                    }

                    // Otherwise, if the difficulty is 2, do the same thing.
                    else
                    {
                        damageCalc2 = damageCalc;
                        if (dds3ConfigMain.cfgGetBit(9) == 2)
                            { damageCalc2 = (int)(damageCalc * 1.34f); }
                    }
                }

                // Multiply the final value by the attacker's Magic buffs and the defender's Magic buffs.
                __result = (int)(damageCalc2 * nbCalc.nbGetHojoRitu(sformindex, 5) * nbCalc.nbGetHojoRitu(dformindex, 7));

                // If enabled, add some more damage mitigation based on the defender's Mag and scale the original result by the hitcount average.
                if (EnableStatScaling)
                {
                    int param2 = datCalc.datGetParam(defender, 2) / POINTS_PER_LEVEL;
                    __result = (int)((float)__result / Math.Ceiling(((float)datNormalSkill.tbl[nskill].targetcntmin + (float)datNormalSkill.tbl[nskill].targetcntmax) / 2) * 255f / (255f + ((float)param2 * 2f + defender.level) * 2f * ((float)defender.level / 100f)));
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMagicKaifuku))]
        private class PatchGetMagicHealing
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                // Get the unit from the user index.
                datUnitWork_t work = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);

                // Unseeded random number generator.
                System.Random rng = new();

                // Get Mag
                int param = datCalc.datGetParam(work, 2);

                // If enabled, get Int instead.
                if (EnableIntStat)
                    { param = datCalc.datGetParam(work, 1); }

                // If enabled, scale.
                if (EnableStatScaling)
                    { param /= POINTS_PER_LEVEL; }

                // Final number considers your Magic buffs.
                __result = (int)(nbCalc.nbGetHojoRitu(sformindex, 5) * (rng.Next(0, 8) + param * 4 + work.level / 10) * waza);
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMakaBaramaki))]
        private class PatchGetMaccaScattering
        {
            private static bool Prefix(out int __result, datUnitWork_t w)
            {
                // Get the Demi-Fiend
                datUnitWork_t work = dds3GlobalWork.DDS3_GBWK.unitwork[0];

                // Get whatever demon is dropping Macca.
                datDevilFormat_t devil = datDevilFormat.Get(w.id, true);

                // Grab Demi-Fiend's Luc.
                int playerLuck = datCalc.datGetParam(work, 5);

                // Grab the player's total Macca.
                int macca = dds3GlobalWork.DDS3_GBWK.maka;
                int baseMacca = macca;

                // Get the above demon's Luc.
                int luck = datCalc.datGetParam(w, 5);

                // Unseeded rng.
                System.Random rng = new();

                // Result init.
                __result = 0;

                // If you have no Macca, skip.
                if (macca == 0)
                    { return false; }

                // Formula values.
                float adjform, baseform;
                adjform = 0.0f;
                baseform = 0.0f;

                // Scale Luck variables.
                if (EnableStatScaling)
                {
                    luck = luck / POINTS_PER_LEVEL;

                    // This makes sure the formula makes sense later.
                    playerLuck = (MAXSTATS - playerLuck) / POINTS_PER_LEVEL;
                }

                // Flag nonsense again.
                if ((w.flag >> 5 & 1) == 0)
                {
                    // Do some math for the base formula.
                    baseform = Mathf.Abs((float)luck / ((float)w.level / 5.0f + 4.0f));

                    // If enabled, do a different one.
                    if (EnableStatScaling)
                        { baseform = Mathf.Abs(30f * ((float)w.level / 25.5f + (float)luck / 2f + 1)); }

                    // If you're under 1/1000, just set the adjustment to zero.
                    if (baseform < 0.001f)
                        { adjform = 0; }
                    // Otherwise, grab a small segment of your Macca.
                    else
                    {
                        adjform = (float)macca / 20.0f * baseform;

                        // If enabled, grab the whole stack instead.
                        if (EnableStatScaling)
                            { adjform = (float)(macca) * baseform; }
                    }
                }

                // If that flag was false.
                // Effectively this means the demon isn't on your team.
                else
                {
                    // If you're fine, return.
                    // Basically this checks if you're not suffering from Panic.
                    if (work.badstatus == 0)
                        { return false; }

                    // Base formula math again.
                    baseform = Mathf.Abs((float)playerLuck / ((float)work.level / 5.0f + 4.0f));

                    // If enabled, scale differently.
                    if (EnableStatScaling)
                    {
                        baseform = Mathf.Abs(30f * ((float)work.level / 25.5f / 2f + (float)playerLuck / 2 + 1));
                    }

                    // Grab the enemy's whole stack.
                    adjform = (float)devil.dropmakka * baseform;
                }

                // Generate a number between 0.0 and 1.0.
                float variance = (float)rng.NextDouble();

                // Do some math and return the result later.
                __result = (int)Mathf.Abs((variance - 0.5f) * 2f * 0.1f * ((float)adjform * 2.0f));

                // If enabled, scale it differently.
                if (EnableStatScaling)
                    { __result = (int)Mathf.Abs(Mathf.Pow((float)adjform, 1.125f) * (1f + Mathf.Log10(adjform)) * (0.1f + variance * 2 / 3) / baseform); }
                
                // If difficulty bit is 1 or lower and some more flag nonsense, divide by 10.
                if(dds3ConfigMain.cfgGetBit(9) <= 1 && (w.flag & 0x20) == 0)
                    { __result = __result / 10; }

                // If result is less than 2, set it to 1.
                if (__result < 2)
                    { __result = 1; }
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetSakePow))]
        private class PatchGetAccuracyPow
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                // Result init.
                // Grabs the user's Level and Agi and does some math.
                __result = work.level + datCalc.datGetParam(work, 4) * 2;

                // Grabs the user's Luc.
                int luc = datCalc.datGetParam(work, 5);

                // If it's under 2 or some weird "badstatus" flag nonsense is true, set Luc to 1.
                if (luc < 2 || (work.badstatus & 0xFFF) == 0x200)
                    { luc = 1; }

                // Add Luc + 10 to the result.
                __result += luc + 10;

                // If enabled, recalculate.
                if (EnableStatScaling)
                    {__result = work.level + (datCalc.datGetParam(work, 4) * 2 + luc) / POINTS_PER_LEVEL + 10;}
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetMagicHitPow))]
        private class PatchGetMagicAccuracy
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                // Result init.
                // Grabs Level, Mag, and Agi and does some math.
                __result = work.level + datCalc.datGetParam(work, 2) + datCalc.datGetParam(work, 4) * 2;

                // If enabled, lowers Mag scaling and adds Int scaling.
                if (EnableIntStat)
                    { __result = work.level + datCalc.datGetParam(work, 1) * 2 + datCalc.datGetParam(work, 2) + datCalc.datGetParam(work, 4); }

                // Grabs Luc.
                int luc = datCalc.datGetParam(work, 5);

                // If under 2 or flag nonsense, set Luc to 1.
                if (luc < 2 || (work.badstatus & 0xFFF) == 0x200)
                    { luc = 1; }

                // Adjust Luc by 6, then if Luc + 5 is > -1 (which it *should* be due to the above), Adjust Luc by 5.
                // Really I have no idea why this is here. It shows up in the actual formula.
                int luckValue = luc + 6;
                if (luc + 5 > -1)
                    { luckValue = luc + 5; }

                // Bitshift the above result by 1 and add 15.
                __result += luckValue >> 1 + 0xf;

                // If enable, scale it differently.
                if (EnableStatScaling)
                {
                    __result = work.level + (datCalc.datGetParam(work, 2) * 2 + datCalc.datGetParam(work, 4) * 2 + luckValue >> 1 + 0xf) / POINTS_PER_LEVEL;
                    
                    // If enabled, scale with Int.
                    if (EnableIntStat)
                        { __result = work.level + (datCalc.datGetParam(work, 1) * 2 + datCalc.datGetParam(work, 2) + datCalc.datGetParam(work, 4) + luckValue >> 1 + 0xf) / POINTS_PER_LEVEL; }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetDefPow))]
        private class PatchGetDefPow
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                // Result init.
                // Grab the user's Vit and Level and just multiply the result by 2.
                __result = (datCalc.datGetParam(work, 3) + work.level) * 2;

                // If enabled, do some actual math.
                if (EnableStatScaling)
                    { __result = (int)((float)datCalc.datGetParam(work, 3) * 2f / (float)POINTS_PER_LEVEL + work.level) * 2; }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbActionProcess), nameof(nbActionProcess.GetKoukaList))]
        private class PatchGetTargetList
        {
            private static bool Prefix(ref nbActionProcessData_t a, Il2CppReferenceArray<Il2CppStructArray<sbyte>> koukalist, uint select, int nskill)
            {
                // Effect List init.
                // We're gonna overwrite the other list with this new one after all the changes.
                // Keep in mind, this will only happen if the entire function goes through perfectly.
                Il2CppReferenceArray<Il2CppStructArray<sbyte>> effectlist = koukalist;

                // If the Skill doesn't target randomly or the Effect List is empty, then return.
                if (datNormalSkill.tbl[nskill].targetrandom < 1 || effectlist.Length == 0)
                    { return true; }

                // Loop through the Effect List and clear it.
                int i = 0;
                do
                {
                    // If this somehow happened, we've got a problem, so just return.
                    if (effectlist[i].Length < 2)
                        { return true; }
                    // Clear both indices.
                    // -1 means no target and the following zero sets the hitcount to, well, zero.
                    effectlist[i][0] = -1;
                    effectlist[i][1] = 0;
                    i++;
                }
                while (i < effectlist.Length);

                // Unseeded rng.
                System.Random rng = new();

                // Randomly set the hitcount.
                int hitcount = rng.Next(datNormalSkill.tbl[nskill].targetcntmin, datNormalSkill.tbl[nskill].targetcntmax);

                // Loop some other stuff.
                i = 0;
                do
                {
                    ulong ID = (uint)(1 << i & 0x1f);
                    // Bit math nonsense.
                    // If this results in not zero, do some checks.
                    if ((ID & select) != 0)
                    {
                        // If this particular demon is above the Party Length, return.
                        // The "party" variable is just a list of demons in combat.
                        // Note that this doesn't actively check for who's side you're on normally.
                        if (a.data.form[i].partyindex >= a.data.party.Length)
                            { return true; }

                        // Checks some more Bit flag nonsense.
                        if ((a.data.party[a.data.form[i].partyindex].flag >> 5 & 1) == 0)
                        {
                            // If this happens, we've got a problem, so return.
                            if (a.timelist.Length <= i)
                                { return true; }

                            // If that demon's HP is zero or whatever this format flag is says that demon's dead, continue the loop.
                            if (a.timelist[i].hp != 0 || (nbCalc.nbGetDevilFormatFlag(i) >> 8 & 1) != 0)
                                { i++; continue; }
                        }
                        
                        // I have no idea what this is doing. This shows up in the original function. Please don't ask.
                        select = (uint)(select & (ID ^ 0xffffffff));
                    }
                    i++;
                }
                while (i < 0xf);

                // Setting up a loop to fill the Effect List.
                int effectID = -1;
                i = 0;
                int j = 0;
                int enemycnt = 0;
                
                // Loop through the Hit Count.
                do
                {
                    // Loop through each Effect ID and make sure it has HP.
                    // Yes, I'm doing this at most 1000 times. Realistically it should never take that long.
                    j = 0;
                    do
                    {

                        // Randomly selected since we're actively randomly targeting.
                        effectID = rng.Next(0, a.timelist.Length - 1);

                        // If you're not on your target's team and their HP is over zero, break.
                        if ((
                            (a.data.form[effectID].formindex < 4 && a.form.formindex >= 4) ||
                            (a.data.form[effectID].formindex >= 4 && a.form.formindex < 4)) &&
                            a.timelist[effectID].hp > 0)
                            { break; }

                        // Otherwise, set it to a blank target.
                        else
                            { effectID = -1; }

                        // Increment.
                        j++;
                    }
                    while (j < 1000);

                    // This for loop checks if the demon in question already exists in the list.
                    bool found = false;
                    for (j = 0; j < a.timelist.Length; j++)
                    {

                        // If it does, increment their personal hit count.
                        if (effectlist[j][0] == (sbyte)a.data.form[effectID].formindex)
                        {
                            found = true;
                            effectlist[j][1]++;
                        }
                    }

                    // If they weren't found, add them to the Effect List and give them a hit count.
                    if (found == false)
                    {
                        effectlist[enemycnt][0] = (sbyte)a.data.form[effectID].formindex;
                        effectlist[enemycnt][1]++;
                        enemycnt++;
                    }

                    // Increment.
                    i++;
                }
                while (i < hitcount);

                // At this point we're good, so replace the original list.
                koukalist = effectlist;
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetBaseMaxHp))]
        private class PatchGetBaseMaxHP
        {
            public static int GetBaseMaxHP(datUnitWork_t work)
            {
                // Calculate the game's original Base HP value.
                int result = (datCalc.datGetBaseParam(work, 3) + work.level) * 6;

                // If you're leveling up, add those points as well.
                if (rstinit.GBWK != null)
                    { result += rstinit.GBWK.ParamOfs[3] * 6; }

                // If enabled, scale differently.
                if (EnableStatScaling)
                {
                    result = (int)((float)datCalc.datGetBaseParam(work, 3) / (float)POINTS_PER_LEVEL + (float)work.level) * 6;
                    if (rstinit.GBWK != null)
                        { result += (int)((float)rstinit.GBWK.ParamOfs[3] / (float)POINTS_PER_LEVEL) * 6; }
                }

                // Return the result.
                return result;
            }

            private static bool Prefix(ref int __result, datUnitWork_t work)
            {
                // Return the above function's value.
                __result = GetBaseMaxHP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetBaseMaxMp))]
        private class PatchGetBaseMaxMP
        {
            public static int GetBaseMaxMP(datUnitWork_t work)
            {
                // Calculate the game's original Base MP value.
                int result = (datCalc.datGetBaseParam(work, 2) + work.level) * 3;

                // If you're leveling up, add those points as well.
                if (rstinit.GBWK != null)
                    { result += rstinit.GBWK.ParamOfs[2] * 3; }

                // If enabled, scale differently.
                if (EnableStatScaling)
                {
                    result = datCalc.datGetBaseParam(work, 2) * 3 / POINTS_PER_LEVEL + work.level * 3;
                    if (rstinit.GBWK != null)
                        { result += rstinit.GBWK.ParamOfs[2] * 3 / POINTS_PER_LEVEL; }
                }

                // Return the result.
                return result;
            }

            private static bool Prefix(ref int __result, datUnitWork_t work)
            {
                // Similar to the HP function, just return the above function's result.
                __result = GetBaseMaxMP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetMaxHp))]
        private class PatchGetMaxHP
        {
            public static uint GetMaxHP(datUnitWork_t work)
            {
                // Grab the Base Max HP.
                uint result = (uint)PatchGetBaseMaxHP.GetBaseMaxHP(work);

                // Add a percentage of your Max HP to your Max HP with certain special Skills.
                float boost = 0.0f;
                boost += datCalc.datCheckSyojiSkill(work, 0x122) == 1 ? 0.1f : 0;
                boost += datCalc.datCheckSyojiSkill(work, 0x123) == 1 ? 0.2f : 0;
                boost += datCalc.datCheckSyojiSkill(work, 0x124) == 1 ? 0.3f : 0;

                // Clamp the result.
                result = Math.Clamp((uint)((float)result * boost), 1, MAXHPMP);
                return result;
            }

            private static bool Prefix(ref uint __result, datUnitWork_t work)
            {
                // Again, return the above function's result.
                __result = GetMaxHP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetMaxMp))]
        private class PatchGetMaxMP
        {
            public static uint GetMaxMP(datUnitWork_t work)
            {
                // Grab the Base Max MP.
                uint result = (uint)PatchGetBaseMaxMP.GetBaseMaxMP(work);

                // Like before, add percentages of it to it based on certain Skills.
                float boost = 0.0f;
                boost += datCalc.datCheckSyojiSkill(work, 0x125) == 1 ? 0.1f : 0;
                boost += datCalc.datCheckSyojiSkill(work, 0x126) == 1 ? 0.2f : 0;
                boost += datCalc.datCheckSyojiSkill(work, 0x127) == 1 ? 0.3f : 0;

                // Clamp.
                result = Math.Clamp((uint)((float)result * boost), 1, MAXHPMP);

                // Return.
                return result;
            }
            private static bool Prefix(ref uint __result, datUnitWork_t work)
            {
                // Grab and return.
                __result = GetMaxMP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstSetMaxHpMp))]
        private class PatchSetMaxHPMP
        {
            private static bool Prefix(sbyte Mode, ref datUnitWork_t pStock)
            {
                // Grab and set the demon's Max HP/MP.
                pStock.maxhp = (ushort)datCalc.datGetMaxHp(pStock);
                pStock.maxmp = (ushort)datCalc.datGetMaxMp(pStock);

                // If Mode is 1, heal the demon fully.
                if (Mode == 1)
                {
                    pStock.hp = pStock.maxhp;
                    pStock.mp = pStock.maxmp;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(rstCalcCore), nameof(rstCalcCore.cmbAddLevelUpParamEx))]
        private class PatchAddLevelUpParamEx
        {
            public static sbyte AddLevelUpParam(ref datUnitWork_t pStock, sbyte Mode)
            {
                // This is a list of Stats it needs to check.
                bool[] paramChecks = { false, false, false, false, false, false };

                // Change the list's values to true if that Stat is capped.
                for (int i = 0; i < paramChecks.Length; i++)
                {
                    if (pStock.param[i] + rstinit.GBWK.ParamOfs[i] >= MAXSTATS)
                        { paramChecks[i] = true; }
                }

                // Loop through the Stats.
                do
                {
                    // Grab a particular Stat ID.
                    int ctr = (int)(fclMisc.FCL_RAND() % paramChecks.Length);

                    // If it's capped, continue and do it again.
                    if (paramChecks[ctr] == true)
                        { continue; }

                    // If over zero and Int is disabled, make sure to skip Int.
                    if (ctr > 0 && !EnableIntStat)
                        { ctr++; }

                    // If you this somehow broke, break the loop.
                    if (rstinit.GBWK.ParamOfs.Length <= ctr)
                        { break; }

                    // Increment the LevelUp Stat.
                    rstinit.GBWK.ParamOfs[ctr]++;

                    // If the Stat is somehow zero, return 0x7f.
                    // This probably means it's capped but I really don't know what this is doing.
                    if (pStock.param[ctr] + rstinit.GBWK.ParamOfs[ctr] <= 0)
                        { return 0x7f; }

                    // Return the Stat ID.
                    return (sbyte)ctr;
                }
                while (true);

                // If you got to here, the function broke somehow, so just make sure it assigns nothing.
                return -1;
            }

            private static bool Prefix(out sbyte __result, ref datUnitWork_t pStock, sbyte Mode)
            {
                // Returns the previous function's value
                // Btw the "Mode" parameter did nothing in the original function from what I could tell.
                // It'll do nothing here as well.
                __result = AddLevelUpParam(ref pStock, Mode);
                return false;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstAutoAsignDevilParam))]
        private class PatchAutoAsignDevilParam
        {
            private static bool Prefix()
            {
                // Set the Evolution check to false.
                // This is specifically so that you don't get stuck in another menu later.
                EvoCheck = false;

                // Iterate a loop through LevelUp Stats and clear them.
                int i = 0;
                for (i = 0; i < rstinit.GBWK.ParamOfs.Length; i++)
                {
                    if (i == 1 && !EnableIntStat)
                        { continue; }
                    rstinit.GBWK.ParamOfs[i] = 0;
                }
                
                // Grab the current Stock demon.
                datUnitWork_t pStock = rstinit.GBWK.pCurrentStock;

                // Iterate and set the demon's new LevelUp Stats.
                i = 0;
                do
                {
                    // If you end up going over the Stat points per level, break.
                    if (rstinit.GBWK.AsignParam * POINTS_PER_LEVEL <= i)
                        { break; }

                    // Add a random stat to the demon.
                    var paramID = rstCalcCore.cmbAddLevelUpParamEx(ref pStock, 0);

                    // If the Stat's ID is over 5 or somehow hit -1, return.
                    if (paramID > 5 || paramID == -1)
                        { continue; }

                    // If you're exceeding your Maximum with the bonus, clamp it.
                    if (pStock.param[paramID] + rstinit.GBWK.ParamOfs[paramID] >= MAXSTATS)
                        { pStock.param[paramID] = MAXSTATS; }

                    // Otherwise, add to your Base Stats.
                    else
                        { pStock.param[paramID] += rstinit.GBWK.ParamOfs[paramID]; }

                    // Increment.
                    i++;
                }
                while (true);

                return false;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstCalcEvo))]
        private class PatchChkDevilEvo
        {
            // Literally just checks if the demon's evolving and set's a flag.
            // The game will take over as intended afterwards.
            private static bool Prefix()
                { EvoCheck = true; return true; }
        }

        [HarmonyPatch(typeof(cmpPanel), nameof(cmpPanel.cmpDrawDevilInfo))]
        private class PatchDrawDevilInfo
        {
            private static void Postfix(int X, int Y, uint Z, uint Col, sbyte SelFlag, sbyte DrawType, datUnitWork_t pStock, cmpCursorEff_t pEff, int FadeRate, GameObject obj, int MatCol)
            {
                // Set up a list of the demon's HP/MP. We'll be referencing this.
                int[] StockStats = new int[] { pStock.hp, pStock.mp };

                // For loop going through Game Objects.
                for (int i = 0; i < 2; i++)
                {
                    // Grabs whatever the base Game Object's name is, then checks if the current Stock Bar object is a child of it.
                    GameObject g2 = GameObject.Find(obj.name + "/" + StockBarValues[i]);
                    
                    // If it isn't, continue.
                    if (g2 == null)
                        { continue; }

                    // If it exists and its Counter's Image count is less than 4.
                    if (g2.GetComponent<CounterCtr>().image.Length < 4)
                    {
                        // Copy the first Image object.
                        GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtr>().image[0].gameObject);

                        // Set the new object's parent to the original's parent.
                        g.transform.parent = g2.transform;

                        // Change its position and scale to match the original object we copied.
                        g.transform.position = g2.GetComponent<CounterCtr>().image[0].transform.position;
                        g.transform.localPosition = g2.GetComponent<CounterCtr>().image[0].transform.localPosition;
                        g.transform.localScale = g2.GetComponent<CounterCtr>().image[0].transform.localScale;

                        // Append the object to the original object's Counter Image list.
                        g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                        
                        // Count through the Counter's Image list.
                        for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                        {
                            // Check if the object's active, then activate it anyway.
                            bool chk = g2.GetComponent<CounterCtr>().image[j].gameObject.active;
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = true;

                            // Set the position and scale to new values. These are very precise.
                            g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3(118 - j * 25, 31, -4);
                            g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtr>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtr>().image[j].transform.localScale.y, g2.GetComponent<CounterCtr>().image[j].transform.localScale.z);
                            
                            // Deactivate the object if it wasn't active.
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = chk;
                        }

                        // MAKE SURE this object doesn't get deleted.
                        GameObject.DontDestroyOnLoad(g);
                    }

                    // Set the object's color to white and set it to the proper Stat's value.
                    g2.GetComponent<CounterCtr>().Set(StockStats[i], Color.white, 0);
                }
            }
        }

        [HarmonyPatch(typeof(nbPanelProcess), nameof(nbPanelProcess.nbPanelPartyDraw))]
        private class PatchPanelPartyDraw
        {
            private static void Postfix()
            {
                // Iterate through your party's stats.
                for (int i = 0; i < 4; i++)
                {
                    // Grab the party object from the index.
                    nbParty_t party = nbMainProcess.nbGetPartyFromFormindex(i);

                    // If it's null, continue.
                    if (party == null)
                        { continue; }

                    // If i is over 0 and somehow this demon is in Demi-Fiend's slot, also continue.
                    // I probably shouldn't have this check, so I commented it out for reference.
                    // It existed in the normal function.
                    /*
                    if (i > 0 && party.statindex == 0)
                        { continue; }
                    */

                    // Grab the demon from the party's statindex.
                    datUnitWork_t pStock = dds3GlobalWork.DDS3_GBWK.unitwork[party.statindex];

                    // Grab their HP/MP for later.
                    int[] PartyStats = new int[] { pStock.hp, pStock.mp };

                    // Loops through the party's Bar objects
                    for (int k = 0; k < 2; k++)
                    {
                        // Grab the demon's Party Bar values.
                        GameObject g2 = GameObject.Find("bparty(Clone)/bparty_window0" + (i + 1) + "/" + PartyBarValues[k]);
                        
                        // If it doesn't exist, continue;
                        if (g2 == null)
                            { continue; }

                        // If the Counter's image count is less than 4.
                        if (g2.GetComponent<CounterCtrBattle>().image.Length < 4)
                        {
                            // Copy the first image.
                            GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtrBattle>().image[0].gameObject);
                            
                            // Set the parent to the Party Bar.
                            g.transform.parent = g2.transform;

                            // Set its position and scale to the original image's values.
                            g.transform.position = g2.GetComponent<CounterCtrBattle>().image[0].transform.position;
                            g.transform.localPosition = g2.GetComponent<CounterCtrBattle>().image[0].transform.localPosition;
                            g.transform.localScale = g2.GetComponent<CounterCtrBattle>().image[0].transform.localScale;

                            // Append the new image to the Party Bar's Counter Image List.
                            g2.GetComponent<CounterCtrBattle>().image = g2.GetComponent<CounterCtrBattle>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                            
                            // Iterate through the Images.
                            for (int j = 0; j < g2.GetComponent<CounterCtrBattle>().image.Length; j++)
                            {
                                // Save previous active state and set to active.
                                bool chk = g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active;
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = true;

                                // Set new position and scale. Again, very precise.
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localPosition = new Vector3(119 - j * 25, 0, -4);
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.y, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.z);
                                
                                // Set active state back.
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = chk;
                            }

                            // MAKE SURE it doesn't get destroyed.
                            GameObject.DontDestroyOnLoad(g);
                        }

                        // Set color to white and set it up with the demon's stats.
                        g2.GetComponent<CounterCtrBattle>().Set(PartyStats[k], Color.white, 0);
                    }
                }


                // This section iterates through the Battle Menu's Summon Selection.
                for (int i = 0; i < 3; i++)
                {
                    // Iterate through the Party Bars.
                    for (int k = 0; k < 2; k++)
                    {
                        // Grab the Party Bar.
                        GameObject g2 = GameObject.Find("summon_command/bmenu_command/bmenu_command_s0" + (i + 1) + "/" + PartyBarValues[k]);
                        if (g2 == null)
                            { continue; }

                        // If the Counter's image count is less than 4.
                        if (g2.GetComponent<CounterCtrBattle>().image.Length < 4)
                        {
                            // Copy the first image.
                            GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtrBattle>().image[0].gameObject);

                            // Set its parent to the Party Bar.
                            g.transform.parent = g2.transform;

                            // Set its position and scale to the original object's values.
                            g.transform.position = g2.GetComponent<CounterCtrBattle>().image[0].transform.position;
                            g.transform.localPosition = g2.GetComponent<CounterCtrBattle>().image[0].transform.localPosition;
                            g.transform.localScale = g2.GetComponent<CounterCtrBattle>().image[0].transform.localScale;

                            // Append the image to the Counters' image lists.
                            // I dunno why there's two of them, but there is.
                            // Nocturne's weird.
                            g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                            g2.GetComponent<CounterCtrBattle>().image = g2.GetComponent<CounterCtrBattle>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                            
                            // Iterate through the images.
                            for (int j = 0; j < g2.GetComponent<CounterCtrBattle>().image.Length; j++)
                            {
                                // Get the active state then set it to true.
                                bool chk = g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active;
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = true;

                                // Change the image's position and scale. You get the point by now. This happens a lot.
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localPosition = new Vector3(119 - j * 25, 0, -4);
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.y, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.z);
                                
                                // Set active to previous value.
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = chk;
                            }

                            // MAKE SURE it stays.
                            GameObject.DontDestroyOnLoad(g);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(nbPanelProcess), nameof(nbPanelProcess.nbPanelAnalyzeRun))]
        private class PatchPanelAnalyzeRun
        {
            private static void Postfix()
            {
                // Grab the unit you're analyzing
                datUnitWork_t unit = nbPanelProcess.pNbPanelAnalyzeUnitWork;

                // If it exists, which it should.
                // While I was testing the Random Target Multihits, it could end upnull, so thank god I put this here.
                if (unit != null)
                {
                    // Set up the Analyze Bars.
                    int[] AnalyzeStats = new int[] { unit.hp, unit.maxhp, unit.mp, unit.maxmp };

                    // These are the specific images we're copying.
                    string[] images = { "num_hp01", "num_hpfull01", "num_mp01", "num_mpfull01", };

                    // Iterate through the Analyze Bars and images.
                    for (int i = 0; i < 4; i++)
                    {
                        // Find the image.
                        GameObject g2 = GameObject.Find(AnalyzeBarValues[i / 2] + "/" + images[i]);

                        // If it doesn't exist, skip.
                        if (g2 == null)
                            { continue; }

                        // If it has no Counter, skip.
                        if (g2.GetComponent<CounterCtr>() == null)
                            { continue; }

                        // If it's Counter has less than 5 images.
                        if (g2.GetComponent<CounterCtr>().image.Length < 5)
                        {

                            // Grab the currently Length and iterate until 5.
                            for (int j = g2.GetComponent<CounterCtr>().image.Length; j < 5; j++)
                            {
                                // Copy the image.
                                GameObject g = GameObject.Instantiate(g2);

                                // Remove the Counter from the copy since we don't need duplicates.
                                GameObject.Destroy(g.GetComponent<CounterCtr>());

                                // Rename the image. I don't think this matters, but I'm doing it anyway.
                                g.name = images[i].Replace("1","") + (i + 1);

                                // Set the copy's parent to the original image's parent.
                                g.transform.parent = g2.transform.parent;

                                // Set the position and scale to the original's values.
                                g.transform.position = g2.GetComponent<CounterCtr>().transform.position;
                                g.transform.localPosition = g2.GetComponent<CounterCtr>().transform.localPosition;
                                g.transform.localScale = g2.GetComponent<CounterCtr>().transform.localScale;

                                // Append the copy to the Counter's image list.
                                g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                                
                                // MAKE SURE it doesn't die.
                                GameObject.DontDestroyOnLoad(g);
                            }

                            // Iterate through the image lists completely.
                            for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                            {
                                // Set position and scale accordingly.
                                g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3((i % 2) * 130 + 86 - j * 20 + 5, 32, -8);
                                g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(0.8f, 1, 1);
                            }
                        }

                        // Set Counter's value and color.
                        g2.GetComponent<CounterCtr>().Set(AnalyzeStats[i], Color.white, 0);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(rstupdate), nameof(rstupdate.rstResetAsignParam))]
        private class PatchResetAsignParam
        {
            private static bool Prefix()
            {
                // Just a call, nothing more.
                ResetParam();
                return false;
            }
            public static void ResetParam()
            {
                // Recalculates your LevelUp Points and their Maximum distribution.
                rstinit.GBWK.AsignParam = (short)(rstinit.GBWK.LevelUpCnt * POINTS_PER_LEVEL);
                rstinit.GBWK.AsignParamMax = (short)(rstinit.GBWK.LevelUpCnt * POINTS_PER_LEVEL);

                // Clears out whatever points you did distribute.
                for (int i = 0; i < rstinit.GBWK.ParamOfs.Length; i++)
                    { rstinit.GBWK.ParamOfs[i] = 0; }

                // I dunno what this does, but I'm guessing it just makes the Stat Point number visually glow.
                rstinit.SetPointAnime(rstinit.GBWK.AsignParam);
            }
        }

        [HarmonyPatch(typeof(rstupdate), nameof(rstupdate.rstUpdateSeqAsignPlayerParam))]
        private class PatchUpdateAsignPlayerParam
        {
            public static sbyte YesResponse()
            {
                // I shit you not, this number is EXTREMELY important.
                // Wihtout this, you can't leave the menu properly. I honestly have no idea why.
                rstinit.GBWK.SeqInfo.Current = 0x18;

                // Return that you said yes.
                return 1;
            }
            public static sbyte NoResponse()
            {
                // Return that you said no.
                return 0;
            }
            private static bool Prefix(ref datUnitWork_t pStock)
            {
                // If you're in a Message.
                if (fclMisc.fclChkMessage() == 2)
                {
                    // If you're at position zero and you hit the OK button.
                    if (fclMisc.fclGetSelMessagePos() == 0
                        && dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OK)
                        && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OK) == true)
                    {
                        // If you're in a Selection Message, then respond.
                        if (fclMisc.fclChkSelMessage() == 1)
                            { YesResponse(); }
                    }

                    // If you're at position 1 and you hit the OK button or you hit the Cancel button.
                    if ((fclMisc.fclGetSelMessagePos() == 1
                        && dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OK)
                        && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OK) == true) ||
                        (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL)
                        && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL) == true))
                    {
                        // If you're in a Selection Message, then respond.
                        if (fclMisc.fclChkSelMessage() == 1)
                            { NoResponse(); }
                    }

                    // This is so it doesn't continue while you're in a message.
                    return false;
                }

                // If you're not assigning your Stats, reset them once and start assigning them.
                // If you had the Evolution menu open previously, disable the check.
                if (SettingAsignParam == false)
                {
                    SettingAsignParam = true;
                    EvoCheck = false;
                    PatchResetAsignParam.ResetParam();
                }

                // If your LevelUp Count is 0 or less, stop assigning.
                if (rstinit.GBWK.LevelUpCnt <= 0)
                    { SettingAsignParam = false; }

                // If your stats are capped, immediately skip the entire process.
                if (EnableIntStat && pStock.param[0] + rstinit.GBWK.ParamOfs[0] >= MAXSTATS &&
                    pStock.param[1] + rstinit.GBWK.ParamOfs[1] >= MAXSTATS &&
                    pStock.param[2] + rstinit.GBWK.ParamOfs[2] >= MAXSTATS &&
                    pStock.param[3] + rstinit.GBWK.ParamOfs[3] >= MAXSTATS &&
                    pStock.param[4] + rstinit.GBWK.ParamOfs[4] >= MAXSTATS &&
                    pStock.param[5] + rstinit.GBWK.ParamOfs[5] >= MAXSTATS)
                    { YesResponse(); return false; }

                // Same thing as above, but without Int.
                else if (pStock.param[0] + rstinit.GBWK.ParamOfs[0] >= MAXSTATS &&
                    pStock.param[2] + rstinit.GBWK.ParamOfs[2] >= MAXSTATS &&
                    pStock.param[3] + rstinit.GBWK.ParamOfs[3] >= MAXSTATS &&
                    pStock.param[4] + rstinit.GBWK.ParamOfs[4] >= MAXSTATS &&
                    pStock.param[5] + rstinit.GBWK.ParamOfs[5] >= MAXSTATS)
                    { YesResponse(); return false; }

                // If the status object is null, immediately skip the entire process.
                if (cmpStatus.statusObj == null)
                    { YesResponse(); return false; }

                // If you're in a Message, skip the rest.
                if (fclMisc.fclChkMessage() != 0)
                    { return false; }

                // Grab the Cursor's index and adjust it accordingly.
                int cursorIndex = cmpMisc.cmpGetCursorIndex(rstinit.GBWK.ParamCursor);
                sbyte cursorParam = (sbyte)cursorIndex;

                // If Int is disabled, make sure it doesn't select it.
                if (!EnableIntStat)
                    { cursorParam = cmpMisc.cmpExchgParamIndex((sbyte)cursorIndex); }

                // Otherwise, make sure it can select it.
                else
                {
                    // If the Status Bar object count is under 6.
                    if (cmpStatus._statusUIScr.ObjStsBar.Length < 6)
                    {
                        // Find the 6th Status Bar.
                        // This will only exist with Int enabled, so don't worry about it.
                        GameObject g = GameObject.Find("statusUI(Clone)/sstatus/sstatusbar06");

                        // Append to the Status Bar arrays.
                        cmpStatus._statusUIScr.ObjStsBar = cmpStatus._statusUIScr.ObjStsBar.Append(g).ToArray();
                        cmpStatus._statusUIScr.ObjStatus = cmpStatus._statusUIScr.ObjStsBar.Append(g).ToArray();
                    }

                    // Increase the selection caps to 6.
                    rstinit.GBWK.ParamCursor.CursorPos.ShiftMax = 6;
                    rstinit.GBWK.ParamCursor.CursorPos.ListNums = 6;
                }

                // Set up the Status Screen and its cursor.
                cmpUpdate.cmpSetupObject(cmpStatus._statusUIScr.gameObject, true);
                cmpUpdate.cmpMenuCursor(cursorIndex, cmpStatus._statusUIScr.stsCursor, cmpStatus._statusUIScr.ObjStsBar);

                // Check if you pressed up. If you're holding it, delay the input a bit and then repeat it slowly.
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.U) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.U) == true)
                {
                    // Grab the cursor index.
                    cursorIndex = cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 0);

                    // If the cursor index is under the maximum, decrement it and play a sound.
                    if (EnableIntStat && cursorIndex < rstinit.GBWK.ParamCursor.CursorPos.ListNums)
                        { cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, -1); cmpMisc.cmpPlaySE(1 & 0xFFFF); }

                    // Otherwise just play a sound.
                    // This part never happens since the menu loops.
                    else
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); }

                    // Again, not sure what this is doing.
                    rstinit.SetPointAnime(cursorIndex);
                }

                // Check if you pressed down. Works the same as the pressing up calls, just incrementing instead.
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.D) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.D) == true)
                {
                    // Grab the cursor index.
                    cursorIndex = cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 0);

                    // If under the cap, increment and play a sound.
                    if (EnableIntStat && cursorIndex < rstinit.GBWK.ParamCursor.CursorPos.ListNums)
                        { cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 1); cmpMisc.cmpPlaySE(1 & 0xFFFF); }

                    // Otherwise just play a sound.
                    // Again, never happens.
                    else
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); }

                    // Does a thing.
                    rstinit.SetPointAnime(cursorIndex);
                }

                // If you press the cancel Button.
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL) == true)
                {
                    // Reset your stats and play a sound.
                    rstupdate.rstResetAsignParam();
                    cmpMisc.cmpPlaySE(2 & 0xFFFF);
                }

                // If you press the OK button.
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OK) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OK) == true)
                {
                    // If your Stat plus the LevelUp stats exceed or go up to the maximum, then play a sound and skip the rest of the function.
                    if (pStock.param[cursorParam] + rstinit.GBWK.ParamOfs[cursorParam] >= MAXSTATS)
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); return false; }

                    // If you still have points to assign, assing one.
                    if (rstinit.GBWK.AsignParam > 0)
                    {
                        rstinit.GBWK.ParamOfs[cursorParam]++;
                        rstinit.GBWK.AsignParam--;
                        cmpMisc.cmpPlaySE(1 & 0xFFFF);
                    }

                    // If you ran out, ask the player if they're okay with the changes.
                    if (rstinit.GBWK.AsignParam == 0)
                    {
                        // This message asks the player if they're good.
                        fclMisc.fclStartMessage(2);

                        // This asks the player for Yes/No as their response.
                        fclMisc.fclStartSelMessage(0x2b);
                        fclMisc.gSelMsgNo = 0x2b;
                    }
                }

                // If you press the Y/Triangle button.
                // This functionality is new.
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OPT1) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OPT1) == true)
                {
                    // If this stat has no points assigned to it, play a sound and skip.
                    if (rstinit.GBWK.ParamOfs[cursorParam] < 1)
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); return false; }

                    // Otherwise, remove unassign a point to redistribute and play a sound.
                    else
                    {
                        rstinit.GBWK.ParamOfs[cursorParam]--;
                        rstinit.GBWK.AsignParam++;
                        cmpMisc.cmpPlaySE(1 & 0xFFFF);
                    }
                }

                // Recalculate HP/MP.
                rstcalc.rstSetMaxHpMp(0, ref pStock);
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpMisc), nameof(cmpMisc.cmpGetParamName))]
        private class PatchGetParamName
        {
            private static bool Prefix(out string __result, sbyte Index)
            {
                // Grab the Stat's Name from a clamped index.
                Index = (sbyte)Math.Clamp((int)Index, 0, 5);
                __result = paramNames[Index];
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpDrawDH), nameof(cmpDrawDH.cmpDrawHeartsInfo))]
        private class PatchDrawMagatamaInfo
        {
            private static bool Prefix(cmpHeartsInfo_t pHeartsInfo, sbyte HeartsID)
            {
                // If you're not using the Int stat, just skip this entire function.
                if (!EnableIntStat)
                    { return true; }

                // This is for the various loops used within this function.
                int i = 0;

                // Grab the parent object of the entire Magatama Status menu.
                GameObject g = GameObject.Find("magUI(Clone)/magstatus");

                // If null, return.
                if (g == null)
                    { return false; }

                // If it's inactive, return.
                if (g.activeSelf == false)
                    { return false; }

                // If there's no 6th base.
                if(!GameObject.Find("magUI(Clone)/magstatus/magstatus_base06"))
                {
                    // Find the first one.
                    GameObject orig = GameObject.Find("magUI(Clone)/magstatus/magstatus_base01");

                    // Copy it.
                    GameObject g2 = GameObject.Instantiate(orig);

                    // MAKE SURE it stays put.
                    GameObject.DontDestroyOnLoad(g2);

                    // Rename it.
                    g2.name = "magstatus_base06";

                    // Set its parent to the whole menu.
                    g2.transform.parent = g.transform;

                    // Adjust position and scale.
                    g2.transform.position = orig.transform.position;
                    g2.transform.localScale = orig.transform.localScale;
                    
                    // Make sure its local position compared to the parent is correct, since it's gonna get multiplied a bit.
                    g2.transform.localPosition = new(orig.transform.localPosition.x, orig.transform.localPosition.y - 56 * 5, orig.transform.localPosition.z);

                    // Increment through all of the base bars.
                    for (i = 0; i < 6; i++)
                    {
                        // Grab the base bar
                        GameObject g3 = GameObject.Find("magUI(Clone)/magstatus/magstatus_base0" + (i + 1));

                        // If it doesn't exist, continue;
                        if (g3 == null)
                            { continue; }

                        // Grab position and scale.
                        Vector3 newScale = g3.transform.localScale;
                        Vector3 newPos = g3.transform.localPosition;

                        // Multiply position and scale.
                        newPos.x *= 1;
                        newPos.y *= 0.825f;
                        newPos.x *= 1;
                        newScale.x *= 0.825f;
                        newScale.y *= 0.825f;
                        newScale.z *= 1;

                        // Set new position and scale.
                        g3.transform.localScale = newScale;
                        g3.transform.localPosition = newPos;
                    }
                }

                // If there's no 6th item.
                if (!GameObject.Find("magUI(Clone)/magstatus/magstatus_item06"))
                {
                    // Grab the first one.
                    GameObject orig = GameObject.Find("magUI(Clone)/magstatus/magstatus_item01");

                    // Copy it.
                    GameObject g2 = GameObject.Instantiate(orig);

                    // MAKE SURE it stays put.
                    GameObject.DontDestroyOnLoad(g2.gameObject);

                    // Rename it.
                    g2.name = "magstatus_item06";

                    // Set parent.
                    g2.transform.parent = g.transform;

                    // Set position and scale.
                    g2.transform.position = orig.transform.position;
                    g2.transform.localScale = orig.transform.localScale;

                    // Make sure it's in the right spot before scaling.
                    g2.transform.localPosition = new(orig.transform.localPosition.x, orig.transform.localPosition.y - 56 * 5, orig.transform.localPosition.z);
                    
                    // Iterate through each item.
                    for (i = 0; i < 6; i++)
                    {
                        // Grab item.
                        GameObject g3 = GameObject.Find("magUI(Clone)/magstatus/magstatus_item0" + (i + 1));

                        // If null, continue.
                        if (g3 == null)
                            { continue; }

                        // Grab position and scale.
                        Vector3 newScale = g3.transform.localScale;
                        Vector3 newPos = g3.transform.localPosition;

                        // Multiply them.
                        newPos.x *= 1;
                        newPos.y *= 0.825f;
                        newPos.x *= 1;
                        newScale.x *= 0.825f;
                        newScale.y *= 0.825f;
                        newScale.z *= 1;

                        // Set them to new values.
                        g3.transform.localScale = newScale;
                        g3.transform.localPosition = newPos;
                    }
                }

                // Draw some various things to the screen.
                fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, new(4), 0xb, 0, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, new(4), 0xb, 1, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);

                // Grab a color.
                uint color = fclMisc.fclGetBlendColor(0x80808080,0x40404080,(uint)pHeartsInfo.Timer);

                // Set a list to that color.
                uint[] colorptr = { color, color, color, color };

                // Iterate until i is 6.
                i = 0;
                do
                {
                    // Unknown value.
                    int unk = 0;

                    // If i goes over the list of Stat Names, break.
                    if (paramNames.Length < i)
                        { break; }

                    // Grab Magatama Item object.
                    g = GameObject.Find("magUI(Clone)/magstatus/magstatus_item0" + (i+1));

                    // If null, increment and continue.
                    if (g == null)
                        { i++; continue; }

                    // Set up the previous object.
                    cmpUpdate.cmpSetupObject(g, true);

                    // Grab the text object from the previous object.
                    GameObject g2 = GameObject.Find("magUI(Clone)/magstatus/" + g.name + "/TextTM");

                    // If null, increment and continue.
                    if (g2 == null)
                        { i++; continue; }

                    // Set the text object's text.
                    g2.GetComponentInChildren<TMP_Text>().SetText(Localize.GetLocalizeText(cmpMisc.cmpGetParamName((sbyte)i)));

                    // Draw more parts to the screen.
                    fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, colorptr, 0xb, 3, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                    fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, colorptr, 0xb, (ushort)(i + 4), cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);

                    // Set up the text object.
                    cmpUpdate.cmpSetupObject(g2, true);

                    // Grab the numerical text object.
                    g2 = GameObject.Find("magUI(Clone)/magstatus/" + g.name + "/magtex");

                    // If null, increment and continue.
                    if (g2 == null)
                        { i++; continue; }

                    // Grab the Magatama Stat with ID i.
                    int heartParam = rstCalcCore.cmbGetHeartsParam(HeartsID, (sbyte)i);

                    // If it's 0, set the unknown value to 11.
                    if (heartParam == 0)
                        { unk = 0xb; }

                    // Otherwise set it to 9.
                    // If you somehow get the Stat to be negative, set it to 10 instead.
                    else
                    {
                        unk = 9;
                        if (heartParam < 1)
                            { unk = 10; }
                    }

                    // More draw calls.
                    fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, colorptr, 0xb, (ushort)unk, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);

                    // Set up the numerical text object.
                    cmpUpdate.cmpSetupObject(g2, true);

                    // Grab the number object
                    g2 = GameObject.Find("magUI(Clone)/magstatus/" + g.name + "/magtex/num_mag");

                    // If the object's Counter is null, return.
                    if (g2.GetComponent<CounterCtr>() == null)
                        { i++; continue; }

                    // Generate a color and set the Counter's value and color.
                    Color rgb = cmpInit.GetToRGBA(heartParam > 0 ? 0xFFFFFFFF : heartParam < 0 ? 0xFF8080FF : 0x80808080);
                    g2.GetComponent<CounterCtr>().Set(heartParam, rgb);

                    // Set up the number object.
                    cmpUpdate.cmpSetupObject(g2, true);
                    i++;
                }
                while (i < 6);

                // Draw the Magatama Help panel
                cmpDrawDH.cmpDrawHeartsHelpPanel(pHeartsInfo.Timer);

                // Draw the Magatama's name.
                cmpDrawDH.cmpDrawHeartsName(0, 0, 0, pHeartsInfo.Timer, HeartsID);

                // Grab the Magamama Menu object.
                g = cmpInitDH.DHeartsObj;

                // If it's null, don't set it up.
                if (g == null)
                    { return false; }

                // Otherwise, set it up and draw a gradient of some sort.
                cmpUpdate.cmpSetupObject(g, true);
                cmpDrawDH.cmpDrawDisactiveGrad(pHeartsInfo.Timer);
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpDrawStatus), nameof(cmpDrawStatus.cmpDrawParamPanel))]
        private class PatchDrawParamPanel
        {
            private static void CreateParamGauge(sbyte ctr2, int X, int Y, uint[] pBaseCol, datUnitWork_t pStock, sbyte CursorPos, sbyte CursorMode, sbyte FlashMode)
            {
                // If the Stat supercedes the demon's stat list, the base status object doesn't exist, or the Base Color length is 0, return.
                if (ctr2 > pStock.param.Length || pBaseCol.Length == 0 || cmpStatus.statusObj == null)
                    { return; }

                // If the Stat is capped, make sure it doesn't overshoot.
                if (pStock.param[ctr2] >= MAXSTATS)
                    { pStock.param[ctr2] = MAXSTATS; }

                // Grab the Status Menu object.
                GameObject stsObj = GameObject.Find("statusUI(Clone)/sstatus");

                // If there's text objects in the Status Menu's children, set up the Stat Names.
                if (stsObj.GetComponentsInChildren<TMP_Text>() != null)
                    { stsObj.GetComponentsInChildren<TMP_Text>()[(ctr2 > 1 && !EnableIntStat) ? ctr2 - 1 : ctr2].SetText(Localize.GetLocalizeText(cmpMisc.cmpGetParamName(ctr2))); }
                // If there's Counter objects in the Status Menu's children, set up their values and colors.
                if (stsObj.GetComponentsInChildren<CounterCtr>() != null)
                    { stsObj.GetComponentsInChildren<CounterCtr>()[(ctr2 > 1 && !EnableIntStat) ? ctr2 -1 : ctr2].Set(pStock.param[ctr2], Color.white, (CursorMode == 2 && CursorPos > -1) ? 1 : 0); }
                
                // If your Cursor Position is over -1, set the FlashMode to 2.
                // Not sure what this does.
                if (-1 < CursorPos)
                    { FlashMode = 2; }

                // Rework the entire Stat Bar.
                // I have no choice but to use a custom call here. For some reason using the original prefix crashes.
                PatchDrawParamGauge.ReworkParamGauge(pBaseCol, 0x14, (sbyte)ctr2, (sbyte)ctr2, FlashMode, pStock, stsObj);
            }
            private static bool Prefix(int X, int Y, uint[] pBaseCol, sbyte[] pParamOfs, datUnitWork_t pStock, sbyte CursorPos, sbyte CursorMode, sbyte FlashMode)
            {
                // If the Base status object is null, return.
                if (cmpStatus.statusObj == null)
                    { return false; }

                // If there's no demon, return.
                if (pStock == null)
                    { return false; }

                // Set up the demon's HP/MP values for reference.
                int[] StatusStats = new int[] { pStock.hp, pStock.maxhp, pStock.mp, pStock.maxmp };

                // Grab the Status Menu object.
                GameObject stsObj = GameObject.Find("statusUI(Clone)/sstatus");

                // If this is null, return.
                if (stsObj == null)
                    { return false; }

                // If it's inactive, return.
                if (stsObj.activeSelf == false)
                    { return false; }

                // If you have a Status Bar cursor and Int is enabled, scale the cursor down.
                if (GameObject.Find(stsObj.name + "/sstatusbar_cursur") && EnableIntStat)
                {
                    Vector3 newScale = new(1, 0.9f, 1);
                    GameObject.Find(stsObj.name + "/sstatusbar_cursur").transform.localScale = newScale;
                }

                // If there's no 6th Status Bar and Int is enabled.
                if (!GameObject.Find(stsObj.name + "/sstatusbar06") && EnableIntStat)
                {
                    // Grab the first bar.
                    GameObject g2 = GameObject.Find(stsObj.name + "/sstatusbar01");

                    // If it exists.
                    if (g2 != null)
                    {
                        // Duplicate it.
                        GameObject g = GameObject.Instantiate(g2);

                        // MAKE SURE the new one stays put.
                        GameObject.DontDestroyOnLoad(g);

                        // Rename.
                        g.name = "sstatusbar06";

                        // Set parent.
                        g.transform.parent = g2.transform.parent;

                        // Set position and scale.
                        g.transform.position = g2.transform.position;
                        g.transform.localPosition = new Vector3(g2.transform.localPosition.x, g2.transform.localPosition.y - 48 * 5, g2.transform.localPosition.z);
                        g.transform.localScale = g2.transform.localScale;

                        // Iterate through all bars.
                        for (int i = 0; i < 6; i++)
                        {
                            // Grab.
                            GameObject g3 = GameObject.Find("sstatusbar0" + (i + 1));

                            // If null, continue;
                            if (g3 == null)
                                { continue; }

                            // Get position and scale.
                            Vector3 newScale = g3.transform.localScale;
                            Vector3 newPos = g3.transform.localPosition;

                            // Adjust.
                            newPos.x *= 1;
                            newPos.y *= 0.9f;
                            newPos.x *= 1;
                            newScale.x *= 1;
                            newScale.y *= 0.9f;
                            newScale.z *= 1;

                            // Set to new values.
                            g3.transform.localScale = newScale;
                            g3.transform.localPosition = newPos;
                        }
                    }
                }

                // If there's no 6th Stat Bar number and Int is enabled.
                if (!GameObject.Find(stsObj.name + "/sstatusnum06") && EnableIntStat)
                {
                    // Get the first one.
                    GameObject g2 = GameObject.Find(stsObj.name + "/sstatusnum01");

                    // If it exists.
                    if (g2 != null)
                    {
                        // Copy it.
                        GameObject g = GameObject.Instantiate(g2);

                        // MAKE SURE it stays put.
                        GameObject.DontDestroyOnLoad(g);

                        // Rename.
                        g.name = "sstatusnum06";

                        // Set parent.
                        g.transform.parent = g2.transform.parent;

                        // Set position and scale.
                        g.transform.position = g2.transform.position;
                        g.transform.localPosition = new Vector3(g2.transform.localPosition.x, g2.transform.localPosition.y - 48 * 5, g2.transform.localPosition.z);
                        g.transform.localScale = g2.transform.localScale;
                        
                        // Iterate.
                        for (int i = 0; i < 6; i++)
                        {
                            // Grab.
                            GameObject g3 = GameObject.Find("sstatusnum0" + (i + 1));

                            // If null, continue.
                            if (g3 == null)
                                { continue; }

                            // Get position and scale.
                            Vector3 newScale = g3.transform.localScale;
                            Vector3 newPos = g3.transform.localPosition;

                            // Adjust.
                            newPos.x *= 1;
                            newPos.y *= 0.9f;
                            newPos.x *= 1;
                            newScale.x *= 1;
                            newScale.y *= 0.9f;
                            newScale.z *= 1;

                            // Set to new values.
                            g3.transform.localScale = newScale;
                            g3.transform.localPosition = newPos;
                        }
                    }
                }

                // If there's no 6th Stat Name object and Int is enabled.
                if (!GameObject.Find(stsObj.name + "/Text_stat06TM") && EnableIntStat)
                {
                    // Grab.
                    GameObject g2 = GameObject.Find(stsObj.name + "/Text_stat01TM");

                    // If it exists.
                    if (g2 != null)
                    {
                        // Copy.
                        GameObject g = GameObject.Instantiate(g2);

                        // Keep it.
                        GameObject.DontDestroyOnLoad(g);

                        // Rename.
                        g.name = "Text_stat06TM";

                        // Parent.
                        g.transform.parent = g2.transform.parent;

                        // Pos and Scale.
                        g.transform.position = g2.transform.position;
                        g.transform.localPosition = new Vector3(g2.transform.localPosition.x, g2.transform.localPosition.y - 48 * 5, g2.transform.localPosition.z);
                        g.transform.localScale = g2.transform.localScale;

                        // Iterate.
                        for (int i = 0; i < 6; i++)
                        {
                            // Grab.
                            GameObject g3 = GameObject.Find("Text_stat0" + (i + 1) + "TM");

                            // If null, continue.
                            if (g3 == null)
                                { continue; }

                            // Grab Pos and scale.
                            Vector3 newScale = g3.transform.localScale;
                            Vector3 newPos = g3.transform.localPosition;

                            // Adjust
                            newPos.x *= 1;
                            newPos.y *= 0.9f;
                            newPos.x *= 1;
                            newScale.x *= 1;
                            newScale.y *= 0.9f;
                            newScale.z *= 1;

                            // Set to new.
                            g3.transform.localScale = newScale;
                            g3.transform.localPosition = newPos;
                        }
                    }
                }

                // Check all of the Status Bar Values.
                for (int i = 0; i < 4; i++)
                {
                    // Grab.
                    GameObject g2 = GameObject.Find(stsObj.name + "/" + StatusBarValues[i]);

                    // If null, continue.
                    if (g2 == null)
                        { continue; }

                    // If its Counter has less than 4 images.
                    if (g2.GetComponent<CounterCtr>().image.Length < 4)
                    {
                        // Copy.
                        GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtr>().image[0].gameObject);
                        
                        // Set parent.
                        g.transform.parent = g2.transform;

                        // Set pos and scale.
                        g.transform.position = g2.GetComponent<CounterCtr>().image[0].transform.position;
                        g.transform.localPosition = g2.GetComponent<CounterCtr>().image[0].transform.localPosition;
                        g.transform.localScale = g2.GetComponent<CounterCtr>().image[0].transform.localScale;

                        // Append to Counter's image list.
                        g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                        
                        // Iterate.
                        for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                        {
                            // Grab active state then set active.
                            bool chk = g2.GetComponent<CounterCtr>().image[j].gameObject.active;
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = true;

                            // Set new pos and scale/
                            g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3(60 - j * 25, 0, -4);
                            g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtr>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtr>().image[j].transform.localScale.y, g2.GetComponent<CounterCtr>().image[j].transform.localScale.z);
                            
                            // Reset active state.
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = chk;
                        }
                        // Keep it.
                        GameObject.DontDestroyOnLoad(g);
                    }
                    // Set Stat value and color.
                    g2.GetComponent<CounterCtr>().Set(StatusStats[i], Color.white, 0);
                }

                // Check the bar count.
                int bars = 5 + (EnableIntStat ? 1 : 0);

                // Iterate through all bars.
                for (int i = 0; i < bars; i++)
                {
                    // Grab the bar.
                    GameObject g2 = GameObject.Find(stsObj.name + "/sstatusnum0" + (i+1));

                    // If null, continue.
                    if (g2 == null)
                        { continue; }

                    // If inactive, continue.
                    if (g2.activeSelf == false)
                        { continue; }

                    // If the bar's Counter has less than 3 images.
                    if (g2.GetComponent<CounterCtr>().image.Length < 3)
                    {
                        // Copy it.
                        GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtr>().image[0].gameObject);

                        // Set parent.
                        g.transform.parent = g2.transform;

                        // Set pos and scale.
                        g.transform.position = g2.GetComponent<CounterCtr>().image[0].transform.position;
                        g.transform.localPosition = g2.GetComponent<CounterCtr>().image[0].transform.localPosition;
                        g.transform.localScale = g2.GetComponent<CounterCtr>().image[0].transform.localScale;

                        // Append.
                        g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                        
                        // Iterate.
                        for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                        {
                            // Grab active, then activate.
                            bool chk = g2.GetComponent<CounterCtr>().image[j].gameObject.active;
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = true;

                            // Set pos and scale.
                            g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3(30 - j * 25 + 5, 0, -4);
                            g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtr>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtr>().image[j].transform.localScale.y, g2.GetComponent<CounterCtr>().image[j].transform.localScale.z);
                            
                            // Reset active.
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = chk;
                        }
                        // Keep.
                        GameObject.DontDestroyOnLoad(g);
                    }
                    // Grab Stat ID.
                    int stat = (i > 0 && !EnableIntStat) ? i + 1 : i;

                    // If the object's not null and you're not evolving, set the LevelUp stat value.
                    int levelstat = 0;
                    if (rstinit.GBWK != null && !EvoCheck)
                        { levelstat = rstinit.GBWK.ParamOfs[stat]; }

                    // Set Stat value and color.
                    g2.GetComponent<CounterCtr>().Set(rstCalcCore.cmbGetParam(pStock, stat) + levelstat, Color.white, 0);
                }

                // If the Status Bar UI components don't exist, return.
                if (stsObj.GetComponentsInChildren<sstatusbarUI>() == null)
                    { return false; }

                // Iterate through the ui.
                for (int i = 0; i < 6; i++)
                {
                    // If Int is disabled, continue.
                    if (i == 1 && !EnableIntStat)
                        { continue; }

                    // If this particular Status UI bar doesn't exist, continue.
                    if (stsObj.GetComponentsInChildren<sstatusbarUI>()[(i > 0 && !EnableIntStat) ? i-1 : i] == null)
                        { continue; }

                    // If it's disabled, continue.
                    if (!stsObj.GetComponentsInChildren<sstatusbarUI>()[(i > 0 && !EnableIntStat) ? i-1 : i].gameObject.activeSelf)
                        { continue; }

                    // Create the Stat Bar.
                    CreateParamGauge((sbyte)i, (int)(X * 3.75), (int)(Y * 2.25), pBaseCol, pStock, CursorPos, CursorMode, FlashMode);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpDrawStatus), nameof(cmpDrawStatus.cmpDrawParamGauge))]
        private class PatchDrawParamGauge
        {
            // This should NEVER get called, but if it ever does this is here as a back up.
            private static bool Prefix(int X, int Y, uint[] pBaseCol, int StepY, sbyte Pos, sbyte ParamOfs, sbyte FlashMode, datUnitWork_t pStock, GameObject stsObj)
            {
                // If no demon or status object, return.
                if (pStock == null || stsObj == null)
                    { return false; }
                // Rework the Stat Bar.
                ReworkParamGauge(pBaseCol, StepY, Pos, ParamOfs, FlashMode, pStock, stsObj);
                return false;
            }
            public static void ReworkParamGauge(uint[] pBaseCol, int StepY, sbyte Pos, sbyte ParamOfs, sbyte FlashMode, datUnitWork_t pStock, GameObject stsObj)
            {
                // If the Status Bar UI count is wrong, return.
                if (stsObj.GetComponentsInChildren<sstatusbarUI>().Length < 5 + (EnableIntStat ? 1 : 0))
                    { return; }

                // If the Stat is over 1 and Int doesn't exist, decrement.
                int stat = ParamOfs;
                if (stat > 0 && !EnableIntStat)
                    { stat--; }

                // If the number of Animator objects within the Stat Bar of a particular Stat isn't equal to the maximum stat limit.
                if (stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length != MAXSTATS)
                {
                    // This gets set within the while loops.
                    GameObject g;

                    // While the count is under, create new objects.
                    while (stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length < MAXSTATS)
                    {
                        // Copy the first one.
                        g = GameObject.Instantiate(stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Animator>().gameObject);
                        
                        // Set Parent.
                        g.transform.parent = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.transform;

                        // Set Pos and Scale.
                        g.transform.position = g.transform.parent.position;
                        g.transform.localPosition = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Animator>().gameObject.transform.localPosition;
                        g.transform.localScale = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Animator>().gameObject.transform.localScale;
                    }

                    // While the count is over, destroy the extra objects.
                    while (stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length > MAXSTATS)
                        { GameObject.Destroy(stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length - 1].gameObject); }
                    
                    // Iterate through the Animator list.
                    for (int len = MAXSTATS - 1; len >= 0; len--)
                    {
                        // Grab pos and scale.
                        Vector3 barScale = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localScale;
                        Vector3 barPos = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localPosition;
                        
                        // Adjust.
                        barScale.x = BAR_SCALE_X;
                        barPos.x = 250 + (len) * BAR_SEGMENT_X + (18 - BAR_SEGMENT_X) + 2 / BAR_SCALE_X;
                        
                        // Set new.
                        stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localScale = barScale;
                        stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localPosition = barPos;
                    }

                    // If the background bar is null, load it from the Asset Bundle.
                    // This is a custom AssetBundle you have to manually put in the right place.
                    if (barData == null)
                        { barData = AssetBundle.LoadFromFile(AppContext.BaseDirectory + BundlePath + barSpriteName); AssetBundle.DontDestroyOnLoad(barData); }
                    
                    // If it's no longer null.
                    if (barData != null)
                    {
                        // Load its Texture2D.
                        barAsset[stat] = barData.LoadAsset(barSpriteName).Cast<Texture2D>();

                        // Keep it.
                        Texture2D.DontDestroyOnLoad(barAsset[stat]);

                        // Create a Sprite from the Texture2D and make sure to apply the Texture.
                        barSprite[stat] = Sprite.Create(barAsset[stat], new Rect(0, 0, barAsset[stat].width, barAsset[stat].height), Vector2.zero);
                        barSprite[stat].texture.Apply();

                        // Keep it.
                        Sprite.DontDestroyOnLoad(barSprite[stat]);

                        // Set the Stat Bar's sprite.
                        stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Image>().sprite = barSprite[stat];
                    }
                }

                // Magatama Value.
                int heartValue = 0;
                if ((pStock.flag >> 2 & 1) == 0)
                    { heartValue = 0; }
                else
                    { heartValue = rstCalcCore.cmbGetHeartsParam((sbyte)dds3GlobalWork.DDS3_GBWK.heartsequip, ParamOfs); }

                // Stat Value
                int paramValue = pStock.param[ParamOfs];

                // Mitama Bonus Value.
                int mitamaValue = pStock.mitamaparam[ParamOfs];

                // LevelUp Value.
                int levelupValue = 0;
                if (rstinit.GBWK != null && !EvoCheck)
                    { levelupValue = rstinit.GBWK.ParamOfs[ParamOfs]; }

                // Iterate through all of the Stat Points of a particular stat.
                for (int ctr = 0; ctr < paramValue + levelupValue + mitamaValue; ctr++)
                {

                    // If it's over the maximum, break.
                    if (MAXSTATS <= ctr)
                        { break; }

                    // Set color ID to 3 (Blue) for Magatama Values.
                    int segmentColor = 3;

                    // Keep Color ID at 3 for Mitama Values.
                    if (paramValue + levelupValue + mitamaValue - heartValue > ctr)
                        { segmentColor = 3; }

                    // Set Color ID to 2 (Red, Glow) for LevelUp Values.
                    if (paramValue + levelupValue - heartValue > ctr)
                        { segmentColor = 2; }

                    // Set Color ID to 1 (Red) for Base Values. 
                    if (paramValue - heartValue > ctr)
                        { segmentColor = 1; }

                    // Set the Animator's color.
                    stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[ctr].SetInteger("sstatusbar_color", segmentColor);
                }

                // Set the Color to 0 (Off) for any remaining Stat Points.
                for(int ctr = paramValue + levelupValue + mitamaValue; ctr < MAXSTATS; ctr++)
                    { stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[ctr].SetInteger("sstatusbar_color", 0); }

                // If Stat Position is greater than or equal to Blink que length, cap it.
                if (Pos >= cmpDrawStatus.gStatusBlinkQue.Length)
                    { Pos = (sbyte)(cmpDrawStatus.gStatusBlinkQue.Length - 1); }

                // If FlashMode is 0 and the Blink que value is not zero, set FlashMode to 2.
                if (FlashMode == 0)
                {
                    if (cmpDrawStatus.gStatusBlinkQue[Pos] != 0)
                        {FlashMode = 2;}
                }

                // If FlashMode is 1 or 2, set the Blink color to FlashMode.
                if (FlashMode == 1 || FlashMode == 2)
                    { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], (sbyte)FlashMode, pCol); }

                // If FlashMode is 3.
                if (FlashMode == 3)
                {
                    // If the Blink que value is not 0, set the Blink color to 0.
                    if (cmpDrawStatus.gStatusBlinkQue[Pos] != 0)
                        { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], 0, pCol); }
                }

                // If Flash Mode is 4 or 5, set the Blink color to FlashMode.
                if (FlashMode == 4 || FlashMode == 5)
                    { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], FlashMode, pCol); }
                return;
            }
        }
    }
}