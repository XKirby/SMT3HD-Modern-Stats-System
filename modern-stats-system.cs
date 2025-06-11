using HarmonyLib;
using Il2Cpp;
using Il2Cppcamp_H;
using Il2Cppeffect_H;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppnewbattle_H;
using Il2Cppnewdata_H;
using Il2CppTMPro;
using MelonLoader;
using System.Collections;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.Arm;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(ModernStatsSystem.ModernStatsSystem), "Modern Stats System", "1.0.0", "X Kirby")]
[assembly: MelonGame("アトラス", "smt3hd")]

namespace ModernStatsSystem
{
    public class ModernStatsSystem : MelonMod
    {
        private const int MAXSTATS = 100;
        private const int MAXHPMP = 9999;
        private const int POINTS_PER_LEVEL = 2;
        private const bool EnableIntStat = true;
        private const bool EnableStatScaling = true;
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
        private static bool SettingAsignParam;
        private static bool EvoCheck;

        public override void OnInitializeMelon()
        {
            System.Random rng = new(5318008); // lol I had to. For real btw, I didn't want Int to be completely random for every demon on-load.
            for (int i = 0; i < datDevilFormat.tbl.Length; i++)
            {
                int sign;
                do
                    { sign = rng.Next(-1, 1); }
                while (sign == 0);
                if (EnableIntStat)
                    {datDevilFormat.tbl[i].param[1] = (sbyte)Math.Clamp(datDevilFormat.tbl[i].param[2] + datDevilFormat.tbl[i].param[2] * sign * 10 / 100, 1, MAXSTATS);}
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
            for (int i = 0; i < tblHearts.fclHeartsTbl.Length; i++)
            {
                int sign;
                if (EnableIntStat)
                {
                    do
                        { sign = rng.Next(-1, 1); }
                    while (sign == 0);
                    tblHearts.fclHeartsTbl[i].GrowParamTbl[1] = (sbyte)Math.Clamp(tblHearts.fclHeartsTbl[i].GrowParamTbl[2] + tblHearts.fclHeartsTbl[i].GrowParamTbl[2] * sign * 10 / 100, 1, MAXSTATS);
                    do
                        { sign = rng.Next(-1, 1); }
                    while (sign == 0);
                    tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[1] = (sbyte)Math.Clamp(tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[2] + tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[2] * sign * 10 / 100, 1, MAXSTATS);
                }
                if (EnableStatScaling)
                {
                    for (int j = 0; j < tblHearts.fclHeartsTbl[i].GrowParamTbl.Length; j++)
                    {
                        tblHearts.fclHeartsTbl[i].GrowParamTbl[j] *= POINTS_PER_LEVEL;
                        tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[j] *= POINTS_PER_LEVEL;
                    }
                }
            }
            if (EnableIntStat)
            {
                fclCombineTable.fclSpiritParamUpTbl[0].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(3 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[1].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[2].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[3].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(2 + 1).ToArray();
            }
            LoggerInstance.Msg("Modern Stats System Initialized.");
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstChkParamLimitAll))]
        private class PatchChkParamLimitAll
        {
            private static bool Prefix(ref int __result, datUnitWork_t pStock, bool paramSet = true)
            {
                __result = 0;
                if (PatchGetBaseParam.GetParam(pStock, 0) >= MAXSTATS)
                {
                    if (EnableIntStat && PatchGetBaseParam.GetParam(pStock, 1) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 2) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 3) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 4) < MAXSTATS) { return false; }
                    if (PatchGetBaseParam.GetParam(pStock, 5) < MAXSTATS) { return false; }
                    if (paramSet)
                        { rstcalc.rstSetMaxHpMp(0, ref pStock); }
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
                foreach (datUnitWork_t work in dds3GlobalWork.DDS3_GBWK.unitwork.Where(x => x.id == 0))
                {
                    datUnitWork_t pStock = work;
                    pStock.param[id] += (sbyte)add;
                    if (datCalc.datGetPlayerParam(id) >= MAXSTATS)
                        { pStock.param[id] = MAXSTATS; }
                    if (datCalc.datGetPlayerParam(id) < 1)
                        { pStock.param[id] = 1; }
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
                __result = GetParam(work, paratype);
                return false;
            }
            public static int GetParam(datUnitWork_t work, int paratype)
            {
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
                __result = datCalc.datGetBaseParam(work, paratype) + work.mitamaparam[paratype];
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpMisc), nameof(cmpMisc.cmpUseItemKou))]
        private class PatchIncense
        {
            private static bool Prefix(ushort ItemID, datUnitWork_t pStock)
            {
                int statID = ItemID - 0x26;
                if (statID > -1 && statID < 6)
                {
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
                __result = 0;
                int mitama = MitamaID -= 40;
                if (mitama < 0 || mitama >= 4)
                    { return false; }
                if (EnableIntStat && pStock.param[0] + pStock.mitamaparam[0] >= MAXSTATS &&
                    pStock.param[1] + pStock.mitamaparam[1] >= MAXSTATS &&
                    pStock.param[2] + pStock.mitamaparam[2] >= MAXSTATS &&
                    pStock.param[3] + pStock.mitamaparam[3] >= MAXSTATS &&
                    pStock.param[4] + pStock.mitamaparam[4] >= MAXSTATS &&
                    pStock.param[5] + pStock.mitamaparam[5] >= MAXSTATS)
                    { return false; }
                else if (pStock.param[0] + pStock.mitamaparam[0] >= MAXSTATS &&
                    pStock.param[2] + pStock.mitamaparam[2] >= MAXSTATS &&
                    pStock.param[3] + pStock.mitamaparam[3] >= MAXSTATS &&
                    pStock.param[4] + pStock.mitamaparam[4] >= MAXSTATS &&
                    pStock.param[5] + pStock.mitamaparam[5] >= MAXSTATS)
                    { return false; }
                System.Random rng = new();
                ushort paramID = fclCombineTable.fclSpiritParamUpTbl[mitama].ParamType[rng.Next(fclCombineTable.fclSpiritParamUpTbl[mitama].ParamType.Length)];
                if (paramID < 0)
                    { return false; }
                if (paramID < pStock.param.Length && paramID < pStock.mitamaparam.Length)
                {
                    int paramNewValue = (pStock.param[paramID] * fclCombineTable.fclSpiritParamUpTbl[mitama].UpRate) / 100 - pStock.param[paramID];
                    if (paramNewValue <= 0)
                        { paramNewValue = 1; }
                    paramNewValue += pStock.mitamaparam[paramID];
                    if (pStock.param[paramID] + paramNewValue < MAXSTATS)
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
                __result = (datCalc.datGetParam(work, 0) + work.level) * 2;
                if (EnableStatScaling)
                    { __result = (datCalc.datGetParam(work, 0) * 2 / POINTS_PER_LEVEL) + work.level * 2; }
                if ((work.badstatus & 0xFFF) == 0x40)
                    { __result = __result >> 1; }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetButuriAttack))]
        private class PatchGetNBPhysicalPow
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                __result = 0;
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(dformindex);
                int paramValue = datCalc.datGetParam(attacker, 0);
                int unkval = 48;
                int finalvalue = 0;
                if (EnableStatScaling)
                    { paramValue /= POINTS_PER_LEVEL; }
                finalvalue = (int)(((datCalc.datGetNormalAtkPow(attacker) * 2) * 1.33f) * 0.8f);
                System.Random rng = new();
                if (EnableStatScaling)
                    { unkval = 64; }
                if (nskill != 0)
                {
                    finalvalue = (int)((float)datCalc.datGetNormalAtkPow(attacker) * (float)waza * 2 / 23.2f * 0.8f);
                    unkval = 50;
                    if (EnableStatScaling)
                        { unkval = 64; }
                }
                int reduction = unkval / (attacker.level + 10);
                if (!EnableStatScaling)
                    { finalvalue = (int)((float)finalvalue * 0.6f); }
                finalvalue = (int)(finalvalue - reduction);
                __result = finalvalue;
                if (dds3ConfigMain.cfgGetBit(9) == 3)
                {
                    __result = (int)(finalvalue * 1.34f);
                    if (!EventBit.evtBitCheck(0x8a0))
                    {
                        __result = finalvalue;
                    }
                }
                else
                {
                    __result = finalvalue;
                    if (dds3ConfigMain.cfgGetBit(9) == 2)
                        { __result = (int)(finalvalue * 1.34); }
                }
                __result = (int)((float)__result * nbCalc.nbGetHojoRitu(sformindex, 4) * nbCalc.nbGetHojoRitu(dformindex, 7));
                if (EnableStatScaling)
                    { __result = (int)((float)__result * 255f / (255f + (float)datCalc.datGetDefPow(defender) * ((float)defender.level / 100))); }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMagicAttack))]
        private class PatchNBGetMagicPow
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                int LevelLimit = attacker.level;
                int skillPower = datNormalSkill.tbl[nskill].hpn;
                int skillLimit = datNormalSkill.tbl[nskill].magiclimit;
                int skillBase = datNormalSkill.tbl[nskill].magicbase;
                int damageCalc = (int)((float)waza * (float)attacker.level * 2f / 21 + (float)skillBase);
                if (EnableStatScaling)
                    { damageCalc = (int)((float)waza * (float)attacker.level * 2f / 20f + (float)skillBase); }
                if (damageCalc > skillLimit)
                    { damageCalc = skillLimit; }
                if (LevelLimit > 160 && !EnableStatScaling)
                    { LevelLimit = 160; }
                int param = datCalc.datGetParam(attacker, 2);
                if (EnableIntStat)
                    { param = datCalc.datGetParam(attacker, 1); }
                if (EnableStatScaling)
                {
                    param /= POINTS_PER_LEVEL;
                    damageCalc = (int)((float)damageCalc + ((float)skillPower + (float)skillBase) * 2 + (float)damageCalc / 100f * ((float)param - ((float)LevelLimit / 5f + 4f)) * 2.5f * 0.8f);
                }
                else
                    { damageCalc = (int)((float)damageCalc + (float)damageCalc / 100f * ((float)param - ((float)LevelLimit / 5f + 4f)) * 2.5f * 0.8f); }
                int damageCalc2 = damageCalc;
                if (!EnableStatScaling)
                {
                    if (attacker.level > 100)
                    {
                        int wazaCalc = waza * 200;
                        int levelcheck = 100;
                        do
                        {
                            damageCalc2 = (wazaCalc * attacker.level) / 21 + skillBase;
                            if (damageCalc2 > skillLimit)
                            { damageCalc2 = skillLimit; }
                            LevelLimit = levelcheck;
                            if (LevelLimit > 160)
                            { LevelLimit = 160; }
                            levelcheck++;
                            wazaCalc += waza * 2;
                            damageCalc2 = (int)((damageCalc2 + (damageCalc2 / 100) * (param - (LevelLimit / 5 + 4)) * 2.5f) * 0.8f);
                            if (damageCalc <= damageCalc2)
                            { damageCalc = damageCalc2; }
                            damageCalc2 = damageCalc;
                        }
                        while (levelcheck < attacker.level);
                    }
                }
                if ((attacker.flag >> 5 & 1) != 0)
                {
                    damageCalc = (int)(damageCalc2 * 0.75f + -50 / (attacker.level + 10));
                    if (dds3ConfigMain.cfgGetBit(9) == 3)
                    {
                        damageCalc2 = (int)(damageCalc * 1.34f);
                        if (!EventBit.evtBitCheck(0x8a0))
                        { damageCalc2 = damageCalc; }
                    }
                    else
                    {
                        damageCalc2 = damageCalc;
                        if (dds3ConfigMain.cfgGetBit(9) == 2)
                        { damageCalc2 = (int)(damageCalc * 1.34f); }
                    }
                }

                __result = (int)(damageCalc2 * nbCalc.nbGetHojoRitu(sformindex, 5) * nbCalc.nbGetHojoRitu(dformindex, 7));
                if (EnableStatScaling)
                {
                    int param2 = datCalc.datGetParam(defender, 2);
                    { __result = (int)((float)__result * 255f / (255f + ((float)param2 * 2f + defender.level) * 2f * ((float)defender.level / 100f))); }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMagicKaifuku))]
        private class PatchGetMagicHealing
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                datUnitWork_t work = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                System.Random rng = new();
                int param = datCalc.datGetParam(work, 2);
                if (EnableIntStat)
                    { param = datCalc.datGetParam(work, 1); }
                if (EnableStatScaling)
                    { param /= POINTS_PER_LEVEL; }
                __result = (int)(nbCalc.nbGetHojoRitu(sformindex, 5) * (rng.Next(0, 8) + param * 4 + work.level / 10) * waza);
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMakaBaramaki))]
        private class PatchGetMaccaScattering
        {
            private static bool Prefix(out int __result, datUnitWork_t w)
            {
                datUnitWork_t work = dds3GlobalWork.DDS3_GBWK.unitwork[0];
                datDevilFormat_t devil = datDevilFormat.Get(w.id, true);
                int playerLuck = datCalc.datGetParam(work, 5);
                int macca = dds3GlobalWork.DDS3_GBWK.maka;
                int baseMacca = macca;
                int luck = datCalc.datGetParam(w, 5);
                System.Random rng = new();
                __result = 0;
                if (macca == 0)
                    { return false; }
                float adjform, baseform;
                adjform = 0.0f;
                baseform = 0.0f;
                if (EnableStatScaling)
                {
                    luck = luck / POINTS_PER_LEVEL;
                    playerLuck = (MAXSTATS - playerLuck) / POINTS_PER_LEVEL;
                }
                if ((w.flag >> 5 & 1) == 0)
                {
                    baseform = Mathf.Abs((float)luck / ((float)w.level / 5.0f + 4.0f));
                    if (EnableStatScaling)
                        { baseform = Mathf.Abs(30f * ((float)w.level / 25.5f + (float)luck / 2f + 1)); }
                    if (baseform < 0.001f)
                        { adjform = 0; }
                    else
                    {
                        adjform = (float)macca / 20.0f * baseform;
                        if (EnableStatScaling)
                            { adjform = (float)(macca) * baseform; }
                    }
                }
                else
                {
                    if (work.badstatus == 0)
                        { return false; }
                    baseform = Mathf.Abs((float)playerLuck / ((float)work.level / 5.0f + 4.0f));
                    if (EnableStatScaling)
                    {
                        baseform = Mathf.Abs(30f * ((float)work.level / 25.5f / 2f + (float)playerLuck / 2 + 1));
                    }
                    adjform = (float)devil.dropmakka * baseform;
                }
                float variance = (float)rng.NextDouble();
                __result = (int)Mathf.Abs((variance - 0.5f) * 2f * 0.1f * ((float)adjform * 2.0f));
                if (EnableStatScaling)
                    { __result = (int)Mathf.Abs(Mathf.Pow((float)adjform, 1.125f) * (1f + Mathf.Log10(adjform)) * (0.1f + variance * 2 / 3) / baseform); }
                if(dds3ConfigMain.cfgGetBit(9) <= 1 && (w.flag & 0x20) == 0)
                    { __result = __result / 10; }
                if (__result < 2)
                    { __result = 1; }
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetSakePow))]
        private class PatchGetSakePow
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                __result = work.level + datCalc.datGetParam(work, 4) * 2;
                int luc = datCalc.datGetParam(work, 5);
                if (luc < 2 || (work.badstatus & 0xFFF) == 0x200)
                    { luc = 1; }
                __result += luc + 10;
                if (EnableStatScaling)
                    {__result = work.level + (datCalc.datGetParam(work, 4) * 2 + datCalc.datGetParam(work, 5)) / POINTS_PER_LEVEL;}
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetMagicHitPow))]
        private class PatchGetMagicAccuracy
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                __result = work.level + datCalc.datGetParam(work, 2) + datCalc.datGetParam(work, 4) * 2;
                if (EnableIntStat)
                    { __result = work.level + datCalc.datGetParam(work, 1) * 2 + datCalc.datGetParam(work, 2) + datCalc.datGetParam(work, 4); }
                int luc = datCalc.datGetParam(work, 5);
                if (luc < 2 || (work.badstatus & 0xFFF) == 0x200)
                    { luc = 1; }
                int luckValue = luc + 6;
                if (luc + 5 > -1)
                    { luckValue = luc + 5; }
                __result += luckValue >> 1 + 0xf;
                if (EnableStatScaling)
                {
                    __result = work.level + (datCalc.datGetParam(work, 2) * 2 + datCalc.datGetParam(work, 4) * 2 + datCalc.datGetParam(work, 5)) / POINTS_PER_LEVEL;
                    if (EnableIntStat)
                        { __result = work.level + (datCalc.datGetParam(work, 1) * 2 + datCalc.datGetParam(work, 2) + datCalc.datGetParam(work, 4) + datCalc.datGetBaseParam(work, 5)) / POINTS_PER_LEVEL; }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetDefPow))]
        private class PatchGetDefPow
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                __result = (datCalc.datGetParam(work, 3) + work.level) * 2;
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
                Il2CppReferenceArray<Il2CppStructArray<sbyte>> effectlist = koukalist;
                if (datNormalSkill.tbl[nskill].targetrandom < 1 || effectlist.Length == 0)
                    { return true; }
                int i = 0;
                do
                {
                    if (effectlist[i].Length < 2)
                        { return true; }
                    effectlist[i][0] = -1;
                    effectlist[i][1] = 0;
                    i++;
                }
                while (i < effectlist.Length);
                System.Random rng = new();
                int hitcount = rng.Next(datNormalSkill.tbl[nskill].targetcntmin, datNormalSkill.tbl[nskill].targetcntmax);
                i = 0;
                do
                {
                    if (((1 << i & 0x1f) & select) != 0)
                    {
                        if (a.data.form[i].partyindex >= a.data.party.Length)
                        { return true; }
                        if ((a.data.party[a.data.form[i].partyindex].flag >> 5 & 1) == 0)
                        {
                            if (a.timelist.Length <= i)
                            { return true; }
                            if (a.timelist[i].hp != 0 || (nbCalc.nbGetDevilFormatFlag(i) >> 8 & 1) != 0)
                            { i++; continue; }
                        }
                        select = (uint)(select & (i ^ -1));
                    }
                    i++;
                }
                while (i < 0xf);
                int effectID = rng.Next(4, a.timelist.Length - 1);
                i = 0;
                int j = 0;
                int enemycnt = 0;
                do
                {
                    j = 0;
                    do
                    {
                        effectID = rng.Next(4, a.timelist.Length - 1);
                        j++;
                    }
                    while (a.timelist[effectID].hp == 0 && j < 100);
                    bool found = false;
                    for (j = 0; j < a.timelist.Length; j++)
                    {
                        if (effectlist[j][0] == (sbyte)a.data.form[effectID].formindex)
                        {
                            found = true;
                            effectlist[j][1]++;
                        }
                    }
                    if (found == false)
                    {
                        effectlist[enemycnt][0] = (sbyte)a.data.form[effectID].formindex;
                        effectlist[enemycnt][1]++;
                        enemycnt++;
                    }
                    i++;
                }
                while (i < hitcount);
                koukalist = effectlist;
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetBaseMaxHp))]
        private class PatchGetBaseMaxHP
        {
            public static int GetBaseMaxHP(datUnitWork_t work)
            {
                int result = (datCalc.datGetBaseParam(work, 3) + work.level) * 6;
                if (rstinit.GBWK != null)
                    { result += rstinit.GBWK.ParamOfs[3] * 6; }
                if (EnableStatScaling)
                {
                    result = (int)((float)datCalc.datGetBaseParam(work, 3) / (float)POINTS_PER_LEVEL + (float)work.level) * 6;
                    if (rstinit.GBWK != null)
                        { result += (int)((float)rstinit.GBWK.ParamOfs[3] / (float)POINTS_PER_LEVEL) * 6; }
                }
                return result;
            }

            private static bool Prefix(ref int __result, datUnitWork_t work)
            {
                __result = GetBaseMaxHP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetBaseMaxMp))]
        private class PatchGetBaseMaxMP
        {
            public static int GetBaseMaxMP(datUnitWork_t work)
            {
                int result = (datCalc.datGetBaseParam(work, 2) + work.level) * 3;
                if (rstinit.GBWK != null)
                    { result += rstinit.GBWK.ParamOfs[2] * 3; }
                if (EnableStatScaling)
                {
                    result = datCalc.datGetBaseParam(work, 2) * 3 / POINTS_PER_LEVEL + work.level * 3;
                    if (rstinit.GBWK != null)
                        { result += rstinit.GBWK.ParamOfs[2] * 3 / POINTS_PER_LEVEL; }
                }
                return result;
            }

            private static bool Prefix(ref int __result, datUnitWork_t work)
            {
                __result = GetBaseMaxMP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetMaxHp))]
        private class PatchGetMaxHP
        {
            public static uint GetMaxHP(datUnitWork_t work)
            {
                uint result = (uint)PatchGetBaseMaxHP.GetBaseMaxHP(work);
                result += datCalc.datCheckSyojiSkill(work, 0x122) == 1 ? (uint)(result * 0.1) : 0;
                result += datCalc.datCheckSyojiSkill(work, 0x123) == 1 ? (uint)(result * 0.2) : 0;
                result += datCalc.datCheckSyojiSkill(work, 0x124) == 1 ? (uint)(result * 0.3) : 0;
                result = Math.Clamp(result, 1, MAXHPMP);
                return result;
            }

            private static bool Prefix(ref uint __result, datUnitWork_t work)
            {
                __result = PatchGetMaxHP.GetMaxHP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetMaxMp))]
        private class PatchGetMaxMP
        {
            public static uint GetMaxMP(datUnitWork_t work)
            {
                uint result = (uint)PatchGetBaseMaxMP.GetBaseMaxMP(work);
                result += datCalc.datCheckSyojiSkill(work, 0x125) == 1 ? (uint)(result * 0.1) : 0;
                result += datCalc.datCheckSyojiSkill(work, 0x126) == 1 ? (uint)(result * 0.2) : 0;
                result += datCalc.datCheckSyojiSkill(work, 0x127) == 1 ? (uint)(result * 0.3) : 0;
                result = Math.Clamp(result, 1, MAXHPMP);
                return result;
            }
            private static bool Prefix(ref uint __result, datUnitWork_t work)
            {
                __result = (uint)PatchGetMaxMP.GetMaxMP(work);
                return false;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstSetMaxHpMp))]
        private class PatchSetMaxHPMP
        {
            private static bool Prefix(sbyte Mode, ref datUnitWork_t pStock)
            {
                pStock.maxhp = (ushort)datCalc.datGetMaxHp(pStock);
                pStock.maxmp = (ushort)datCalc.datGetMaxMp(pStock);
                if (Mode == 1)
                {
                    pStock.hp = pStock.maxhp;
                    pStock.mp = pStock.maxmp;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstAddLevel))]
        private class PatchAddLevel
        {
            private static bool Prefix(int Val, datUnitWork_t pStock)
            {
                pStock.level = (ushort)Math.Clamp(pStock.level + Val, 1, 0xff);
                return false;
            }
        }

        [HarmonyPatch(typeof(rstCalcCore), nameof(rstCalcCore.cmbAddLevelUpParamEx))]
        private class PatchAddLevelUpParamEx
        {
            private static bool Prefix(out sbyte __result, ref datUnitWork_t pStock, sbyte Mode)
            {
                __result = AddLevelUpParam(ref pStock, Mode);
                return false;
            }

            public static sbyte AddLevelUpParam(ref datUnitWork_t pStock, sbyte Mode)
            {
                bool[] paramChecks = { false, false, false, false, false, false };
                for (int i = 0; i < paramChecks.Length; i++)
                {
                    if (pStock.param[i] + rstinit.GBWK.ParamOfs[i] >= MAXSTATS)
                        {paramChecks[i] = true;}
                }
                do
                {
                    int ctr = (int)(fclMisc.FCL_RAND() % paramChecks.Length);
                    if (paramChecks[ctr] == true)
                        { continue; }
                    if (ctr > 0 && !EnableIntStat)
                        { ctr++; }
                    if (rstinit.GBWK.ParamOfs.Length <= ctr)
                        { break; }
                    rstinit.GBWK.ParamOfs[ctr]++;
                    if (pStock.param[ctr] + rstinit.GBWK.ParamOfs[ctr] <= 0)
                        { return 0x7f; }
                    return (sbyte)ctr;
                }
                while (true);
                return 6;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstAutoAsignDevilParam))]
        private class PatchAutoAsignDevilParam
        {
            private static bool Prefix()
            {
                EvoCheck = false;
                int i = 0;
                for (i = 0; i < rstinit.GBWK.ParamOfs.Length; i++)
                {
                    if (i == 1 && !EnableIntStat)
                        { continue; }
                    rstinit.GBWK.ParamOfs[i] = 0;
                }
                i = 0;
                datUnitWork_t pStock = rstinit.GBWK.pCurrentStock;
                do
                {
                    if (rstinit.GBWK.AsignParam * POINTS_PER_LEVEL <= i)
                        { break; }
                    var paramID = rstCalcCore.cmbAddLevelUpParamEx(ref pStock, 0);
                    if (paramID > 5 || paramID == -1)
                        { continue; }
                    if (pStock.param[paramID] + rstinit.GBWK.ParamOfs[paramID] >= MAXSTATS)
                        { pStock.param[paramID] = MAXSTATS; }
                    else
                        { pStock.param[paramID] += rstinit.GBWK.ParamOfs[paramID]; }
                    i++;
                }
                while (true);
                return false;
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstCalcEvo))]
        private class PatchChkDevilEvo
        {
            private static bool Prefix()
                { EvoCheck = true; return true; }
        }

        [HarmonyPatch(typeof(cmpPanel), nameof(cmpPanel.cmpDrawDevilInfo))]
        private class PatchDrawDevilInfo
        {
            private static void Postfix(int X, int Y, uint Z, uint Col, sbyte SelFlag, sbyte DrawType, datUnitWork_t pStock, cmpCursorEff_t pEff, int FadeRate, GameObject obj, int MatCol)
            {
                int[] StockStats = new int[] { pStock.hp, pStock.mp };
                for (int i = 0; i < 2; i++)
                {
                    GameObject g2 = GameObject.Find(obj.name + "/" + StockBarValues[i]);
                    if (g2 == null)
                    { continue; }
                    if (g2.GetComponent<CounterCtr>().image.Length < 4)
                    {
                        GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtr>().image[0].gameObject);
                        g.transform.parent = g2.transform;
                        g.transform.position = g2.GetComponent<CounterCtr>().image[0].transform.position;
                        g.transform.localPosition = g2.GetComponent<CounterCtr>().image[0].transform.localPosition;
                        g.transform.localScale = g2.GetComponent<CounterCtr>().image[0].transform.localScale;
                        g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                        for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                        {
                            bool chk = g2.GetComponent<CounterCtr>().image[j].gameObject.active;
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = true;
                            g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3(118 - j * 25, 31, -4);
                            g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtr>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtr>().image[j].transform.localScale.y, g2.GetComponent<CounterCtr>().image[j].transform.localScale.z);
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = chk;
                        }
                        GameObject.DontDestroyOnLoad(g);
                    }
                    g2.GetComponent<CounterCtr>().Set(StockStats[i], Color.white, 0);
                }
            }
        }

        [HarmonyPatch(typeof(nbPanelProcess), nameof(nbPanelProcess.nbPanelPartyDraw))]
        private class PatchPanelPartyDraw
        {
            private static void Postfix()
            {
                for (int i = 0; i < 4; i++)
                {
                    nbParty_t party = nbMainProcess.nbGetPartyFromFormindex(i);
                    if (party == null)
                        { continue; }
                    if (i > 0 && party.statindex == 0)
                        { continue; }
                    datUnitWork_t pStock = dds3GlobalWork.DDS3_GBWK.unitwork[party.statindex];
                    int[] PartyStats = new int[] { pStock.hp, pStock.mp };

                    for (int k = 0; k < 2; k++)
                    {
                        GameObject g2 = GameObject.Find("bparty(Clone)/bparty_window0" + (i + 1) + "/" + PartyBarValues[k]);
                        if (g2 == null)
                        { continue; }
                        if (g2.GetComponent<CounterCtrBattle>().image.Length < 4)
                        {
                            GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtrBattle>().image[0].gameObject);
                            g.transform.parent = g2.transform;
                            g.transform.position = g2.GetComponent<CounterCtrBattle>().image[0].transform.position;
                            g.transform.localPosition = g2.GetComponent<CounterCtrBattle>().image[0].transform.localPosition;
                            g.transform.localScale = g2.GetComponent<CounterCtrBattle>().image[0].transform.localScale;
                            g2.GetComponent<CounterCtrBattle>().image = g2.GetComponent<CounterCtrBattle>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                            for (int j = 0; j < g2.GetComponent<CounterCtrBattle>().image.Length; j++)
                            {
                                bool chk = g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active;
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = true;
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localPosition = new Vector3(119 - j * 25, 0, -4);
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.y, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.z);
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = chk;
                            }
                            GameObject.DontDestroyOnLoad(g);
                        }
                        g2.GetComponent<CounterCtrBattle>().Set(PartyStats[k], Color.white, 0);
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        GameObject g2 = GameObject.Find("summon_command/bmenu_command/bmenu_command_s0" + (i + 1) + "/" + PartyBarValues[k]);
                        if (g2 == null)
                        { continue; }
                        if (g2.GetComponent<CounterCtrBattle>().image.Length < 4)
                        {
                            GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtrBattle>().image[0].gameObject);
                            g.transform.parent = g2.transform;
                            g.transform.position = g2.GetComponent<CounterCtrBattle>().image[0].transform.position;
                            g.transform.localPosition = g2.GetComponent<CounterCtrBattle>().image[0].transform.localPosition;
                            g.transform.localScale = g2.GetComponent<CounterCtrBattle>().image[0].transform.localScale;
                            g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                            g2.GetComponent<CounterCtrBattle>().image = g2.GetComponent<CounterCtrBattle>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                            for (int j = 0; j < g2.GetComponent<CounterCtrBattle>().image.Length; j++)
                            {
                                bool chk = g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active;
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = true;
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localPosition = new Vector3(119 - j * 25, 0, -4);
                                g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.y, g2.GetComponent<CounterCtrBattle>().image[j].transform.localScale.z);
                                g2.GetComponent<CounterCtrBattle>().image[j].gameObject.active = chk;
                            }
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
                datUnitWork_t unit = nbPanelProcess.pNbPanelAnalyzeUnitWork;
                if (unit != null)
                {
                    int[] AnalyzeStats = new int[] { unit.hp, unit.maxhp, unit.mp, unit.maxmp };
                    string[] images = { "num_hp01", "num_hpfull01", "num_mp01", "num_mpfull01", };
                    for (int i = 0; i < 4; i++)
                    {
                        GameObject g2 = GameObject.Find(AnalyzeBarValues[i / 2] + "/" + images[i]);
                        if (g2 == null)
                            { continue; }
                        if (g2.GetComponent<CounterCtr>() == null)
                            { continue; }
                        if (g2.GetComponent<CounterCtr>().image.Length < 5)
                        {
                            for (int j = g2.GetComponent<CounterCtr>().image.Length; j < 5; j++)
                            {
                                GameObject g = GameObject.Instantiate(g2);
                                GameObject.Destroy(g.GetComponent<CounterCtr>());
                                g.name = images[i].Replace("1","") + (i + 1);
                                g.transform.parent = g2.transform.parent;
                                g.transform.position = g2.GetComponent<CounterCtr>().transform.position;
                                g.transform.localPosition = g2.GetComponent<CounterCtr>().transform.localPosition;
                                g.transform.localScale = g2.GetComponent<CounterCtr>().transform.localScale;
                                g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                                GameObject.DontDestroyOnLoad(g);
                            }
                            for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                            {
                                g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3((i % 2) * 130 + 86 - j * 20 + 5, 32, -8);
                                g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(0.8f, 1, 1);
                            }
                        }
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
                ResetParam();
                return false;
            }
            public static void ResetParam()
            {
                rstinit.GBWK.AsignParam = (short)(rstinit.GBWK.LevelUpCnt * POINTS_PER_LEVEL);
                rstinit.GBWK.AsignParamMax = (short)(rstinit.GBWK.LevelUpCnt * POINTS_PER_LEVEL);
                for (int i = 0; i < rstinit.GBWK.ParamOfs.Length; i++)
                    { rstinit.GBWK.ParamOfs[i] = 0; }
                rstinit.SetPointAnime(rstinit.GBWK.AsignParam);
            }
        }

        [HarmonyPatch(typeof(rstupdate), nameof(rstupdate.rstUpdateSeqAsignPlayerParam))]
        private class PatchUpdateAsignPlayerParam
        {
            public static sbyte YesResponse()
            {
                rstinit.GBWK.SeqInfo.Current = 0x18;
                return 1;
            }
            public static sbyte NoResponse()
            {
                rstupdate.rstResetAsignParam();
                return 0;
            }
            private static bool Prefix(ref datUnitWork_t pStock)
            {
                if (fclMisc.fclChkMessage() == 2)
                {
                    if (fclMisc.fclGetSelMessagePos() == 0
                        && dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OK)
                        && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OK) == true)
                    {
                        if (fclMisc.fclChkSelMessage() == 1)
                            { YesResponse(); }
                    }
                    if ((fclMisc.fclGetSelMessagePos() == 1
                        && dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OK)
                        && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OK) == true) ||
                        (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL)
                        && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL) == true))
                    {
                        if (fclMisc.fclChkSelMessage() == 1)
                            { NoResponse(); }
                    }
                    return false;
                }
                if (SettingAsignParam == false)
                {
                    SettingAsignParam = true;
                    EvoCheck = false;
                    PatchResetAsignParam.ResetParam();
                }
                if (rstinit.GBWK.LevelUpCnt <= 0)
                    { SettingAsignParam = false; }
                if (EnableIntStat && pStock.param[0] + rstinit.GBWK.ParamOfs[0] >= MAXSTATS &&
                    pStock.param[1] + rstinit.GBWK.ParamOfs[1] >= MAXSTATS &&
                    pStock.param[2] + rstinit.GBWK.ParamOfs[2] >= MAXSTATS &&
                    pStock.param[3] + rstinit.GBWK.ParamOfs[3] >= MAXSTATS &&
                    pStock.param[4] + rstinit.GBWK.ParamOfs[4] >= MAXSTATS &&
                    pStock.param[5] + rstinit.GBWK.ParamOfs[5] >= MAXSTATS)
                    { YesResponse(); return false; }
                else if (pStock.param[0] + rstinit.GBWK.ParamOfs[0] >= MAXSTATS &&
                    pStock.param[2] + rstinit.GBWK.ParamOfs[2] >= MAXSTATS &&
                    pStock.param[3] + rstinit.GBWK.ParamOfs[3] >= MAXSTATS &&
                    pStock.param[4] + rstinit.GBWK.ParamOfs[4] >= MAXSTATS &&
                    pStock.param[5] + rstinit.GBWK.ParamOfs[5] >= MAXSTATS)
                    { YesResponse(); return false; }
                if (cmpStatus.statusObj == null)
                    { YesResponse(); return false; }
                if (fclMisc.fclChkMessage() != 0)
                    { return false; }
                int cursorIndex = cmpMisc.cmpGetCursorIndex(rstinit.GBWK.ParamCursor);
                sbyte cursorParam = (sbyte)cursorIndex;
                if (!EnableIntStat)
                    { cursorParam = cmpMisc.cmpExchgParamIndex((sbyte)cursorIndex); }
                else
                {
                    if (cmpStatus._statusUIScr.ObjStsBar.Length < 6)
                    {
                        GameObject g = GameObject.Find("statusUI(Clone)/sstatus/sstatusbar06");
                        cmpStatus._statusUIScr.ObjStsBar = cmpStatus._statusUIScr.ObjStsBar.Append(g).ToArray();
                        GameObject.Find("statusUI(Clone)/sstatus/sstatusnum06");
                        cmpStatus._statusUIScr.ObjStatus = cmpStatus._statusUIScr.ObjStsBar.Append(g).ToArray();
                    }
                    rstinit.GBWK.ParamCursor.CursorPos.ShiftMax = 6;
                    rstinit.GBWK.ParamCursor.CursorPos.ListNums = 6;
                }
                cmpUpdate.cmpSetupObject(cmpStatus._statusUIScr.gameObject, true);
                cmpUpdate.cmpMenuCursor(cursorIndex, cmpStatus._statusUIScr.stsCursor, cmpStatus._statusUIScr.ObjStsBar);
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.U) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.U) == true)
                {
                    cursorIndex = cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 0);
                    if (EnableIntStat && cursorIndex < 6)
                        { cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, -1); cmpMisc.cmpPlaySE(1 & 0xFFFF); }
                    else if (cursorIndex < 5)
                        { cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, -1); cmpMisc.cmpPlaySE(1 & 0xFFFF); }
                    else
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); }
                    rstinit.SetPointAnime(cursorIndex);
                }
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.D) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.D) == true)
                {
                    cursorIndex = cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 0);
                    if (EnableIntStat && cursorIndex < 6)
                        { cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 1); cmpMisc.cmpPlaySE(1 & 0xFFFF); }
                    else if (cursorIndex < 5)
                        { cmpMisc.cmpMoveCursor(rstinit.GBWK.ParamCursor, 1); cmpMisc.cmpPlaySE(1 & 0xFFFF); }
                    else
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); }
                    rstinit.SetPointAnime(cursorIndex);
                }
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.CANCEL) == true)
                {
                    rstupdate.rstResetAsignParam();
                    cmpMisc.cmpPlaySE(2 & 0xFFFF);
                }
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OK) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OK) == true)
                {
                    if (pStock.param[cursorParam] + rstinit.GBWK.ParamOfs[cursorParam] > MAXSTATS)
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); return false; }
                    if (rstinit.GBWK.AsignParam > 0)
                    {
                        rstinit.GBWK.ParamOfs[cursorParam]++;
                        rstinit.GBWK.AsignParam--;
                        cmpMisc.cmpPlaySE(1 & 0xFFFF);
                    }
                    if (rstinit.GBWK.AsignParam == 0)
                    {
                        fclMisc.fclStartMessage(2);
                        fclMisc.fclStartSelMessage(0x2b);
                        fclMisc.gSelMsgNo = 0x2b;
                    }
                }
                if (dds3PadManager.DDS3_PADCHECK_PRESS(Il2Cpplibsdf_H.SDF_PADMAP.OPT1) && dds3PadManager.DDS3_PADCHECK_REP(Il2Cpplibsdf_H.SDF_PADMAP.OPT1) == true)
                {
                    if (rstinit.GBWK.ParamOfs[cursorParam] < 1)
                        { cmpMisc.cmpPlaySE(2 & 0xFFFF); return false; }
                    else
                    {
                        rstinit.GBWK.ParamOfs[cursorParam]--;
                        rstinit.GBWK.AsignParam++;
                        cmpMisc.cmpPlaySE(2 & 0xFFFF);
                    }
                }
                rstcalc.rstSetMaxHpMp(0, ref pStock);
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpMisc), nameof(cmpMisc.cmpGetParamName))]
        private class PatchGetParamName
        {
            private static bool Prefix(out string __result, sbyte Index)
            {
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
                if (!EnableIntStat)
                    { return true; }
                int i = 0;
                GameObject g = GameObject.Find("magUI(Clone)/magstatus");
                if (g == null)
                    { return false; }
                if (g.activeSelf == false)
                    { return false; }
                if(!GameObject.Find("magUI(Clone)/magstatus/magstatus_base06"))
                {
                    GameObject orig = GameObject.Find("magUI(Clone)/magstatus/magstatus_base01");
                    GameObject g2 = GameObject.Instantiate(orig);
                    GameObject.DontDestroyOnLoad(g2);
                    g2.name = "magstatus_base06";
                    g2.transform.parent = g.transform;
                    g2.transform.position = orig.transform.position;
                    g2.transform.localScale = orig.transform.localScale;
                    g2.transform.localPosition = new(orig.transform.localPosition.x, orig.transform.localPosition.y - 56 * 5, orig.transform.localPosition.z);
                    for (i = 0; i < 6; i++)
                    {
                        GameObject g3 = GameObject.Find("magUI(Clone)/magstatus/magstatus_base0" + (i + 1));
                        if (g3 == null)
                            { continue; }
                        Vector3 newScale = g3.transform.localScale;
                        Vector3 newPos = g3.transform.localPosition;
                        newPos.x *= 1;
                        newPos.y *= 0.825f;
                        newPos.x *= 1;
                        newScale.x *= 0.825f;
                        newScale.y *= 0.825f;
                        newScale.z *= 1;
                        g3.transform.localScale = newScale;
                        g3.transform.localPosition = newPos;
                    }
                }
                if (!GameObject.Find("magUI(Clone)/magstatus/magstatus_item06"))
                {
                    GameObject orig = GameObject.Find("magUI(Clone)/magstatus/magstatus_item01");
                    GameObject g2 = GameObject.Instantiate(orig);
                    GameObject.DontDestroyOnLoad(g2.gameObject);
                    g2.name = "magstatus_item06";
                    g2.transform.parent = g.transform;
                    g2.transform.position = orig.transform.position;
                    g2.transform.localScale = orig.transform.localScale;
                    g2.transform.localPosition = new(orig.transform.localPosition.x, orig.transform.localPosition.y - 56 * 5, orig.transform.localPosition.z);
                    for (i = 0; i < 6; i++)
                    {
                        GameObject g3 = GameObject.Find("magUI(Clone)/magstatus/magstatus_item0" + (i + 1));
                        if (g3 == null)
                            { continue; }
                        Vector3 newScale = g3.transform.localScale;
                        Vector3 newPos = g3.transform.localPosition;
                        newPos.x *= 1;
                        newPos.y *= 0.825f;
                        newPos.x *= 1;
                        newScale.x *= 0.825f;
                        newScale.y *= 0.825f;
                        newScale.z *= 1;
                        g3.transform.localScale = newScale;
                        g3.transform.localPosition = newPos;
                    }
                }
                fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, new(4), 0xb, 0, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, new(4), 0xb, 1, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                uint color = fclMisc.fclGetBlendColor(0x80808080,0x40404080,(uint)pHeartsInfo.Timer);
                uint[] colorptr = { color ,color ,color ,color };
                do
                {
                    int unk = 0;
                    if (paramNames.Length < i)
                        { break; }
                    g = GameObject.Find("magUI(Clone)/magstatus/magstatus_item0" + (i+1));
                    if (g == null)
                        { i++; continue; }
                    cmpUpdate.cmpSetupObject(g, true);
                    GameObject g2 = GameObject.Find("magUI(Clone)/magstatus/" + g.name + "/TextTM");
                    if (g2 == null)
                        { i++; continue; }
                    g2.GetComponentInChildren<TMP_Text>().SetText(Localize.GetLocalizeText(cmpMisc.cmpGetParamName((sbyte)i)));
                    fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, colorptr, 0xb, 3, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                    fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, colorptr, 0xb, (ushort)(i + 4), cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                    cmpUpdate.cmpSetupObject(g2, true);
                    g2 = GameObject.Find("magUI(Clone)/magstatus/" + g.name + "/magtex");
                    if (g2 == null)
                        { i++; continue; }
                    int heartParam = rstCalcCore.cmbGetHeartsParam(HeartsID, (sbyte)i);
                    if (heartParam == 0)
                        { unk = 0xb; }
                    else
                    {
                        unk = 9;
                        if (heartParam < 1)
                            { unk = 10; }
                    }
                    fclDraw.fclDrawParts(0, 0x28 + i * 0xd0, 0, colorptr, 0xb, (ushort)unk, cmpInitDH.GBWK.TexHandle, etcSprTbl.cmpSprTblArry, 0x47);
                    cmpUpdate.cmpSetupObject(g2, true);
                    g2 = GameObject.Find("magUI(Clone)/magstatus/" + g.name + "/magtex/num_mag");
                    Color rgb = cmpInit.GetToRGBA(heartParam != 0 ? 0xFFFFFFFF : 0x80808080);
                    if (g2.GetComponent<CounterCtr>() == null)
                        { i++; continue; }
                    g2.GetComponent<CounterCtr>().Set(heartParam, rgb);
                    cmpUpdate.cmpSetupObject(g2, true);
                    i++;
                }
                while (i < 6);
                cmpDrawDH.cmpDrawHeartsHelpPanel(pHeartsInfo.Timer);
                cmpDrawDH.cmpDrawHeartsName(0, 0, 0, pHeartsInfo.Timer, HeartsID);
                g = cmpInitDH.DHeartsObj;
                if (g == null)
                    { return false; }
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
                if (ctr2 > pStock.param.Length || pBaseCol.Length == 0 || cmpStatus.statusObj == null)
                    { return; }
                if (pStock.param[ctr2] >= MAXSTATS)
                    { pStock.param[ctr2] = MAXSTATS; }
                GameObject stsObj = GameObject.Find("statusUI(Clone)/sstatus");
                if (stsObj.GetComponentsInChildren<TMP_Text>() != null)
                    { stsObj.GetComponentsInChildren<TMP_Text>()[(ctr2 > 1 && !EnableIntStat) ? ctr2 - 1 : ctr2].SetText(Localize.GetLocalizeText(cmpMisc.cmpGetParamName(ctr2))); }
                if (stsObj.GetComponentsInChildren<TMP_Text>() != null)
                    { stsObj.GetComponentsInChildren<CounterCtr>()[(ctr2 > 1 && !EnableIntStat) ? ctr2 -1 : ctr2].Set(pStock.param[ctr2], Color.white, (CursorMode == 2 && CursorPos > -1) ? 1 : 0); }
                if (-1 < CursorPos)
                    { FlashMode = 2; }
                PatchDrawParamGauge.ReworkParamGauge(pBaseCol, 0x14, (sbyte)ctr2, (sbyte)ctr2, FlashMode, pStock, stsObj);
            }
            private static bool Prefix(int X, int Y, uint[] pBaseCol, sbyte[] pParamOfs, datUnitWork_t pStock, sbyte CursorPos, sbyte CursorMode, sbyte FlashMode)
            {
                if (cmpStatus.statusObj == null)
                    { return false; }
                if (pStock == null)
                    { return false; }
                int[] StatusStats = new int[] { pStock.hp, pStock.maxhp, pStock.mp, pStock.maxmp };
                GameObject stsObj = GameObject.Find("statusUI(Clone)/sstatus");
                if (stsObj == null)
                    { return false; }
                if (stsObj.activeSelf == false)
                    { return false; }
                if (GameObject.Find(stsObj.name + "/sstatusbar_cursur") && EnableIntStat)
                {
                    Vector3 newScale = new(1, 0.9f, 1);
                    GameObject.Find(stsObj.name + "/sstatusbar_cursur").transform.localScale = newScale;
                }
                if (!GameObject.Find(stsObj.name + "/sstatusbar06") && EnableIntStat)
                {
                    GameObject g2 = GameObject.Find(stsObj.name + "/sstatusbar01");
                    if (g2 != null)
                    {
                        GameObject g = GameObject.Instantiate(g2);
                        GameObject.DontDestroyOnLoad(g);
                        g.name = "sstatusbar06";
                        g.transform.parent = g2.transform.parent;
                        g.transform.position = g2.transform.position;
                        g.transform.localPosition = new Vector3(g2.transform.localPosition.x, g2.transform.localPosition.y - 48 * 5, g2.transform.localPosition.z);
                        g.transform.localScale = g2.transform.localScale;
                        for (int i = 0; i < 6; i++)
                        {
                            GameObject g3 = GameObject.Find("sstatusbar0" + (i + 1));
                            if (g3 == null)
                                { continue; }
                            Vector3 newScale = g3.transform.localScale;
                            Vector3 newPos = g3.transform.localPosition;
                            newPos.x *= 1;
                            newPos.y *= 0.9f;
                            newPos.x *= 1;
                            newScale.x *= 1;
                            newScale.y *= 0.9f;
                            newScale.z *= 1;
                            g3.transform.localScale = newScale;
                            g3.transform.localPosition = newPos;
                        }
                    }
                }
                if (!GameObject.Find(stsObj.name + "/sstatusnum06") && EnableIntStat)
                {
                    GameObject g2 = GameObject.Find(stsObj.name + "/sstatusnum01");
                    if (g2 != null)
                    {
                        GameObject g = GameObject.Instantiate(g2);
                        GameObject.DontDestroyOnLoad(g);
                        g.name = "sstatusnum06";
                        g.transform.parent = g2.transform.parent;
                        g.transform.position = g2.transform.position;
                        g.transform.localPosition = new Vector3(g2.transform.localPosition.x, g2.transform.localPosition.y - 48 * 5, g2.transform.localPosition.z);
                        g.transform.localScale = g2.transform.localScale;
                        for (int i = 0; i < 6; i++)
                        {
                            GameObject g3 = GameObject.Find("sstatusnum0" + (i + 1));
                            if (g3 == null)
                                { continue; }
                            Vector3 newScale = g3.transform.localScale;
                            Vector3 newPos = g3.transform.localPosition;
                            newPos.x *= 1;
                            newPos.y *= 0.9f;
                            newPos.x *= 1;
                            newScale.x *= 1;
                            newScale.y *= 0.9f;
                            newScale.z *= 1;
                            g3.transform.localScale = newScale;
                            g3.transform.localPosition = newPos;
                        }
                    }
                }
                if (!GameObject.Find(stsObj.name + "/Text_stat06TM") && EnableIntStat)
                {
                    GameObject g2 = GameObject.Find(stsObj.name + "/Text_stat01TM");
                    if (g2 != null)
                    {
                        GameObject g = GameObject.Instantiate(g2);
                        GameObject.DontDestroyOnLoad(g);
                        g.name = "Text_stat06TM";
                        g.transform.parent = g2.transform.parent;
                        g.transform.position = g2.transform.position;
                        g.transform.localPosition = new Vector3(g2.transform.localPosition.x, g2.transform.localPosition.y - 48 * 5, g2.transform.localPosition.z);
                        g.transform.localScale = g2.transform.localScale;
                        for (int i = 0; i < 6; i++)
                        {
                            GameObject g3 = GameObject.Find("Text_stat0" + (i + 1) + "TM");
                            if (g3 == null)
                                { continue; }
                            Vector3 newScale = g3.transform.localScale;
                            Vector3 newPos = g3.transform.localPosition;
                            newPos.x *= 1;
                            newPos.y *= 0.9f;
                            newPos.x *= 1;
                            newScale.x *= 1;
                            newScale.y *= 0.9f;
                            newScale.z *= 1;
                            g3.transform.localScale = newScale;
                            g3.transform.localPosition = newPos;
                        }
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    GameObject g2 = GameObject.Find(stsObj.name + "/" + StatusBarValues[i]);
                    if (g2 == null)
                        { continue; }
                    if (g2.GetComponent<CounterCtr>().image.Length < 4)
                    {
                        GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtr>().image[0].gameObject);
                        g.transform.parent = g2.transform;
                        g.transform.position = g2.GetComponent<CounterCtr>().image[0].transform.position;
                        g.transform.localPosition = g2.GetComponent<CounterCtr>().image[0].transform.localPosition;
                        g.transform.localScale = g2.GetComponent<CounterCtr>().image[0].transform.localScale;
                        g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                        for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                        {
                            bool chk = g2.GetComponent<CounterCtr>().image[j].gameObject.active;
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = true;
                            g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3(60 - j * 25, 0, -4);
                            g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtr>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtr>().image[j].transform.localScale.y, g2.GetComponent<CounterCtr>().image[j].transform.localScale.z);
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = chk;
                        }
                        GameObject.DontDestroyOnLoad(g);
                    }
                    g2.GetComponent<CounterCtr>().Set(StatusStats[i], Color.white, 0);
                }
                int bars = 5;
                if (EnableIntStat)
                    { bars = 6; }
                for (int i = 0; i < bars; i++)
                {
                    GameObject g2 = GameObject.Find(stsObj.name + "/sstatusnum0" + (i+1));
                    if (g2 == null)
                        { continue; }
                    if (g2.activeSelf == false)
                        { continue; }
                    if (g2.GetComponent<CounterCtr>().image.Length < 3)
                    {
                        GameObject g = GameObject.Instantiate(g2.GetComponent<CounterCtr>().image[0].gameObject);
                        g.transform.parent = g2.transform;
                        g.transform.position = g2.GetComponent<CounterCtr>().image[0].transform.position;
                        g.transform.localPosition = g2.GetComponent<CounterCtr>().image[0].transform.localPosition;
                        g.transform.localScale = g2.GetComponent<CounterCtr>().image[0].transform.localScale;
                        g2.GetComponent<CounterCtr>().image = g2.GetComponent<CounterCtr>().image.Append<Image>(g.GetComponent<Image>()).ToArray<Image>();
                        for (int j = 0; j < g2.GetComponent<CounterCtr>().image.Length; j++)
                        {
                            bool chk = g2.GetComponent<CounterCtr>().image[j].gameObject.active;
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = true;
                            g2.GetComponent<CounterCtr>().image[j].transform.localPosition = new Vector3(30 - j * 25 + 5, 0, -4);
                            g2.GetComponent<CounterCtr>().image[j].transform.localScale = new Vector3(g2.GetComponent<CounterCtr>().image[j].transform.localScale.x * 0.85f, g2.GetComponent<CounterCtr>().image[j].transform.localScale.y, g2.GetComponent<CounterCtr>().image[j].transform.localScale.z);
                            g2.GetComponent<CounterCtr>().image[j].gameObject.active = chk;
                        }
                        GameObject.DontDestroyOnLoad(g);
                    }
                    int stat = (i > 0 && !EnableIntStat) ? i + 1 : i;
                    int levelstat = 0;
                    if (rstinit.GBWK != null && !EvoCheck)
                        { levelstat = rstinit.GBWK.ParamOfs[stat]; }
                    g2.GetComponent<CounterCtr>().Set(rstCalcCore.cmbGetParam(pStock, stat) + levelstat, Color.white, 0);
                }
                if (stsObj.GetComponentsInChildren<sstatusbarUI>() == null)
                    { return false; }
                for (int i = 0; i < 6; i++)
                {
                    if (i == 1 && !EnableIntStat)
                        { continue; }
                    if (stsObj.GetComponentsInChildren<sstatusbarUI>()[(i > 0 && !EnableIntStat) ? i-1 : i] == null)
                        { continue; }
                    if (!stsObj.GetComponentsInChildren<sstatusbarUI>()[(i > 0 && !EnableIntStat) ? i-1 : i].gameObject.activeSelf)
                        { continue; }
                    CreateParamGauge((sbyte)i, (int)(X * 3.75), (int)(Y * 2.25), pBaseCol, pStock, CursorPos, CursorMode, FlashMode);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(cmpDrawStatus), nameof(cmpDrawStatus.cmpDrawParamGauge))]
        private class PatchDrawParamGauge
        {
            private static bool Prefix(int X, int Y, uint[] pBaseCol, int StepY, sbyte Pos, sbyte ParamOfs, sbyte FlashMode, datUnitWork_t pStock, GameObject stsObj)
            {
                if (pStock == null || stsObj == null)
                    { return false; }
                ReworkParamGauge(pBaseCol, StepY, Pos, ParamOfs, FlashMode, pStock, stsObj);
                return false;
            }
            public static void ReworkParamGauge(uint[] pBaseCol, int StepY, sbyte Pos, sbyte ParamOfs, sbyte FlashMode, datUnitWork_t pStock, GameObject stsObj)
            {
                if (stsObj.GetComponentsInChildren<sstatusbarUI>().Length < 6 && EnableIntStat)
                    { return; }
                else if (stsObj.GetComponentsInChildren<sstatusbarUI>().Length < 5)
                    { return; }
                int stat = ParamOfs;
                if (stat > 0 && !EnableIntStat)
                    { stat--; }
                if (stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length != MAXSTATS)
                {
                    GameObject g;
                    while (stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length < MAXSTATS)
                    {
                        g = GameObject.Instantiate(stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Animator>().gameObject);
                        g.transform.parent = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.transform;
                        g.transform.position = g.transform.parent.position;
                        g.transform.localPosition = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Animator>().gameObject.transform.localPosition;
                        g.transform.localScale = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Animator>().gameObject.transform.localScale;
                    }
                    while (stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length > MAXSTATS)
                    {
                        GameObject.Destroy(stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>().Length - 1].gameObject);
                    }
                    for (int len = MAXSTATS - 1; len >= 0; len--)
                    {
                        Vector3 barScale = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localScale;
                        Vector3 barPos = stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localPosition;
                        barScale.x = BAR_SCALE_X;
                        barPos.x = 250 + (len) * BAR_SEGMENT_X + (18 - BAR_SEGMENT_X) + 2 / BAR_SCALE_X;
                        stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localScale = barScale;
                        stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[len].gameObject.transform.localPosition = barPos;
                    }
                    if (barData == null)
                        { barData = AssetBundle.LoadFromFile(AppContext.BaseDirectory + BundlePath + barSpriteName); AssetBundle.DontDestroyOnLoad(barData); }
                    barAsset[stat] = barData.LoadAsset(barSpriteName).Cast<Texture2D>();
                    Texture2D.DontDestroyOnLoad(barAsset[stat]);
                    barSprite[stat] = Sprite.Create(barAsset[stat], new Rect(0, 0, barAsset[stat].width, barAsset[stat].height), Vector2.zero);
                    barSprite[stat].texture.Apply();
                    Sprite.DontDestroyOnLoad(barSprite[stat]);
                    stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentInChildren<Image>().sprite = barSprite[stat];
                }
                int heartValue = 0;
                if ((pStock.flag >> 2 & 1) == 0)
                    { heartValue = 0; }
                else
                    { heartValue = rstCalcCore.cmbGetHeartsParam((sbyte)dds3GlobalWork.DDS3_GBWK.heartsequip, ParamOfs); }
                int paramValue = pStock.param[ParamOfs];
                int levelupValue = 0;
                int mitamaValue = pStock.mitamaparam[ParamOfs];
                if (rstinit.GBWK != null && !EvoCheck)
                    { levelupValue = rstinit.GBWK.ParamOfs[ParamOfs]; }
                for (int ctr = 0; ctr < paramValue + levelupValue + mitamaValue; ctr++)
                {
                    if (MAXSTATS <= ctr)
                        { break; }
                    int segmentColor = 3;
                    if (paramValue + levelupValue + mitamaValue - heartValue > ctr)
                        { segmentColor = 3; }
                    if (paramValue + levelupValue - heartValue > ctr)
                        { segmentColor = 2; }
                    if (paramValue - heartValue > ctr)
                        { segmentColor = 1; }
                    stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[ctr].SetInteger("sstatusbar_color", segmentColor);
                }
                for(int ctr = paramValue + levelupValue + mitamaValue; ctr < MAXSTATS; ctr++)
                    { stsObj.GetComponentsInChildren<sstatusbarUI>()[stat].gameObject.GetComponentsInChildren<Animator>()[ctr].SetInteger("sstatusbar_color", 0); }
                int newFlashMode = FlashMode;
                if (Pos >= cmpDrawStatus.gStatusBlinkQue.Length)
                    { Pos = (sbyte)(cmpDrawStatus.gStatusBlinkQue.Length - 1); }
                if (FlashMode == 0)
                {
                    if (cmpDrawStatus.gStatusBlinkQue[Pos] != 0)
                        {FlashMode = 2;}
                }
                if (FlashMode == 1 || FlashMode == 2)
                    { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], (sbyte)newFlashMode, pCol); }
                if (FlashMode == 3)
                {
                    if (cmpDrawStatus.gStatusBlinkQue[Pos] != 0)
                        { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], 0, pCol); }
                }
                if (FlashMode == 4 || FlashMode == 5)
                    { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], FlashMode, pCol); }
                return;
            }
        }
    }
}