using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace empireMaker
{
    public partial class EmpireMaker
    {
        private static RoyalTitlePermitDef s_tradeSettlement;
        private static RoyalTitlePermitDef s_tradeCaravan;
        private static RoyalTitlePermitDef s_tradeOrbital;

        private const int BaseCombatPower = 240;
        private const int ScaledCombatPower = 300;

        private static bool GeneratePermits(ConversionSettings settings, FactionDef factionDef,
            TechLevel techLevel, List<PawnKindDef> allPawns,
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

            SortFighterPawnKinds(settings, factionDef, allPawns, out var fighterPawns);
            SortPermitPawns(settings, factionDef, fighterPawns, out List<PawnKindDef> permitPawns);

            // TODO: add settings for permits
            var militarySmall = GenerateCombatPermitDef(settings, factionDef, 0, callMilitaryAidSmall, permitPawns);
            var militaryLarge = GenerateCombatPermitDef(settings, factionDef, 1, callMilitaryAidLarge, permitPawns);
            var militaryGrand = GenerateCombatPermitDef(settings, factionDef, 2, callMilitaryAidGrand, permitPawns);

            var tradeSettlement = EmpireHelpers.CreateBasePermitDef(factionDef, s_tradeSettlement);
            var tradeCaravan = EmpireHelpers.CreateBasePermitDef(factionDef, s_tradeCaravan);

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

            // Setup TraderKinds ---
            // if faction is at least spacer, generate orbital trade permit stuff too
            if (techLevel >= TechLevel.Spacer) {
                s_tradeOrbital ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("TradeOrbital");
                var tradeOrbital = EmpireHelpers.CreateBasePermitDef(factionDef, s_tradeOrbital);

                DefDatabase<RoyalTitlePermitDef>.Add(tradeOrbital);
                generatedPermitDefs.Add("TradeOrbital", tradeOrbital);

                if (settings.RequiresTradePermit) {
                    // 궤도상선
                    foreach (var traderKindDef in from traders in DefDatabase<TraderKindDef>.AllDefs
                                                  where traders.orbital && traders.faction != null && traders.faction == factionDef
                                                  select traders
                    ) {
                        traderKindDef.permitRequiredForTrading = tradeOrbital;
                    }
                }
            }

            // 계급에 따른 거래제한
            if (settings.RequiresTradePermit) {
                var unused = new RoyalTitlePermitDef();

                // 기지
                var traderKindDefList = new List<TraderKindDef>();
                foreach (var traderKindDef in factionDef.baseTraderKinds) {
                    var newTraderKindDef = EmpireHelpers.CopyTraderKind(traderKindDef);
                    newTraderKindDef.defName = $"{traderKindDef.defName}_{factionDef.defName}_base";
                    newTraderKindDef.permitRequiredForTrading = tradeSettlement;
                    newTraderKindDef.faction = factionDef;
                    DefDatabase<TraderKindDef>.Add(newTraderKindDef);
                    traderKindDefList.Add(newTraderKindDef);
                }

                factionDef.baseTraderKinds = traderKindDefList;


                // 캐러밴
                traderKindDefList.Clear();
                foreach (var caravanTraderKindDef in factionDef.caravanTraderKinds) {
                    var newCaravanTraderKindDef = EmpireHelpers.CopyTraderKind(caravanTraderKindDef);
                    newCaravanTraderKindDef.defName =
                        $"{caravanTraderKindDef.defName}_{factionDef.defName}_caravan";
                    newCaravanTraderKindDef.permitRequiredForTrading = tradeCaravan;
                    newCaravanTraderKindDef.faction = factionDef;
                    DefDatabase<TraderKindDef>.Add(newCaravanTraderKindDef);
                    traderKindDefList.Add(newCaravanTraderKindDef);
                }

                factionDef.caravanTraderKinds = traderKindDefList;
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
    }
}