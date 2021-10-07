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

        static Dictionary<TechLevel, Permit> permitMaps = new Dictionary<TechLevel, Permit>() {
            {
                TechLevel.Neolithic,
                new Permit() {
                    prefix = "f2e_Tribal_"
                }
            },
            {
                TechLevel.Medieval,
                new Permit() {
                    prefix = "f2e_Medieval_"
                }
            },
            {
                TechLevel.Industrial,
                new Permit() {
                    prefix = "f2e_Outlander_"
                }
            },
            {
                TechLevel.Spacer,
                new Permit() {
                    prefix = "f2e_Spacer_"
                }
            },
            {
                TechLevel.Ultra,
                new Permit() {
                    prefix = "f2e_Spacer_"
                }
            },
        };

        private static RoyalTitlePermitDef s_callMilitaryAidSmall;
        private static RoyalTitlePermitDef s_callMilitaryAidLarge;
        private static RoyalTitlePermitDef s_callMilitaryAidGrand;

        public static bool GenerateCombatPermits(ConversionSettings settings, FactionDef factionDef,
            TechLevel techLevel, List<PawnKindDef> fighterPawns)
        {
            // by the time this is run, factiondef should be one of the valid techlevels.

            s_callMilitaryAidSmall ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("CallMilitaryAidSmall");
            s_callMilitaryAidLarge ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("CallMilitaryAidLarge");
            s_callMilitaryAidGrand ??= DefDatabase<RoyalTitlePermitDef>.GetNamed("CallMilitaryAidGrand");

            if (permitMaps.ContainsKey(techLevel)) {
                // TODO: add settings for permits
                var militarySmall = GenerateCombatPermitDef(settings, factionDef, techLevel, 0, s_callMilitaryAidSmall, fighterPawns);
                var militaryLarge = GenerateCombatPermitDef(settings, factionDef, techLevel, 0, s_callMilitaryAidLarge, fighterPawns);
                var militaryGrand = GenerateCombatPermitDef(settings, factionDef, techLevel, 0, s_callMilitaryAidGrand, fighterPawns);

                if (militarySmall != s_callMilitaryAidSmall) DefDatabase<RoyalTitlePermitDef>.Add(militarySmall);
                if (militaryLarge != s_callMilitaryAidSmall) DefDatabase<RoyalTitlePermitDef>.Add(militaryLarge);
                if (militaryGrand != s_callMilitaryAidSmall) DefDatabase<RoyalTitlePermitDef>.Add(militaryGrand);
            }
            else {
                return false;
            }

            if (debugMode) {
                Log.Message("C");
            }

            return true;
        }

        public static RoyalTitlePermitDef GenerateCombatPermitDef(ConversionSettings settings, FactionDef factionDef, TechLevel techLevel, int tier, RoyalTitlePermitDef derivedFrom, List<PawnKindDef> fighterPawns)
        {
            RoyalTitlePermitDef def;

            if (EmpireHelpers.IsUnmoddedFaction(factionDef.defName)) {
                def = derivedFrom;
            }
            else {
                def = new RoyalTitlePermitDef();

                def.defName = $"{derivedFrom.defName}_{factionDef.defName}";
                def.label = derivedFrom.label;
                def.workerClass = derivedFrom.workerClass;
                def.workerClass = derivedFrom.workerClass;
                def.cooldownDays = derivedFrom.cooldownDays;

                SortPermitPawns(settings, factionDef, fighterPawns, out List<PawnKindDef> permitPawns);

                int combatPower = (tier == 0) ? 240 : 300 * (tier + 1);

                def.royalAid = new RoyalAid {
                    favorCost = derivedFrom.royalAid.favorCost,
                    pawnKindDef = permitPawns[tier],
                    pawnCount = Mathf.RoundToInt(combatPower / permitPawns[tier].combatPower)
                };
            }

            return def;
        }
    }
}