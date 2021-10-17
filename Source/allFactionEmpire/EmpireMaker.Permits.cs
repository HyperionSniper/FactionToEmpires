using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace empireMaker
{
    public partial class EmpireMaker
    {
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

            SortFighterPawnKinds(settings, factionDef, allPawns, out var fighterPawns);
            SortPermitPawns(settings, factionDef, fighterPawns, out List<PawnKindDef> permitPawns);

            // Combat permits --
            // TODO: add settings for permits
            var militarySmall = GenerateCombatPermitDef(settings, factionDef, 0, callMilitaryAidSmall, permitPawns);
            var militaryLarge = GenerateCombatPermitDef(settings, factionDef, 1, callMilitaryAidLarge, permitPawns);
            var militaryGrand = GenerateCombatPermitDef(settings, factionDef, 2, callMilitaryAidGrand, permitPawns);

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

            // Trade permits --
            var tradeSettlement = new RoyalTitlePermitDef();
            var tradeCaravan = new RoyalTitlePermitDef();
            var tradeOrbital = new RoyalTitlePermitDef();

            tradeSettlement.defName = $"TradeSettlement_{factionDef.defName}";
            tradeCaravan.defName = $"TradeCaravan_{factionDef.defName}";
            tradeOrbital.defName = $"TradeOrbital_{factionDef.defName}";

            tradeSettlement.label = "trade with settlements";
            tradeCaravan.label = $"trade with caravans";
            tradeOrbital.label = $"trade with orbital traders";

            DefDatabase<RoyalTitlePermitDef>.Add(tradeSettlement);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeCaravan);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeOrbital);

            generatedPermitDefs.Add("TradeSettlement", tradeSettlement);
            generatedPermitDefs.Add("TradeCaravan", tradeCaravan);
            generatedPermitDefs.Add("TradeOrbital", tradeOrbital);

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

                // 궤도상선
                foreach (var traderKindDef in from traders in DefDatabase<TraderKindDef>.AllDefs
                                              where traders.orbital && traders.faction != null && traders.faction == factionDef
                                              select traders
                ) {
                    traderKindDef.permitRequiredForTrading = tradeOrbital;
                }
            }

            if (debugMode) {
                Log.Message("C");
            }

            return true;
        }

        private static RoyalTitlePermitDef GenerateCombatPermitDef(ConversionSettings settings, FactionDef factionDef, int tier, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> permitPawns)
        {
            if (derivedFrom == null) return null;

            RoyalTitlePermitDef def = EmpireHelpers.ClonePermitDef(factionDef, derivedFrom);

            int combatPower = (tier == 0) ? BaseCombatPower : ScaledCombatPower * (tier + 1);

            def.royalAid = new RoyalAid {
                favorCost = derivedFrom?.royalAid.favorCost ?? (tier + 2) * 2,
                pawnKindDef = permitPawns[tier],
                pawnCount = Mathf.RoundToInt(combatPower / permitPawns[tier].combatPower)
            };

            return def;
        }
    }
}