using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2Cppnewdata_H;

namespace ModernStatsSystem
{
    internal partial class ModernStatsSystem : MelonMod
    {
        [HarmonyPatch(typeof(datCalc), nameof(datCalc.datExecSkill))]
        private class PatchCursedGospelEffect
        {
            private static bool Postfix(ref int nskill)
            {
                // If this Skill is from the Cursed Gospel item mod.
                // If it isn't, return and run the original function.
                if (nskill != 95)
                    { return true; }

                // Grab Demi-Fiend and check his Level.
                // If it's 1 or lower, return and run the original function.
                datUnitWork_t work = dds3GlobalWork.DDS3_GBWK.unitwork[0];
                if (work.level <= 1)
                    { return true; }
                
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
                return false;
            }
        }
    }
}