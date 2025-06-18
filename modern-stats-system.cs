using HarmonyLib;
using Il2Cpp;
using Il2Cppcamp_H;
using Il2Cppnewbattle_H;
using Il2Cppnewdata_H;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(ModernStatsSystem.ModernStatsSystem), "Modern Stats System", "1.1.1.0", "X Kirby")]
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
        
        // Hard-coded Intelligence Value list for every demon in the game
        private static sbyte[] DemonIntTable = {
            2,  // Dummy
            // Deity
            84, // Vishnu
            58, // Mitra
            58, // Amaterasu
            62, // Odin
            35, // Atavaka
            44, // Horus
            // Megami
            50, // Lakshmi
            58, // Scathach
            38, // Sarasvati
            43, // Sati
            27, // Ame-no-Uzume
            // Fury
            61, // Shiva
            56, // Beidou Xingjun
            33, // Qitian Dasheng
            27, // Dionysus
            // Lady
            41, // Kali
            60, // Skadi
            52, // Parvati
            45, // Kushinada
            33, // Kikuri-Hime
            // Kishin
            31, // Bishamonten
            37, // Thor
            27, // Jikokuten
            30, // Take-Mikazuchi
            35, // Okuninushi
            28, // Koumokuten
            23, // Zouchouten
            19, // Take-Minakata
            // Holy
            30, // Chimera
            22, // Baihu
            25, // Senri
            28, // Zhuque
            15, // Shiisaa
            22, // (Avatar) Xiezhai (List Order not completely sorted by Race.)
            22, // Unicorn
            // Element
            21, // Flaemis
            22, // Aquans
            15, // Aeros
            11, // Erthys
            // Mitama
            18, // Saki Mitama
            24, // Kishi Mitama
            32, // Nigi Mitama
            27, // Ara Mitama
            // Yoma
            49, // Efreet
            34, // Pulukishi
            16, // Ongkhot
            41, // Jinn
            25, // Karasu Tengu
            29, // Dis
            15, // Isora
            11, // Asparas
            23, // Koppa Tengu
            // Fairy
            45, // Titania
            35, // Oberon
            24, // Troll
            22, // Setanta
            22, // Kelpie
            19, // Jack-o'-Lantern
            10, // High Pixie
            11, // Jack Frost
            10, // Pixie
            // Divine
            52, // Throne
            38, // Dominion
            35, // Virtue
            19, // Power
            21, // Principality
            13, // Archangel
            12, // Angel
            // Fallen
            32, // Flauros
            48, // Decarabia
            24, // Ose
            26, // Berith
            21, // Eligor
            15, // Forneus
            // Snake
            38, // Yurlungur
            29, // Quetzalcoatl
            24, // Naga Raja
            29, // Mizuchi
            20, // Naga
            16, // Nozuchi
            // Beast
            36, // Cerberus
            25, // Orthrus
            35, // Suparna
            16, // Badb Catha
            17, // Inugami
            26, // Nekomata
            // Jirae
            31, // Gogmagog
            24, // Titan
            17, // Sarutahiko
            13, // Sudama
            9,  // Hua Po
            6,  // Kodama
            // Brute
            43, // Shiki-Ouji
            13, // Oni
            34, // Yomotsu-Ikusa
            11, // Momunofu
            8,  // Shikigami
            // Femme
            43, // Rangda
            26, // Dakini
            23, // Yaksini
            29, // Yomotsu-Shikome
            14, // Taraka
            12, // Datsue-Ba
            // Vile
            47, // Mada
            37, // Girimekhala
            44, // Taotie
            35, // Pazuzu
            32, // Baphomet
            // Tyrant
            69, // Mot (Nice.)
            55, // Aciel
            48, // Surt
            53, // Abaddon
            45, // Loki
            // Night
            61, // Lilith
            58, // Nyx
            46, // Queen Mab
            37, // Succubus
            31, // Incubus
            11, // Fomorian
            12, // Lilim
            // Wilder
            51, // Hresvelgr
            34, // Mothman
            23, // Raiju
            23, // Nue
            11, // Bicorn
            9,  // Zhen
            // Haunt
            46, // Vetala
            28, // Legion
            17, // Yaka
            6,  // Choronzon
            5,  // Preta
            // Foul
            48, // Shadow
            14, // Black Ooze
            9,  // Blob
            9,  // Slime
            10, // Mou-Ryo
            7,  // Will o' Wisp
            // Seraph
            68, // Michael
            64, // Gabriel
            59, // Raphael
            54, // Uriel
            // Wargod
            45, // Ganesha
            31, // Valkyrie
            17, // (Vile) Arahabaki
            // Genma
            39, // Kurama Tengu
            29, // Hanuman
            39, // Cu Chulainn
            // Dragon
            28, // Qing Long
            25, // Xuanwu
            // Avatar
            55, // Barong
            25, // Makami
            45, // (Avian) Garuda
            41, // Yatagarasu
            // Raptor
            48, // Gurulu
            // Entity
            47, // Albion
            // Manikins
            5,
            5,
            5,
            5,
            5,
            48, // (Vile) Samael
            // More Manikins
            5,
            5,
            5,
            5,
            5,
            // Summonable Minibosses
            25, // Pisaca
            42, // Kaiwan
            34, // Kin-Ki
            46, // Sui-Ki
            40, // Fuu-Ki
            62, // Ongyo-Ki
            41, // Clotho
            49, // Lachesis
            52, // Atropos
            41, // Loa
            21, // Chatterskull
            31, // Phantom
            // Unused Data
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            // Summonable Bosses
            47, // Raidou/Dante
            73, // Metatron
            72, // Beelzebub (Fly)
            54, // Pale Rider
            40, // White Rider
            47, // Red Rider
            53, // Black Rider
            23, // Matador
            33, // Hell Biker
            37, // Daisoujou
            56, // Mother Harlet
            70, // Trumpeter
            48, // Futomimi
            37, // Sakahagi
            53, // Black Frost
            62, // Beelzebub (Man)
            // More Unused Data
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            // Some more Unused Data
            // According to the Amicitia Wiki, these demons have japanese name entries.
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            // Even MORE Unused Data
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            // Actual Bosses
            23, // Forneus
            4,  // Specter
            80, // Ahriman
            55, // Noah
            28, // Incubus
            18, // Koppa Tengu
            30, // Kaiwan (Probably unused)
            16, // Ose
            60, // Kagutsuchi
            38, // Mizuchi
            20, // Kin-Ki
            68, // Sui-Ki
            55, // Fuu-Ki
            43, // Ongyo-Ki
            47, // Clotho
            48, // Lachesis
            66, // Atropos
            30, // Specter (There's a bunch of entries for Specter. I'm guessing they're all different.)
            55, // Girimekhala
            25, // Specter
            81, // Aciel
            32, // Skadi
            50, // Albion
            22, // Urthona
            22, // Urizen
            22, // Luvah
            22, // Tharmus
            15, // Futomimi
            38, // Gabriel
            50, // Raphael
            48, // Uriel
            52, // Samael
            65, // Baal Avatar
            28, // Ose Hallel
            26, // Flauros Hallel
            55, // Ahriman (Phase 2?)
            55, // Noah (Phase 2?
            70, // Kagatsuchi (Phase 2?)
            28, // Specter
            2,  // Specter
            2,  // Specter
            80, // Mizuchi (Uhh, when does this one show up?)
            40, // Reserve (Not sure if an actual boss or just some leftover data)
            48, // Sakahagi
            34, // Orthrus
            70, // Yaksini
            66, // Thor
            70, // Black Frost
            5,  // Karasu Tengu 1
            5,  // Karasu Tengu 2
            5,  // Karasu Tengu 3
            17, // Eligor 1
            17, // Eligor 2
            17, // Eligor 3
            20, // Kelpie 1
            35, // Kelpie 2
            20, // Berith
            24, // Succubus
            2,  // High Pixie (Probably unused)
            30, // Kaiwan (Probably unused)
            18, // Nekomata
            26, // Troll
            5,  // Will o' Wisp
            5,  // Preta
            47, // Bishamonten
            81, // Mara
            80, // Bishamonten (80 in all stats)
            80, // Jikokuten
            80, // Koumokuten
            80, // Zouchouten
            50, // Clotho
            54, // Lachesis
            66, // Atropos
            42, // Mitra
            80, // Masakado
            80, // Station Staff (Max stats for the station guy lol)
            80, // Loki (More than likely unused.)
            67, // Mada
            100,// Mot (He's gonna hit REALLY hard.)
            90, // Surt
            27, // Jack-o'-Lantern
            57, // Thor
            80, // Shadow (Unused probably)
            40, // Raidou 1
            48, // Raidou 2
            48, // Raidou 3
            72, // Metatron
            55, // Beelzebub (Fly)
            100,// Lucifer (I'm doing this so Mot isn't technically ahead of the final boss in terms of magic damage.)
            55, // Pale Rider
            33, // White Rider
            44, // Red Rider
            55, // Black Rider
            18, // Matador
            26, // Hell Biker
            49, // Daisoujou
            50, // Mother Harlet
            64, // Trumpeter
            80, // Futomimi (I'm assuming unused.)
            80, // Sakahagi (I'm assuming unused.)
            80, // Seven-Eye
            80, // Beelzebub (Man)
            50, // Loa
            38, // Virtue
            17, // Power
            20, // Legion
            // Last bit of Unused Data
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
        };

        // Menu manipulation variables
        private static bool SettingAsignParam;
        private static bool EvoCheck;

        public override void OnInitializeMelon()
        {
            // Grant Demi-Fiend the Intelligence Stat.
            if (EnableIntStat)
                { datHuman.datHumanUnitWork.param[1] = datHuman.datHumanUnitWork.param[2]; }

            // Alter Demi-Fiend's Base Stats then recalculate HP/MP.
            if (EnableStatScaling)
            {
                for (int i = 0; i < datHuman.datHumanUnitWork.param.Length; i++)
                    { datHuman.datHumanUnitWork.param[i] *= POINTS_PER_LEVEL; }
                datHuman.datHumanUnitWork.maxhp = (ushort)(((float)datHuman.datHumanUnitWork.param[3] / (float)POINTS_PER_LEVEL + (float)datHuman.datHumanUnitWork.level) * 6f);
                datHuman.datHumanUnitWork.hp = datHuman.datHumanUnitWork.maxhp;
                datHuman.datHumanUnitWork.maxmp = (ushort)(((float)datHuman.datHumanUnitWork.param[2] / (float)POINTS_PER_LEVEL + (float)datHuman.datHumanUnitWork.level) * 3f);
                datHuman.datHumanUnitWork.mp = datHuman.datHumanUnitWork.maxmp;
            }

            // Search through all of the demons.
            for (int i = 1; i < datDevilFormat.tbl.Length; i++)
            {
                // If enabled, give the demons Int.
                // This effectively just copies over their Mag to it.
                if (EnableIntStat)
                    { datDevilFormat.tbl[i].param[1] = (sbyte)(DemonIntTable[i] / POINTS_PER_LEVEL); }
                
                // If enabled, scale each demon's stats by how many points per level is set.
                // Additionally, recalculate HP/MP for anything that isn't a boss or forced encounter.
                if (EnableStatScaling)
                {
                    for (int j = 0; j < datDevilFormat.tbl[i].param.Length; j++)
                        { datDevilFormat.tbl[i].param[j] *= POINTS_PER_LEVEL; }
                    if (EnableIntStat)
                        { datDevilFormat.tbl[i].param[1] = DemonIntTable[i]; }

                    /* // The HP/MP recalculation here is gonna remain commented out until I do something that necessitates it.
                    if (i < 254)
                    {
                        datDevilFormat.tbl[i].maxhp = (ushort)((datDevilFormat.tbl[i].param[3] / POINTS_PER_LEVEL + datDevilFormat.tbl[i].level) * 6);
                        datDevilFormat.tbl[i].hp = datDevilFormat.tbl[i].maxhp;
                        datDevilFormat.tbl[i].maxmp = (ushort)((datDevilFormat.tbl[i].param[2] / POINTS_PER_LEVEL + datDevilFormat.tbl[i].level) * 3);
                        datDevilFormat.tbl[i].mp = datDevilFormat.tbl[i].maxmp;
                    }
                    */
                }
            }

            // Searche through al of the Magatamas.
            for (int i = 0; i < tblHearts.fclHeartsTbl.Length; i++)
            {

                // If enabled, gives the Magatama Int.
                if (EnableIntStat)
                {
                    // Set up Int.
                    tblHearts.fclHeartsTbl[i].GrowParamTbl[1] = tblHearts.fclHeartsTbl[i].GrowParamTbl[2];
                    tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[1] = tblHearts.fclHeartsTbl[i].MasterGrowParamTbl[2];
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
            // Additionally, add the Int Incense to the Lucky Ticket Prizes.
            if (EnableIntStat)
            {
                // Mitama Bonuses
                fclCombineTable.fclSpiritParamUpTbl[0].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(3 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[1].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[2].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(1 + 1).ToArray();
                fclCombineTable.fclSpiritParamUpTbl[3].ParamType = fclCombineTable.fclSpiritParamUpTbl[0].ParamType.Append<ushort>(2 + 1).ToArray();
                
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
                    {
                        fclJunkShopTable.fclShopItemBoxTbl[1][i].Rate = 40;
                    }
                }
            }

            // Gives a head's up to the user if they have the console enabled.
            LoggerInstance.Msg("Modern Stats System Initialized.");
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstSetMaxHpMp))]
        private class PatchSetMaxHpMp
        {
            private static void Postfix(sbyte Mode, ref datUnitWork_t pStock)
            {
                // Set the target's Max HP and MP
                pStock.maxhp = (ushort)datCalc.datGetMaxHp(pStock);
                pStock.maxmp = (ushort)datCalc.datGetMaxMp(pStock);

                // If Mode is 0, fully heal the target.
                if (Mode == 0)
                {
                    pStock.hp = pStock.maxhp;
                    pStock.mp = pStock.maxmp;
                }
            }
        }

        [HarmonyPatch(typeof(rstcalc), nameof(rstcalc.rstChkParamLimitAll))]
        private class PatchChkParamLimitAll
        {
            private static bool Prefix(ref int __result, datUnitWork_t pStock, bool paramSet = true)
            {
                // Return value initialization
                __result = 0;
                
                // If your stats are not capped completely, return.
                if (datCalc.datGetBaseParam(pStock, 0) >= MAXSTATS)
                {
                    if (EnableIntStat && datCalc.datGetBaseParam(pStock, 1) < MAXSTATS) { return false; }
                    if (datCalc.datGetBaseParam(pStock, 2) < MAXSTATS) { return false; }
                    if (datCalc.datGetBaseParam(pStock, 3) < MAXSTATS) { return false; }
                    if (datCalc.datGetBaseParam(pStock, 4) < MAXSTATS) { return false; }
                    if (datCalc.datGetBaseParam(pStock, 5) < MAXSTATS) { return false; }

                    // If you got to this point, your stats are completely maxed out.
                    // Additionally, if this is true, recalculate your HP/MP.
                    if (paramSet)
                        { cmpMisc.cmpSetMaxHPMP(pStock); }

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
                    cmpMisc.cmpSetMaxHPMP(pStock);
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
                result = Math.Clamp(result, 1, MAXSTATS);
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
                if (rstinit.GBWK != null)
                    { __result += rstinit.GBWK.ParamOfs[paratype]; }
                return false;
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
                    result = (int)(((float)datCalc.datGetBaseParam(work, 3) / (float)POINTS_PER_LEVEL + (float)work.level) * 6f);
                    if (rstinit.GBWK != null && !EvoCheck)
                        { result += (int)((float)rstinit.GBWK.ParamOfs[3] / (float)POINTS_PER_LEVEL * 6f); }
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
                    result = (int)(((float)datCalc.datGetBaseParam(work, 2) / (float)POINTS_PER_LEVEL + (float)work.level) * 3f);
                    if (rstinit.GBWK != null && !EvoCheck)
                        { result += (int)((float)rstinit.GBWK.ParamOfs[2] / (float)POINTS_PER_LEVEL * 3f); }
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
                uint result = (uint)datCalc.datGetBaseMaxHp(work);

                // Add a percentage of your Max HP to your Max HP with certain special Skills.
                float boost = 1.0f;
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
                uint result = (uint)datCalc.datGetBaseMaxMp(work);

                // Like before, add percentages of it to it based on certain Skills.
                float boost = 1.0f;
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
                return 6;
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
                
                // Clear the fact that you were setting stats.
                SettingAsignParam = false;

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
                // If you're in the confirmation Message.
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
                EvoCheck = false;
                if (SettingAsignParam == false)
                {
                    SettingAsignParam = true;
                    PatchResetAsignParam.ResetParam();
                }

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
                cmpMisc.cmpSetMaxHPMP(pStock);
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

                // If the object's not null and you're not evolving, set the LevelUp stat value.
                int levelstat = pStock.mitamaparam[(ctr2 > 1 && !EnableIntStat) ? ctr2 - 1 : ctr2];
                if (rstinit.GBWK != null && !EvoCheck)
                    { levelstat += rstinit.GBWK.ParamOfs[(ctr2 > 1 && !EnableIntStat) ? ctr2 - 1 : ctr2]; }

                // If there's Counter objects in the Status Menu's children, set up their values and colors.
                if (stsObj.GetComponentsInChildren<CounterCtr>() != null)
                    { stsObj.GetComponentsInChildren<CounterCtr>()[(ctr2 > 1 && !EnableIntStat) ? ctr2 -1 : ctr2].Set(pStock.param[ctr2] + levelstat, Color.white, (CursorMode == 2 && CursorPos > -1) ? 1 : 0); }
                
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
                    int levelstat = pStock.mitamaparam[stat];
                    if (rstinit.GBWK != null && !EvoCheck)
                        { levelstat += rstinit.GBWK.ParamOfs[stat]; }

                    // Set Stat value and color.
                    g2.GetComponent<CounterCtr>().Set(pStock.param[stat] + levelstat, Color.white, 0);
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
                if (Pos >= cmpDrawStatus.gStatusBlinkQue.Length && cmpDrawStatus.gStatusBlinkQue.Length > 0)
                    { Pos = (sbyte)(cmpDrawStatus.gStatusBlinkQue.Length - 1); }

                // If FlashMode is at least 0 or higher but under 3, set the Blink Color to FlashMode.
                if (FlashMode >= 0 && FlashMode < 3)
                { cmpDrawStatus.cmpStatMakeBlinkCol(cmpDrawStatus.gStatusBlinkQue[Pos], FlashMode, pCol); }

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