﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace empireMaker
{
    public partial class EmpireMaker
    {
        private class Permit
        {
            public string prefix;
        }

        //private static RoyalTitlePermitDef s_callMilitaryAidSmall;
        //private static RoyalTitlePermitDef s_callMilitaryAidLarge;
        //private static RoyalTitlePermitDef s_callMilitaryAidGrand;

        private static RoyalTitlePermitDef s_tradeSettlement;
        private static RoyalTitlePermitDef s_tradeCaravan;
        private static RoyalTitlePermitDef s_tradeOrbital;

        private const int BaseCombatPower = 240;
        private const int ScaledCombatPower = 300;

        private static bool GeneratePermits(ConversionSettings settings, FactionDef factionDef,
            TechLevel techLevel, List<PawnKindDef> fighterPawns,
            out Dictionary<string, RoyalTitlePermitDef> generatedPermitDefs)
        {
            // by the time this is run, factiondef should be one of the valid techlevels.

            generatedPermitDefs = new Dictionary<string, RoyalTitlePermitDef>();

            var p = EmpireHelpers.GetDefNamePrefix(techLevel);

            var aidSmallDefName = p + "CallMilitaryAidSmall";
            var aidLargeDefName = p + "CallMilitaryAidLarge";
            var aidGrandDefName = p + "CallMilitaryAidGrand";

            // modify premade permitdefs.
            var callMilitaryAidSmall = DefDatabase<RoyalTitlePermitDef>.GetNamed(aidSmallDefName, false);
            var callMilitaryAidLarge = DefDatabase<RoyalTitlePermitDef>.GetNamed(aidLargeDefName, false);
            var callMilitaryAidGrand = DefDatabase<RoyalTitlePermitDef>.GetNamed(aidGrandDefName, false);

            s_tradeSettlement ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeSettlement");
            s_tradeCaravan ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeCaravan");

            SortPermitPawns(settings, factionDef, fighterPawns, out List<PawnKindDef> permitPawns);

            // TODO: add settings for permits
            var militarySmall = GenerateCombatPermitDef(settings, factionDef, 0, callMilitaryAidSmall, permitPawns);
            var militaryLarge = GenerateCombatPermitDef(settings, factionDef, 1, callMilitaryAidLarge, permitPawns);
            var militaryGrand = GenerateCombatPermitDef(settings, factionDef, 2, callMilitaryAidGrand, permitPawns);

            var tradeSettlement = GenerateTradePermitDef(settings, factionDef, s_tradeSettlement, permitPawns);
            var tradeCaravan = GenerateTradePermitDef(settings, factionDef, s_tradeCaravan, permitPawns);

            // add permits to database if they aren't already loaded
            if (militarySmall != null) {
                DefDatabase<RoyalTitlePermitDef>.Add(militarySmall);
                generatedPermitDefs.Add(aidSmallDefName, militarySmall);
            }
            if (militaryLarge != null) {
                DefDatabase<RoyalTitlePermitDef>.Add(militaryLarge);
                generatedPermitDefs.Add(aidLargeDefName, militaryLarge);
            }
            if (militaryGrand != null) {
                DefDatabase<RoyalTitlePermitDef>.Add(militaryGrand);
                generatedPermitDefs.Add(aidGrandDefName, militaryGrand);
            }

            DefDatabase<RoyalTitlePermitDef>.Add(tradeSettlement);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeCaravan);
            generatedPermitDefs.Add("TradeSettlement", tradeSettlement);
            generatedPermitDefs.Add("TradeCaravan", tradeCaravan);

            // if faction is at least spacer, generate orbital trade permit
            if (techLevel >= TechLevel.Spacer) {
                s_tradeOrbital ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeOrbital");
                var tradeOrbital = GenerateTradePermitDef(settings, factionDef, s_tradeOrbital, permitPawns);

                DefDatabase<RoyalTitlePermitDef>.Add(tradeOrbital);
                generatedPermitDefs.Add("TradeOrbital", tradeOrbital);
            }

            if (debugMode) {
                Log.Message("C");
            }

            return true;
        }

        private static RoyalTitlePermitDef GenerateCombatPermitDef(ConversionSettings settings, FactionDef factionDef, int tier, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> permitPawns)
        {
            if (derivedFrom == null) return null;

            RoyalTitlePermitDef def;

            def = EmpireHelpers.CreateBasePermitDef(factionDef, derivedFrom);

            int combatPower = (tier == 0) ? BaseCombatPower : ScaledCombatPower * (tier + 1);

            def.royalAid = new RoyalAid {
                favorCost = derivedFrom.royalAid.favorCost,
                pawnKindDef = permitPawns[tier],
                pawnCount = Mathf.RoundToInt(combatPower / permitPawns[tier].combatPower)
            };

            //if (EmpireHelpers.IsUnmoddedFaction(factionDef.defName)) {
            //    // even if it's unmodded, we still need to set the faction
            //    def = GenerateBasePermitDef(settings, factionDef, derivedFrom);

            //    def.royalAid = new RoyalAid {
            //        favorCost = derivedFrom.royalAid.favorCost,
            //        pawnKindDef = derivedFrom.royalAid.pawnKindDef,
            //        pawnCount = derivedFrom.royalAid.pawnCount,
            //    };
            //}
            //else {
            //}

            return def;
        }

        private static RoyalTitlePermitDef GenerateTradePermitDef(ConversionSettings settings, FactionDef factionDef, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> permitPawns)
        {
            if (derivedFrom == null) return null;

            var def = EmpireHelpers.CreateBasePermitDef(factionDef, derivedFrom);

            def.royalAid = new RoyalAid {
                favorCost = derivedFrom.royalAid.favorCost,
                pawnKindDef = permitPawns[0],
                pawnCount = Mathf.RoundToInt(BaseCombatPower / permitPawns[0].combatPower)
            };

            return def;
        }
    }
}