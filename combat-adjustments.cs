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

                // The final value is cut down to 60%.
                finalvalue = (int)((float)finalvalue * 0.6f);

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
                if (dds3ConfigMain.cfgGetBit(9) <= 1 && (w.flag & 0x20) == 0)
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
                { __result = work.level + (datCalc.datGetParam(work, 4) * 2 + luc) / POINTS_PER_LEVEL + 10; }
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
    }
}
