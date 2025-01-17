﻿using RimWorld;
using System;
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

        private static bool GenerateRoyalPermits(ConversionParams settings, FactionDef factionDef,
            TechLevel techLevel, List<PawnKindDef> allPawns, List<RoyalTitlePermitDef> baseRoyalPermits,
            out Dictionary<string, RoyalTitlePermitDef> generatedPermitMap)
        {
            // this dictionary contains ORIGINAL defNames as keys, GENERATED defs as values.
            generatedPermitMap = new Dictionary<string, RoyalTitlePermitDef>();

            HashSet<RoyalTitlePermitDef> basePermits = new HashSet<RoyalTitlePermitDef>();

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
                generatedPermitMap.Add(aidSmallDefName, militarySmall);
                basePermits.Add(callMilitaryAidSmall);
            }
            if (militaryLarge != null) {
                DefDatabase<RoyalTitlePermitDef>.Add(militaryLarge);
                generatedPermitMap.Add(aidLargeDefName, militaryLarge);
                basePermits.Add(callMilitaryAidLarge);
            }
            if (militaryGrand != null) {
                DefDatabase<RoyalTitlePermitDef>.Add(militaryGrand);
                generatedPermitMap.Add(aidGrandDefName, militaryGrand);
                basePermits.Add(callMilitaryAidGrand);
            }

            // Trade permits --
            var tradeSettlement = new RoyalTitlePermitDef();
            var tradeCaravan = new RoyalTitlePermitDef();
            var tradeOrbital = new RoyalTitlePermitDef();

            tradeSettlement.defName = $"TradeSettlement_{factionDef.defName}";
            tradeCaravan.defName = $"TradeCaravan_{factionDef.defName}";
            tradeOrbital.defName = $"TradeOrbital_{factionDef.defName}";

            tradeSettlement.label = $"trade with settlements";
            tradeCaravan.label = $"trade with caravans";
            tradeOrbital.label = $"trade with orbital traders";

            tradeSettlement.generated = true;
            tradeCaravan.generated = true;
            tradeOrbital.generated = true;

            DefDatabase<RoyalTitlePermitDef>.Add(tradeSettlement);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeCaravan);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeOrbital);

            generatedPermitMap.Add("TradeSettlement", tradeSettlement);
            generatedPermitMap.Add("TradeCaravan", tradeCaravan);
            generatedPermitMap.Add("TradeOrbital", tradeOrbital);

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
                traderKindDefList = new List<TraderKindDef>();
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

            foreach (var permit in baseRoyalPermits) {
                if (basePermits.Contains(permit)) {
                    if (debugMode) {
                        Log.Message($"Not cloning {permit.defName}.");
                    }
                }
                else {
                    var def = EmpireHelpers.ClonePermitDef(factionDef, permit);

                    if (debugMode) {
                        Log.Message($"Cloning {permit.defName} to {def.defName}. Faction: {def.faction.defName}");
                    }

                    generatedPermitMap.Add(permit.defName, def);
                    DefDatabase<RoyalTitlePermitDef>.Add(def);
                }
            }

            foreach (var pair in generatedPermitMap) {
                RoyalTitlePermitDef def = pair.Value;

                if (def.prerequisite == null) continue;
                else {
                    string prerequisiteDefName = def.prerequisite.defName;

                    if (generatedPermitMap.ContainsKey(prerequisiteDefName)) {
                        var newPermit = generatedPermitMap[prerequisiteDefName];

                        def.prerequisite = newPermit;
                    }
                    else {
                        Log.Error($"F2E royal permit {def.defName} has permit {def.prerequisite.defName} referenced as a prerequisite, but the referenced permit has not been copied to this faction. This should not happen.");
                    }
                }
            }

            return true;
        }

        private static RoyalTitlePermitDef GenerateCombatPermitDef(ConversionParams settings, FactionDef factionDef, int tier, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> permitPawns)
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

        private static Dictionary<EmpireArchetype, List<RoyalTitlePermitDef>> GetBaseRoyalPermits()
        {
            var dict = new Dictionary<EmpireArchetype, List<RoyalTitlePermitDef>>();

            foreach (object value in Enum.GetValues(typeof(EmpireArchetype))) {
                var archetype = (EmpireArchetype)value;

                if (!dict.ContainsKey(archetype)) {
                    dict[archetype] = new List<RoyalTitlePermitDef>();
                }
            }

            foreach (var permitDef in from permit
                                      in DefDatabase<RoyalTitlePermitDef>.AllDefs
                                      where permit != null
                                            && permit.defName.Length > 4
                                            && permit.defName.Substring(0, 4) == "f2e_"
                                      select permit) {
                if (permitDef.faction == null)
                    Log.Error($"F2E permit {permitDef.defName} has no faction - this should not happen.");

                // get rid of f2e_
                // ex: f2e_IndustrialOutlander => IndustrialOutlander
                var permitArchetype = permitDef.faction.defName.Substring(4);

                if (Enum.TryParse<EmpireArchetype>(permitArchetype, out var archetype)) {
                    Log.Message($"Found permit {permitDef.defName}, archetype:{permitArchetype}");
                    dict[archetype].Add(permitDef);
                }
            }

            return dict;
        }
    }
}