using HugsLib.Settings;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace empireMaker {
    public partial class EmpireMaker {
        private static void GenerateLegacyEmpire(ConversionSettings settings, FactionDef factionDef) {
            
        }

        private static void CopyEmpirePermits(FactionDef factionDef, List<PawnKindDef> permitPawns) {
            // 커스텀 권한 생성

            var tradeSettlementPermit = new RoyalTitlePermitDef();
            var tradeOrbitalPermit = new RoyalTitlePermitDef();
            var tradeCaravanPermit = new RoyalTitlePermitDef();

            var callMilitaryAidSmall = new RoyalTitlePermitDef();
            var callMilitaryAidLarge = new RoyalTitlePermitDef();
            var callMilitaryAidGrand = new RoyalTitlePermitDef();

            foreach (var royalTitlePermitDef in from permits in DefDatabase<RoyalTitlePermitDef>.AllDefs
                                                where true
                                                select permits) {
                RoyalTitlePermitDef newPermit = null;
                var n = 0;
                var totalCombatPower = 0f;
                // 복제
                switch (royalTitlePermitDef.defName) {
                    case "TradeSettlement":
                        tradeSettlementPermit.defName = $"TradeSettlement_{factionDef}";
                        tradeSettlementPermit.label = royalTitlePermitDef.label;
                        break;
                    case "TradeOrbital":
                        tradeOrbitalPermit.defName = $"TradeOrbital_{factionDef}";
                        tradeOrbitalPermit.label = royalTitlePermitDef.label;
                        break;
                    case "TradeCaravan":
                        tradeCaravanPermit.defName = $"TradeCaravan_{factionDef}";
                        tradeCaravanPermit.label = royalTitlePermitDef.label;
                        break;

                    case "CallMilitaryAidSmall":
                        newPermit = callMilitaryAidSmall;
                        n = 0;
                        totalCombatPower = 240f;
                        break;
                    case "CallMilitaryAidLarge":
                        newPermit = callMilitaryAidLarge;
                        n = 1;
                        totalCombatPower = 400f;
                        break;
                    case "CallMilitaryAidGrand":
                        newPermit = callMilitaryAidGrand;
                        n = 2;
                        totalCombatPower = 600f;
                        break;
                }

                if (newPermit == null) {
                    continue;
                }

                newPermit.defName = $"{royalTitlePermitDef.defName}_{factionDef.defName}";
                newPermit.label = royalTitlePermitDef.label;
                newPermit.workerClass = royalTitlePermitDef.workerClass;
                newPermit.cooldownDays = royalTitlePermitDef.cooldownDays;
                newPermit.royalAid = new RoyalAid {
                    favorCost = royalTitlePermitDef.royalAid.favorCost,
                    pawnKindDef = permitPawns[n],
                    pawnCount = Mathf.RoundToInt(totalCombatPower / permitPawns[n].combatPower)
                };
            }

            DefDatabase<RoyalTitlePermitDef>.Add(tradeSettlementPermit);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeOrbitalPermit);
            DefDatabase<RoyalTitlePermitDef>.Add(tradeCaravanPermit);

            DefDatabase<RoyalTitlePermitDef>.Add(callMilitaryAidSmall);
            DefDatabase<RoyalTitlePermitDef>.Add(callMilitaryAidLarge);
            DefDatabase<RoyalTitlePermitDef>.Add(callMilitaryAidGrand);

            if (debugMode) {
                Log.Message("D");
            }
        }
    }
}
