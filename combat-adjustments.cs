using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2Cppnewbattle_H;
using Il2Cppnewdata_H;
using MelonLoader;
using UnityEngine;

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {
        public class TargetHitCountManager
        {
            public static Il2CppReferenceArray<Il2CppStructArray<sbyte>> targetData = new(3);

            // Class Constructor
            public static void SetEffectList(Il2CppReferenceArray<Il2CppStructArray<sbyte>> effectlist)
            {
                // Creates a new set of arrays.
                targetData = new(0x20);

                // If you supplied an effect list.
                if (effectlist != null)
                {
                    // Add a new index for total chance to be hit.
                    for (int i = 0; i < targetData.Length; i++)
                    {
                        targetData[i] = new(3);
                        targetData[i][0] = effectlist[i][0];
                        targetData[i][1] = effectlist[i][1];
                        targetData[i][2] = 100;
                    }
                }
            }

            public static int TotalHitChanceCalc()
            {
                // Loop through all of the hit chances and add them together, then return.
                int totalOdds = 0;
                for (int i = 0; i < targetData.Length; i++)
                    { totalOdds += targetData[i][2]; }
                return totalOdds;
            }

            public static List<sbyte> CreateTargetList()
            {
                // Loop through all of the targets and, if they have a chance to be hit, add them as a potential target.
                List<sbyte> possibleTargets = new();
                for (int i = 0; i < targetData.Length; i++)
                {
                    if (targetData[i][2] > 0 && targetData[i][0] > -1)
                        { possibleTargets = possibleTargets.Append((sbyte)i).ToList(); }
                }
                return possibleTargets;
            }

            public static void HitTarget(int index, int minhits, sbyte odds)
            {
                // Increment the target's hit count and change their odds to be hit based on if they exceed a minimum hit limit.
                targetData[index][1]++;
                if (targetData[index][1] >= minhits)
                    { targetData[index][2] = odds; }
            }

            // Removes a target's hit chance.
            public static void RemoveTarget(int index)
                { targetData[index][2] = 0; }

            public static void RemoveMaxHitTargets(byte maxhits)
            {
                // Loops through all of the targets and, if they've been hit enough, remove their chance to be hit.
                for (int i = 0;i < targetData.Length; i++)
                {
                    if (targetData[i][1] >= maxhits)
                        { RemoveTarget(i); }
                }
            }

            public static void RemoveDeadTargets()
            {
                // Loops through all of the targets and, if they have no HP remaining, remove their chance to be hit.
                for (int i = 0; i < targetData.Length; i++)
                {
                    if (targetData[i][0] < 0)
                        { continue; }
                    datUnitWork_t work = nbMainProcess.nbGetUnitWorkFromFormindex(targetData[i][0]);
                    if (work.hp <= 0)
                        { RemoveTarget(i); }
                }
            }

            public static void ClearNoHitTargets()
            {
                // Loops through all of the targets and, if they have no HP remaining, remove their chance to be hit.
                for (int i = 0; i < targetData.Length; i++)
                {
                    if (targetData[i][0] < 0)
                        { continue; }
                    if (targetData[i][1] == 0)
                        { targetData[i][0] = -1; }
                }
            }
        }

        public class DamageMitigation
        {
            // Damage Mitigation Formula.
            // Requires a work unit and one of the parameter IDs.
            public static float Get(datUnitWork_t work, int param)
                { return 255f / (255f + (float)Math.Pow((double)(0.34f + 0.66f * ((float)work.param[param] / (EnableStatScaling ? (float)STATS_SCALING : 1f) * 2f + (float)work.level / 2f)), 2d) * 4f / 100f); }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbCheckStoneDead))]
        private class PatchStoneTargetKill
        {
            private static bool Prefix(out int __result, int nskill, int dformindex)
            {
                // Result init
                __result = 0;

                // Grab the defender from its form index.
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(dformindex);

                // If you're not petrified, just return.
                if ((defender.badstatus & 0xfff) != 0x400)
                    { return false; }

                // Check the Skill's type and make sure it can kill petrified targets.
                // It only checks Physical, Force, and Shot Skills (Insaniax).
                bool found = false;
                foreach (int i in new int[] { 0, 4, 12 })
                {
                    if (datSkill.tbl[nskill].skillattr == i)
                        { found = true; break; }
                }
                
                // If it can't, return.
                if (found == false)
                    { return false; }

                // Grab the user's Luc. It's actually used in the original formula.
                int luckValue = (int)((float)Math.Clamp(datCalc.datGetParam(defender, 5), 0, MAXSTATS) / (EnableStatScaling ? STATS_SCALING : 1f));

                // Assign a basic flat value for a formula later.
                float flatValue = 20f;

                // Double it because flag nonsense said so.
                if ((defender.flag & 4) != 0)
                    { flatValue = 40f; }

                // This formula is FUCKED, don't ask me about it.
                float finalvalue = ((float)luckValue / (((float)defender.level + 20f) / 5f)) * (flatValue + 100f);

                // Instead, here's a better one.
                if (EnableStatScaling)
                    { finalvalue = 75.0f - (float)luckValue / 2f; }

                // RNGesus Take the Wheel
                System.Random rng = new();

                // Return 1 if you hit the defender and killed them, otherwise return 0.
                __result = finalvalue <= rng.Next(100) ? 1 : 0;
                return false;
            }
        }

        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datGetNormalAtkPow))]
        private class PatchGetNormalAtkPow
        {
            private static bool Prefix(out int __result, datUnitWork_t work)
            {
                // Set this variable to the attacker's Str.
                int param = Math.Clamp(datCalc.datGetParam(work, 0), 0, MAXSTATS);

                // If their "badstatus" is 0x200, their Str is set to 1.
                if ((work.badstatus & 0xFFF) == 0x200)
                    { param = 1; }

                // Resulting Formula
                __result = (int)(((float)param / (EnableStatScaling ? (float)STATS_SCALING : 1f) + (float)work.level) * 2f);

                // I dunno what "badstatus" actually is besides a bitflag, but if this setup works, your attack power is basically halved.
                if ((work.badstatus & 0xFFF) == 0x40)
                    { __result = __result >> 1; }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetExpMakaItem))]
        private class PatchAddExp
        {
            private static bool Prefix(datUnitWork_t w)
            {
                // If Disabled, return original function.
                if (!EnableStatScaling)
                    { return true; }

                // Get the Main Process Data.
                nbMainProcessData_t data = nbMainProcess.nbGetMainProcessData();

                // Get Demon's Devil Format
                datDevilFormat_t devil = datDevilFormat.Get(w.id, true);

                // Get the Player's Current level.
                int level = datCalc.datGetPlayerLevel();

                // Get the difference in level count.
                int leveldiff = level - devil.level;

                // Get the Demon's Experience
                int exp = devil.dropexp;

                // Get the Demon's Macca
                int macca = devil.dropmakka;

                // Get the Demi-Fiend's current Luck and do some math for scaling Drop Rates.
                float dropRateMult = EnableStatScaling ? 0.75f + ((float)Math.Clamp(datCalc.datGetParam(dds3GlobalWork.DDS3_GBWK.unitwork[0], 5), 0, MAXSTATS) / STATS_SCALING) : 1f;

                // If the enemy has an Item.
                int droppedItem = 0;

                // Unseeded random number generator
                System.Random rng = new();

                // If the Level Difference is not zero.
                if (leveldiff != 0)
                {
                    // If the Level Difference is negative, gain more Experience.
                    if (leveldiff < 0)
                        { exp = (int)((float)exp * (1f + Math.Min(1.5f, 1f + ((float)Math.Abs(leveldiff) - 2) * 0.0278f))); }
                    
                    // Otherwise, gain less Experience.
                    else
                        { exp = (int)((float)exp / (1f + Math.Min(1.5f, 1f + ((float)Math.Abs(leveldiff) - 2) * 0.0278f))); }
                }

                // If the Difficulty is Merciful, massively increase gains.
                if (dds3ConfigMain.cfgGetBit(9) == 0)
                {
                    macca *= 5;
                    exp *= 3;
                }

                // If in the Boss Rush, massively reduce your gains, add them to the encounter's data, and return.
                if (datCalc.datBossRashChk(data.encno) != 0)
                {
                    macca /= 10;
                    exp /= 10;

                    data.maka += (uint)macca;
                    data.exp += (uint)exp;

                    return false;
                }

                // Add the new Macca and EXP values to the encounter's data.
                data.maka += (uint)macca;
                data.exp += (uint)exp;

                // Check the Drop Chance
                int chance = rng.Next(100);

                // Keep looping until it finds an item and attempts to drop it.
                do
                {
                    // If items exist, continue.
                    bool found = false;
                    for (int i = 0; i < devil.dropitem.Length; i++)
                    {
                        if (devil.dropitem[i] != 0)
                        { found = true; break; }
                    }

                    // Grab an item.
                    int newItem = rng.Next(0, devil.dropitem.Length - 1);

                    // If it's not an item in the list, continue.
                    if (devil.dropitem[newItem] == 0 && found == true)
                    { continue; }

                    // If you meet the Drop Chance, grab this particular item and break.
                    // If the item ID is zero, skip to the break.
                    if (devil.dropitem[newItem] != 0 && (float)devil.droppoint[newItem] * dropRateMult >= chance)
                    {
                        MelonLogger.Msg("Dropped Item Name:" + datItemName.Get(devil.dropitem[newItem]));
                        droppedItem = devil.dropitem[newItem];
                    }

                    break;
                }
                while (true);

                // If this weird variable is 0 or an EventBit Check is true and the enemy has a special item.
                if (devil.specialbit == 0 || (EventBit.evtBitCheck(devil.specialbit) && devil.specialitem != 0))
                {
                    if ((float)devil.specialpoint >= chance)
                    { droppedItem = devil.specialitem; }
                }

                if (droppedItem == 0)
                    { return false; }

                // Check if a Bead drops.
                chance = rng.Next(100);
                bool foundBead = false;
                if ((float)devil.hougyokupoint * dropRateMult >= chance)
                { foundBead = true; }

                // Check if a LifeStone drops.
                chance = rng.Next(100);
                bool foundLifeStone = false;
                if ((float)devil.masekipoint * dropRateMult >= chance)
                { foundLifeStone = true; }

                // Loop through the Data's Item list
                for (int i = 0; i < data.item.Length; i++)
                {
                    // Add a new Item to the table.
                    if (data.item[i] == 0 && droppedItem != 0)
                    {
                        data.item[i] = (byte)droppedItem;
                        data.itemcnt[i] += 1;
                    }
                    // Add to an existing Item's count.
                    if (data.item[i] == droppedItem && droppedItem != 0)
                    {
                        data.itemcnt[i] += 1;
                    }
                    // If no item was found, check for Bead and Life Stones.
                    if (droppedItem == 0)
                    {
                        if (foundBead)
                        { droppedItem = 4; foundBead = false; }

                        else if (foundLifeStone)
                        { droppedItem = 3; foundLifeStone = false; }
                    }
                }

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

                // This value changes the damage output slightly.
                int scale = 48;

                // This eventually becomes the final damage value.
                int finalvalue = 0;

                // Do some initial math for basic attacks.
                finalvalue = (int)(((datCalc.datGetNormalAtkPow(attacker) * 2) * 1.33f) * 0.8f);

                // If you're not doing a basic attack, then use the Physical Skill formula.
                // Additionally change the scale value.
                // Note that "waza" is Skill Power.
                if (nskill != 0)
                {
                    finalvalue = (int)((float)datCalc.datGetNormalAtkPow(attacker) * (float)waza * 2 / 23.2f * 0.8f);
                    scale = 50;
                }

                // Use that number and the attacker's level to figure out some damage reduction.
                int reduction = (int)((float)scale / (float)(attacker.level + 10));

                // If enabled, use a new formula based on the defender's level instead.
                // I'm using a different formula because the above formula reduces the reduction with each level and I need to increase it instead.
                if (EnableStatScaling)
                    { reduction = (int)((float)(scale + (float)defender.level * 2f) / 10f); }

                // The final value is cut down to 60% and reduced by the above reduction formula.
                finalvalue = (int)((float)finalvalue * 0.6f - (float)reduction);

                // Additionally, scale the previous result down to 70%.
                __result = (int)((float)finalvalue * 0.7f);

                // If the difficulty bit is 3 and Event Bit 0x8a0 is true, multiply damage by 134%.
                // Otherwise, damage is normal.
                if (dds3ConfigMain.cfgGetBit(9) == 3 && !EnableStatScaling)
                {
                    __result = (int)(finalvalue * 1.34f);
                    if (!EventBit.evtBitCheck(0x8a0))
                        { __result = finalvalue; }
                }

                // If difficulty is 2, it's also multipled by 134%.
                // Otherwise, it's just normal.
                else
                {
                    __result = finalvalue;
                    if (dds3ConfigMain.cfgGetBit(9) == 2 && !EnableStatScaling)
                        { __result = (int)(finalvalue * 1.34); }
                }

                // This multiplies the final result by the attacker's attack buffs and the defender's Defense Buffs.
                __result = (int)((float)__result * nbCalc.nbGetHojoRitu(sformindex, 4) * nbCalc.nbGetHojoRitu(dformindex, 7));

                // If enabled, introduce some further damage mitigation
                if (EnableStatScaling)
                    { __result = (int)((float)__result * DamageMitigation.Get(defender, 3)); }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMaxHpWazaPoint))]
        private class PatchGetHPPow
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                // Result init.
                __result = 0;

                // If we're not scaling things differently, skip this function entirely.
                if (!EnableStatScaling)
                    { return true; }

                // Set up the attacker and defender objects from the form indices.
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(dformindex);
                
                // Grab the skill's HP Cost. It's gonna be an HP cost in this formula, so no need to check above.
                int hpCost = datNormalSkill.tbl[nskill].cost;

                // This formula uses your Current HP, plus the cost of the Skill, divided by your maximum HP to determine how strong it is.
                // If you're at Maximum HP when casting, you deal full damage.
                // If you're at very low HP when casting, you deal half as much damage.
                __result = (int)(((0.5f + 0.5f * ((float)attacker.hp + (float)hpCost) / attacker.maxhp)) * waza * 0.0114f * nbCalc.nbGetHojoRitu(sformindex, 4) * nbCalc.nbGetHojoRitu(dformindex, 7));
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetMagicAttack))]
        private class PatchGetMagicPow
        {
            private static bool Prefix(out int __result, int nskill, int sformindex, int dformindex, int waza)
            {
                // Result init.
                __result = 0;

                // If this skill doesn't deal damage, skip this function altogether.
                if (datSkill.tbl[nskill].skillattr > 12)
                    { return true; }

                // Set up the attacker/defender objects from the indices.
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);

                // Hit Count Maximum Check
                int maxhits = datNormalSkill.tbl[nskill].targetcntmax - (datNormalSkill.tbl[nskill].targetcntmin - 1);

                // There's a Level Limit for Magic Skills normally.
                int LevelLimit = attacker.level;

                // Magic Skills additionally have a Base Power and Magic Damage Limit.
                int skillLimit = datNormalSkill.tbl[nskill].magiclimit;
                int skillBase = datNormalSkill.tbl[nskill].magicbase;

                // This formula is the base game's Skill peak formula.
                float skillPeak = ((float)skillLimit - (float)skillBase) / (float)waza * (255f / 24f);

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
                int param = Math.Clamp(datCalc.datGetParam(attacker, 2), 0, MAXSTATS);

                // If Enabled, grab Int instead.
                if (EnableIntStat)
                    { param = Math.Clamp(datCalc.datGetParam(attacker, 1), 0, MAXSTATS); }

                // If enabled, perform some new math.
                // Otherwise, use the game's normal formula.
                if (EnableStatScaling)
                    { damageCalc = (int)(((float)waza + (float)skillPeak) * ((float)attacker.level / 2f + (float)param / STATS_SCALING * 2f) / 25.5f); }
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
                if ((attacker.flag >> 5 & 1) != 0 && !EnableStatScaling)
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

                // Multiply the final value by the attacker's Magic buffs and the defender's Defense buffs.
                __result = (int)(damageCalc2 * nbCalc.nbGetHojoRitu(sformindex, 5) * nbCalc.nbGetHojoRitu(dformindex, 7));

                // If enabled, add some more damage mitigation based on the defender's Mag and scale the original result by a hitcount parameter.
                if (EnableStatScaling)
                    { __result = (int)((float)__result / (maxhits > 1 && datNormalSkill.tbl[nskill].targetrandom > 0 ? (float)maxhits / 2f : 1) * DamageMitigation.Get(defender, 2)); }
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
                int param = Math.Clamp(datCalc.datGetParam(work, 2), 0, MAXSTATS);

                // If enabled, get Int instead.
                if (EnableIntStat)
                    { param = Math.Clamp(datCalc.datGetParam(work, 1), 0, MAXSTATS); }

                // Final result considers your Magic buffs.
                __result = (int)(nbCalc.nbGetHojoRitu(sformindex, 5) * ((float)rng.Next(0, 8) + (float)param * 4f / (EnableStatScaling ? (float)STATS_SCALING : 1f) + (float)work.level) / 10f * (float)waza);
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

                // Get the above demon's Luc.
                int luck = Math.Clamp(datCalc.datGetParam(w, 5), 0, MAXSTATS);

                // Grab Demi-Fiend's Luc.
                int playerLuck = Math.Clamp(datCalc.datGetParam(work, 5), 0, MAXSTATS);

                // Grab the player's total Macca.
                int macca = dds3GlobalWork.DDS3_GBWK.maka;
                int baseMacca = macca;

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

                // Flag nonsense again.
                if ((w.flag >> 5 & 1) == 0)
                {
                    // Do some math for the base formula.
                    baseform = Mathf.Abs((float)luck / ((float)w.level / 5.0f + 4.0f));

                    // If enabled, do a different one.
                    if (EnableStatScaling)
                        { baseform = ((float)w.level / 2.55f + (float)luck) / 50f; }

                    // If you're under 1/1000, just set the adjustment to zero.
                    if (baseform < 0.001f)
                        { adjform = 0; }

                    // Otherwise, grab a small segment of your Macca.
                    else
                    {
                        adjform = (float)macca / 20.0f * baseform;

                        // If enabled, use 1/10 of your Macca instead of a 1/20.
                        // Also, clamp it to as low as 1/10 of your total Macca and as high as your entire stack.
                        if (EnableStatScaling)
                            { adjform = (float)Math.Clamp((float)macca * baseform, (float)macca / 10f, (float)macca); }
                    }
                }

                // If that flag was false.
                // Effectively this means the demon isn't on your team.
                else
                {
                    // Base formula math again.
                    baseform = Mathf.Abs((float)playerLuck / ((float)work.level / 5.0f + 4.0f));

                    // If enabled, scale differently.
                    if (EnableStatScaling)
                        { baseform = ((float)work.level / 2.55f + (float)playerLuck) / 50f; }

                    // Grab the enemy's whole stack.
                    // Also make sure you don't accidentally generate more (or less) Macca than intended.
                    adjform = (float)Math.Clamp(devil.dropmakka * baseform, devil.dropmakka / 10f, devil.dropmakka);
                }

                // Generate a number between 0.0 and 1.0.
                float variance = (float)rng.NextDouble();

                // Do some math and return the result later.
                __result = (int)Mathf.Abs((variance - 0.5f) * 2f * 0.1f * ((float)adjform * 2.0f));

                // If enabled, scale it differently.
                if (EnableStatScaling)
                    { __result = (int)Math.Clamp((variance + 0.5f) * adjform, 0d, (w.flag >> 5 & 1) == 0 ? macca : devil.dropmakka); }

                // If difficulty bit is 1 or lower and some more flag nonsense, divide by 10.
                if (dds3ConfigMain.cfgGetBit(9) <= 1 && (w.flag & 0x20) == 0)
                    { __result /= 10; }

                // If result is less than 1, set it to zero.
                if (__result < 1)
                    { __result = 0; }
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
                __result = (int)((float)work.level + (float)Math.Clamp(datCalc.datGetParam(work, 4), 0, MAXSTATS) * 2f / (EnableStatScaling ? STATS_SCALING : 1f));

                // Grabs the user's Luc.
                int luc = (int)((float)Math.Clamp(datCalc.datGetParam(work, 5), 0, MAXSTATS) / (EnableStatScaling ? STATS_SCALING : 1f));

                // If it's under 2 or some weird "badstatus" flag nonsense is true, set Luc to 1.
                if (luc < 2 || (work.badstatus & 0xFFF) == 0x200)
                    { luc = 1; }

                // Adjust the Result by some additional math found in the original formula.
                __result += 5;
                __result = (int)((float)__result * 1.5f);

                // Add Luc + 10 to the result.
                __result += luc + 10;
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
                __result = (int)((float)work.level + ((float)Math.Clamp(datCalc.datGetParam(work, 2), 0, MAXSTATS) * 2f + (float)Math.Clamp(datCalc.datGetParam(work, 4), 0, MAXSTATS) * 2f) / (EnableStatScaling ? STATS_SCALING : 1f));

                // If enabled, adds Int scaling.
                if (EnableIntStat)
                    { __result = (int)((float)work.level + ((float)Math.Clamp(datCalc.datGetParam(work, 1), 0, MAXSTATS) * 8f) / (EnableStatScaling ? STATS_SCALING : 1)); }

                // Grabs Luc.
                int luc = (int)((float)Math.Clamp(datCalc.datGetParam(work, 5), 0, MAXSTATS) / (EnableStatScaling ? STATS_SCALING : 1f));

                // If under 2 or flag nonsense, set Luc to 1.
                if (luc < 2 || (work.badstatus & 0xFFF) == 0x200)
                    { luc = 1; }

                // Adjust Luc by 5.
                // This was really stupid originally.
                int luckValue = luc + 5;

                // Bitshift the above result by 1 and add 15.
                __result += luckValue >> 1 + 0xf;
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
                __result = (Math.Clamp(datCalc.datGetParam(work, 3), 0, MAXSTATS) + work.level) * 2;

                // If enabled, do some actual math.
                if (EnableStatScaling)
                    { __result = (int)((float)Math.Clamp(datCalc.datGetParam(work, 3), 0, MAXSTATS) * 2f / (float)STATS_SCALING + (float)work.level) * 2; }
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetKoukaType))]
        private class PatchTargetHitRate
        {
            private static bool Prefix(int nskill, int sformindex, int dformindex, out int __result)
            {
                // Result init.
                __result = 0;

                // Grab the Process Data.
                nbMainProcessData_t a = nbMainProcess.nbGetMainProcessData();

                // Set up the attacker/defender objects from the indices.
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(dformindex);

                // "Aisyo" translates to "favorite book" with Google Translate.
                // I dug deeper through another website and found out it means something more like "charm" or "character".
                // Turns out this has to do with Affinities and Negotiations with said Demon. "Character" was a good assumption.
                uint aisyo = nbCalc.nbGetAisyo(nskill, dformindex, datSkill.tbl[nskill].skillattr);

                // If the Skill is a larger ID than the table size, return and do the original function instead.
                if (datSkill.tbl.Length <= nskill)
                { return true; }

                // If the Skill's Effect Type is not zero (Physical Damage), return and do the original function instead.
                if (datNormalSkill.tbl[nskill].koukatype != 0)
                { return true; }

                // If the Skill's "Program" is anything that isn't a normal Skill.
                if (datNormalSkill.tbl[nskill].program != 0)
                { return true; }

                // If EnableStatScaling is false, return and do the original function
                if (!EnableStatScaling)
                { return true; }

                // If the Defender Blocks, Repels, or Drains your attack, return the original function.
                if ((aisyo & 0x10000u) == 0x10000u || (aisyo & 0x100000u) == 0x100000u || (aisyo & 0x1000000u) == 0x1000000u)
                { return true; }

                // More flag nonsense.
                if (((a.stat + 1) >> 2 & 1) != 0 &&
                    (attacker.flag >> 5 & 1) != 0)
                {
                    // A bit more flag nonsense.
                    // If this all passes, return a result of 0xb (11).
                    if (((a.form[dformindex].stat + 1) >> 4 & 1) != 0)
                        { __result = 0xb; return false; }

                    // Checks if the Skill Attribute is a Utility Skill.
                    // Also checks if there's more than 1 enemy party I'm guessing. Not sure.
                    else if (datSkill.tbl[nskill].skillattr != 0xe && a.enemypcnt != 1)
                        { __result = 5; return false; }
                }

                // Grab the Skill's accuracy and do some math.
                float basepower = datNormalSkill.tbl[nskill].hitlevel * 0.01f;
                float multi = basepower;

                // Set the basepower to 1.
                basepower = 1f;

                // If Difficulty is specifically Hard, lower it to 0.7.
                if (dds3ConfigMain.cfgGetBit(9) == 2)
                    { basepower = 0.7f; }
                multi *= basepower;

                // I'm assuming these line up with Agi and Vit respectively.
                // I could be wrong, they might both be Agi.
                float atkBuffs = nbCalc.nbGetHojoRitu(sformindex, 8);
                float defBuffs = nbCalc.nbGetHojoRitu(dformindex, 6);

                // Grab both users' Agi and math out the difference.
                float atkAgiCalc = (float)Math.Clamp(datCalc.datGetParam(attacker, 4), 0, MAXSTATS) / (float)STATS_SCALING / ((float)defender.level / 5f + 3f);
                float defAgiCalc = (float)Math.Clamp(datCalc.datGetParam(defender, 4), 0, MAXSTATS) / (float)STATS_SCALING / ((float)attacker.level / 5f + 3f);

                // Calculate the overall hit chance.
                float hitChanceCalc = multi * atkBuffs * defBuffs * ((defAgiCalc - atkAgiCalc) * 6.25f + (100 - nbCalc.GetFailpoint(nskill)));

                // Drop the attacker's hit chance to 25% if you have whatever status byte this is.
                if ((attacker.badstatus & 0xFFF) == 0x100)
                    { hitChanceCalc *= 0.25f; }

                // Make sure it's a maximum of 95% to miss.
                if (hitChanceCalc >= 95.0f)
                    { hitChanceCalc = 95.0f; }

                // If you have any of these statuses, you can't dodge.
                if ((defender.badstatus & 0xFFF) == 1 ||
                    (defender.badstatus & 0xFFF) == 2 ||
                    (defender.badstatus & 0xFFF) == 4 ||
                    (defender.badstatus & 0xFFF) == 8 ||
                    (defender.badstatus & 0xFFF) == 0x10)
                        { hitChanceCalc = 100.0f; }

                // Check hit chance against a random integer from 0 to 99.
                // If you don't hit, set the result to zero.
                System.Random rng = new();
                int hitCheck = (int)(rng.NextDouble() * 100f);
                if (hitCheck < hitChanceCalc)
                    { __result = 0; return false; }

                // Whatever this "Devil Format Flag" is, if it's not zero, return a different result.
                if ((nbCalc.nbGetDevilFormatFlag(dformindex) & 0x800) != 0)
                    { __result = 5; return false; }
                
                return false;
            }
        }

        [HarmonyPatch(typeof(nbCalc), nameof(nbCalc.nbGetHitType))]
        private class PatchGetHitType
        {
            private static bool Prefix(out int __result, nbActionProcessData_t ad, int nskill, int sformindex, int dformindex)
            {
                // Hit Types are as followed:
                // 0 -> Normal
                // 1 -> Critical
                // 2 -> Weakness

                // Grab units from form indices.
                datUnitWork_t attacker = nbMainProcess.nbGetUnitWorkFromFormindex(sformindex);
                datUnitWork_t defender = nbMainProcess.nbGetUnitWorkFromFormindex(dformindex);

                // Result init.
                __result = 0;

                // "Aisyo" translates to "favorite book" with Google Translate.
                // I dug deeper through another website and found out it means something more like "charm" or "character".
                // Turns out this has to do with Affinities and Negotiations with said Demon. "Character" was a good assumption.
                uint aisyo = nbCalc.nbGetAisyo(nskill, dformindex, datSkill.tbl[nskill].skillattr);

                // If false, return original function.
                if (!EnableStatScaling)
                { return true; }

                // Flag Nonsense. If true, return original function.
                if ((byte)datSkill.tbl[nskill].skillattr == 0xff)
                { return true; }

                // If the skill is Magic, return original function.
                if (datNormalSkill.tbl[nskill].koukatype == 1)
                { return true; }

                // If this skill is just the basic attack
                if (nskill == 0)
                {
                    // I'm assuming this is some sort of passive or something.
                    // Google Translate says that "hatsudo" means "each time". Is that the same phrase?
                    if (nbCalc.nbHatudoCheckSkill(attacker, 300) != 0)
                        { ad.autoskill = 300; __result = 1; return false; }

                    // Doing it again, but with the next skill.
                    if (nbCalc.nbHatudoCheckSkill(attacker, 301) != 0)
                        { ad.autoskill = 301; __result = 1; return false; }
                }

                // Seriously, Nocturne, why are you like this???
                // For the record, unsigned integers can't go below 0, so it actively converts it to an int so it can.
                if ((int)aisyo < 0)
                    { __result = 2; return false; }

                // Set the party object from the attacker.
                nbParty_t party = nbMainProcess.nbGetPartyFromFormindex(sformindex);

                // Flag stuff.
                if (((party.flag + 1) & 1) != 0)
                    { __result = 1; return false; }

                // A random boolean to check on things later.
                bool chk;

                // Infinite Loop Check.
                // Because what the fuck.
                bool wtf = false;

                // Base Crit Chance value.
                float val = 1.0f;

                // Yes, I am labeling this section "WhatTheFuck".
                // Go ahead and read it.
                // You'll understand.
            WhatTheFuck:

                // Check yet more flag nonsense.
                if ((((ad.data.form[dformindex].stat + 1) >> 5) & 1) == 0 || wtf)
                {
                    // If the currently used Skill's ID isn't zero.
                    if (nskill != 0)
                        { chk = false; }

                    // Otherwise, do some other stuff.
                    else
                    {

                        // Some random Skill check.
                        // TURNS OUT THIS IS MIGHT SO IT ADJUSTS CRIT CHANCE. THANKS GAME.
                        if (datCalc.datCheckSyojiSkill(attacker, 299) == 0)
                            { chk = false; }
                        
                        // If the above check fails, do some wonky math.
                        else
                        {
                            // The variables are actually single characters.
                            // They're all short integers.
                            // This tells me nothing as to how they function.
                            // Additionally, ghidra doesn't tell me which one to actually use.
                            // I'm taking a blind guess here.
                            if (datSpecialSkill.tbl[nskill].a < 0xc)
                                { __result = 0; return false; }

                            // I can't believe this is making me double-check so hard what goes where.
                            val = datSpecialSkill.tbl[nskill].n / 100f;
                            
                            // Set check to true, finally.
                            chk = true;
                        }
                    }
                }

                // Okay, if the above fuckfest doesn't get called, do the following.
                else
                {
                    // If the Difficulty Bit is over zero, go up and do the previous section.
                    // Seriously. What the fuck Nocturne.
                    if (dds3ConfigMain.cfgGetBit(9) > 0)
                    {
                        // If it's above 1, change the value.
                        if (dds3ConfigMain.cfgGetBit(9) > 1)
                            { val = 1f; }

                        // Make sure this is true so it doesn't loop forever.
                        wtf = true;

                        // Jump back up.
                        goto WhatTheFuck;
                    }

                    // Value adjustment.
                    val = 0.7f;

                    // If you're using a normal attack, jump back up.
                    if (nskill == 0)
                        { wtf = true; goto WhatTheFuck; }

                    // Set check to false.
                    chk = false;
                }
                
                // More flag stuff.
                // Sets the above value to something.
                if ((defender.badstatus & 0xFFF) == 1 || (defender.badstatus & 0xFFF) == 2 || (defender.badstatus & 0xFFF) == 0x10 || (defender.badstatus & 0xFFF) == 4)
                    { val = 100f; }

                // Set Attacker's Crit Chance values.
                float atkCritLevel = (float)attacker.level / 5f + 3f;
                float atkCritStat = (float)Math.Clamp(datCalc.datGetParam(attacker, 5), 0, MAXSTATS) / (float)STATS_SCALING;
                float atkCritChance = 0f;

                // Set Defender's Crit Chance values.
                float defCritLevel = (float)defender.level / 5f + 3f;
                float defCritStat = (float)Math.Clamp(datCalc.datGetParam(defender, 5), 0, MAXSTATS) / (float)STATS_SCALING;
                float defCritChance = 0f;

                // Divide the Crit Chances by the opposite levels.
                if (defCritLevel != 0f)
                    { atkCritChance = atkCritStat / defCritLevel; }
                if (atkCritLevel != 0f)
                    { defCritChance = defCritStat / atkCritLevel; }

                // Set total Crit Value.
                float critValue = (atkCritChance - defCritChance) * 6.25f + (100f - nbCalc.GetFailpoint(nskill));

                // Adjust the Crit Value.
                critValue = val * (critValue / 100f) * datNormalSkill.tbl[nskill].criticalpoint;

                // Generate a random interger and compare to the Crit Value.
                // If it's higher, it's a crit.
                System.Random rng = new();
                if (rng.Next(100) < critValue)
                    { __result = 1; return false; }

                // Finally reference the above check value.
                // And all it's used for is to set the autoskill value to 299.
                // As noted above, this is Might, so it modified crit chance.
                // God damnit.
                if (chk)
                    { ad.autoskill = 299; }

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
                // Additionally return if the Skill doesn't deal damage.
                // Also return if EnableStatScaling is false.
                if (datNormalSkill.tbl[nskill].targetrandom < 1 || datSkill.tbl[nskill].skillattr > 12 || effectlist.Length == 0 || !EnableStatScaling)
                    { return true; }

                // Loop through the Effect List and clear it.
                int i = 0;
                do
                {
                    // If this somehow happened, we've got a problem, so just return.
                    if (effectlist[i].Length < 2)
                        { return true; }

                    // Reset both values.
                    // The first value becomes an existing character in the battlefield later. For now, it's set to nothing.
                    // The second value becomes 0. It's how many hits they receive.
                    effectlist[i][0] = -1;
                    effectlist[i][1] = 0;
                    i++;
                }
                while (i < a.timelist.Length);

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

                // Unseeded rng.
                System.Random rng = new();

                // Get user's unit from form index.
                datUnitWork_t user = nbMainProcess.nbGetUnitWorkFromFormindex(a.form.formindex);

                // Calculate addition maximum hit count based on Skill's Rank and the user's Agi or Luc, depending on Skill Type.
                int extrahits = 0;

                // Physical Attacks (Agi Scaling)
                // 12 is Shot in Insaniax.
                // Otherwise Talk skills will be affected.
                // Which they shouldn't since they don't target randomly.
                if (datNormalSkill.tbl[nskill].koukatype == 0)
                    { extrahits = (int)Math.Max((float)Math.Clamp(datCalc.datGetParam(user, 4), 0, MAXSTATS) / (5f * STATS_SCALING), 0f); }

                // Magic Attacks (Luc Scaling)
                else if (datNormalSkill.tbl[nskill].koukatype == 1)
                    { extrahits = (int)Math.Max((float)Math.Clamp(datCalc.datGetParam(user, 5), 0, MAXSTATS) / (5f * STATS_SCALING), 0f); }

                // Subtract by an eigth of the Skill's Rank, rounded up
                extrahits -= (int)Math.Ceiling((double)tblKeisyoSkillLevel.fclKeisyoSkillLevelTbl[nskill].Level / 8d);

                // If it somehow goes under, set it to zero.
                if (extrahits < 0)
                    { extrahits = 0; }

                // Calculate odds of being hit by this particular skill based on its maximum hit count.
                // Additionally, cap the odds at min 25% and max 70%.
                sbyte hitOdds = (sbyte)Math.Clamp(datNormalSkill.tbl[nskill].targetcntmax * 10 + extrahits * 5, 25, 70);

                // Set the max hit count for a single target based on the average hit count
                byte maxhits = (byte)((datNormalSkill.tbl[nskill].targetcntmax + datNormalSkill.tbl[nskill].targetcntmin + extrahits) / 2);

                // Sets the minimum hit count before reductions start applying.
                byte minhits = (byte)Math.Max(datNormalSkill.tbl[nskill].targetcntmin, maxhits / 3);

                // Randomly set the hitcount.
                int hitcount = rng.Next(datNormalSkill.tbl[nskill].targetcntmax + extrahits) + 1;

                // Loop through the currently available units in the Timelist and insert them into the Effectlist.
                int foundcount = 0;
                for (i = 0; i < a.timelist.Length; i++)
                {
                    // If the unit is not in the attacker's party and also isn't dead, add them to the Effectlist.
                    if ((
                        (a.data.form[i].formindex < 4 && a.form.formindex >= 4) ||
                        (a.data.form[i].formindex >= 4 && a.form.formindex < 4)) &&
                        a.timelist[i].hp >= 1)
                            { effectlist[foundcount++][0] = (sbyte)a.data.form[i].formindex; }
                }

                // Set The Target List
                TargetHitCountManager.SetEffectList(effectlist);

                // Clear out all of the invalid targets from the Target List.
                for (i = 0; i < TargetHitCountManager.targetData.Length; i++)
                {
                    if (TargetHitCountManager.targetData[i][0] < 0)
                        { TargetHitCountManager.RemoveTarget(i); }
                }

                // Loop through the Hit Count and hit the targets.
                i = 0;
                while (i < hitcount)
                {
                    // Remove Dead Targets from the Target List.
                    TargetHitCountManager.RemoveDeadTargets();

                    // Remove Targets that've been hit enough from the Target List.
                    TargetHitCountManager.RemoveMaxHitTargets(maxhits);

                    // If nothing is left to hit, break.
                    if (TargetHitCountManager.TotalHitChanceCalc() == 0)
                        { break; }

                    // Grab the Target List for reference.
                    List<sbyte> possibleTargets = TargetHitCountManager.CreateTargetList();

                    // If this list is entirely empty, just break the loop.
                    if (possibleTargets.Count == 0)
                        { break; }

                    // Grab a random target ID
                    sbyte target = possibleTargets[rng.Next(possibleTargets.Count)];

                    // Grab their current chance to be hit.
                    sbyte consecutiveOdds = TargetHitCountManager.targetData[target][2];

                    // If we hit the minimum hit count threshold and the target is still viable to be hit.
                    if (consecutiveOdds < 100 && TargetHitCountManager.targetData[target][1] >= minhits)
                    {
                        // Check if we hit yet again.
                        int rollForHit = rng.Next(100);
                        if (rollForHit >= TargetHitCountManager.targetData[target][2])
                        {
                            // If we don't, remove the target and continue the loop.
                            TargetHitCountManager.RemoveTarget(target);
                            continue;
                        }
                    }

                    // Add a hit to the target for hit count checking.
                    // Additionally, this adjusts their chance to be hit to a new value if they exceed the minimum hit count.
                    TargetHitCountManager.HitTarget(target, minhits, hitOdds);

                    // Increment.
                    i++;
                }

                // Clear out the Targets that have no hits by removing their ID.
                TargetHitCountManager.ClearNoHitTargets();

                // Clear out the Effect List
                for (i = 0; i < effectlist.Length; i++)
                {
                    effectlist[i][0] = -1;
                    effectlist[i][1] = 0;
                }

                // Check for existing targets and populate the Effect List.
                foundcount = 0;
                for (i = 0; i < TargetHitCountManager.targetData.Length; i++)
                {
                    if (TargetHitCountManager.targetData[i][0] > -1 && TargetHitCountManager.targetData[i][1] > 0)
                    {
                        effectlist[foundcount][0] = TargetHitCountManager.targetData[i][0];
                        effectlist[foundcount][1] = TargetHitCountManager.targetData[i][1];
                        foundcount++;
                    }
                }

                // Clear the Target List.
                TargetHitCountManager.SetEffectList(null);

                // At this point we're good, so replace the original list.
                koukalist = effectlist;
                return false;
            }
        }
    }
}
