using RimWorld;
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

        private static RoyalTitlePermitDef s_callMilitaryAidSmall;
        private static RoyalTitlePermitDef s_callMilitaryAidLarge;
        private static RoyalTitlePermitDef s_callMilitaryAidGrand;

        private static RoyalTitlePermitDef s_tradeSettlement;
        private static RoyalTitlePermitDef s_tradeCaravan;
        private static RoyalTitlePermitDef s_tradeOrbital;

        private const int BaseCombatPower = 240;
        private const int ScaledCombatPower = 300;

        public static bool GenerateTradeAndMilitaryPermits(ConversionSettings settings, FactionDef factionDef,
            TechLevel techLevel, List<PawnKindDef> fighterPawns)
        {
            // by the time this is run, factiondef should be one of the valid techlevels.

            var p = EmpireHelpers.GetDefNamePrefix(techLevel);

            // modify premade permitdefs.
            s_callMilitaryAidSmall ??= DefDatabase<RoyalTitlePermitDef>.GetNamed(p + "CallMilitaryAidSmall");
            s_callMilitaryAidLarge ??= DefDatabase<RoyalTitlePermitDef>.GetNamed(p + "CallMilitaryAidLarge");
            s_callMilitaryAidGrand ??= DefDatabase<RoyalTitlePermitDef>.GetNamed(p + "CallMilitaryAidGrand");

            s_tradeSettlement ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeSettlement");
            s_tradeCaravan ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeCaravan");

            SortPermitPawns(settings, factionDef, fighterPawns, out List<PawnKindDef> permitPawns);

            // TODO: add settings for permits
            var militarySmall = GenerateCombatPermitDef(settings, factionDef, 0, s_callMilitaryAidSmall, permitPawns);
            var militaryLarge = GenerateCombatPermitDef(settings, factionDef, 0, s_callMilitaryAidLarge, permitPawns);
            var militaryGrand = GenerateCombatPermitDef(settings, factionDef, 0, s_callMilitaryAidGrand, permitPawns);

            var tradeSettlement = GenerateTradePermitDef(settings, factionDef, s_tradeSettlement, permitPawns);
            var tradeCaravan = GenerateTradePermitDef(settings, factionDef, s_tradeCaravan, permitPawns);

            // add permits to database if they aren't already loaded
            if (militarySmall != s_callMilitaryAidSmall) DefDatabase<RoyalTitlePermitDef>.Add(militarySmall);
            if (militaryLarge != s_callMilitaryAidLarge) DefDatabase<RoyalTitlePermitDef>.Add(militaryLarge);
            if (militaryGrand != s_callMilitaryAidGrand) DefDatabase<RoyalTitlePermitDef>.Add(militaryGrand);

            DefDatabase<RoyalTitlePermitDef>.Add(tradeSettlement);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeCaravan);

            // if faction is at least spacer, generate orbital trade permit
            if (techLevel >= TechLevel.Spacer) {
                s_tradeOrbital ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeOrbital");
                var tradeOrbital = GenerateTradePermitDef(settings, factionDef, s_tradeOrbital, permitPawns);

                DefDatabase<RoyalTitlePermitDef>.Add(tradeOrbital);
            }

            if (debugMode) {
                Log.Message("C");
            }

            return true;
        }

        public static RoyalTitlePermitDef GenerateBasePermitDef(ConversionSettings settings, FactionDef factionDef, RoyalTitlePermitDef derivedFrom)
        {
            RoyalTitlePermitDef def;

            def = new RoyalTitlePermitDef();

            def.defName = $"{derivedFrom.defName}_{factionDef.defName}";
            def.label = derivedFrom.label;
            def.workerClass = derivedFrom.workerClass;
            def.workerClass = derivedFrom.workerClass;
            def.cooldownDays = derivedFrom.cooldownDays;

            return def;
        }

        public static RoyalTitlePermitDef GenerateCombatPermitDef(ConversionSettings settings, FactionDef factionDef, int tier, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> permitPawns)
        {
            RoyalTitlePermitDef def;

            if (EmpireHelpers.IsUnmoddedFaction(factionDef.defName)) {
                def = derivedFrom;
            }
            else {
                def = GenerateBasePermitDef(settings, factionDef, derivedFrom);

                int combatPower = (tier == 0) ? BaseCombatPower : ScaledCombatPower * (tier + 1);

                def.royalAid = new RoyalAid {
                    favorCost = derivedFrom.royalAid.favorCost,
                    pawnKindDef = permitPawns[tier],
                    pawnCount = Mathf.RoundToInt(combatPower / permitPawns[tier].combatPower)
                };
            }

            return def;
        }
        
        public static RoyalTitlePermitDef GenerateTradePermitDef(ConversionSettings settings, FactionDef factionDef, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> permitPawns) {
            var def = GenerateBasePermitDef(settings, factionDef, derivedFrom);

            def.royalAid = new RoyalAid {
                favorCost = derivedFrom.royalAid.favorCost,
                pawnKindDef = permitPawns[0],
                pawnCount = Mathf.RoundToInt(BaseCombatPower / permitPawns[0].combatPower)
            };

            return def;
        }
    }
}