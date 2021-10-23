using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace empireMaker {
    public partial class EmpireMaker : ModBase {


        public static List<FactionDef> eligibleFactions = new List<FactionDef>();
        public static List<ConversionParams> factionConversionSettings = new List<ConversionParams>();

        public override string ModIdentifier => "empireMaker";

        public override void DefsLoaded() {
            // get all factions that:
            // - aren't royal,
            // - aren't hidden,
            // - don't start with one enemy,
            // - isn't a pawn group maker faction,
            // - and isn't the player faction.
            foreach (var faction in from faction in DefDatabase<FactionDef>.AllDefs
                                    where
                                        faction.royalFavorLabel == null &&
                                        !faction.hidden &&
                                        //(faction.naturalColonyGoodwill.min >= 0 || faction.naturalColonyGoodwill.max >= 0) &&
                                        !faction.mustStartOneEnemy &&
                                        //!faction.permanentEnemy &&
                                        faction.pawnGroupMakers != null &&
                                        !faction.isPlayer
                                    select faction
            ) {
                eligibleFactions.Add(faction);
            }


            GetSettings();
            PatchDef();
        }

        public static void PatchDef() {
            Log.Message("## Empire Maker Start Install");

            // 제국
            var empireFactionDef = FactionDefOf.Empire;

            // 제국 타이틀
            // todo: add ability to create custom royal title maps

            var baseRoyalTitles = new Dictionary<string, List<RoyalTitleDef>> {
                { "NeolithicTitle", new List<RoyalTitleDef>() },
                //{ "MedievalTitle", new List<RoyalTitleDef>() },
                { "IndustrialOutlanderTitle", new List<RoyalTitleDef>() },
                { "IndustrialMercenaryTitle", new List<RoyalTitleDef>() },
                //{ "SpacerTitle", new List<RoyalTitleDef>() },
                //{ "SpacerRaiderTitle", new List<RoyalTitleDef>() },
                //{ "UltraTitle", new List<RoyalTitleDef>() },
            };

            var royalFavorLabels = new Dictionary<EmpireArchetype, string> {
                {  EmpireArchetype.Neolithic, "respect" },
                {  EmpireArchetype.Medieval, "respect" },
                {  EmpireArchetype.IndustrialOutlander, "distinction" },
                {  EmpireArchetype.IndustrialRaider, "glory" },
                {  EmpireArchetype.Spacer, "prestige" },
                {  EmpireArchetype.SpacerRaider, "glory" },
                {  EmpireArchetype.Ultra, "honor" },
            };

            // sort all royal title defs into title types
            // EmpireMaker.RoyalTitles.cs
            GetBaseRoyalTitles(baseRoyalTitles);
            var baseRoyalPermits = GetBaseRoyalPermits();
            
            for (var i = 0; i < eligibleFactions.Count; i++) {
                var factionDef = eligibleFactions[i];
                var settings = factionConversionSettings[i];

                Log.Message(
                    settings.ConversionType != Conversion.noConversion
                        ? $"# Empirizing: {eligibleFactions[i].defName} ({eligibleFactions[i].modContentPack.PackageId})"
                        : $"# Ignored: {eligibleFactions[i].defName} ({eligibleFactions[i].modContentPack.PackageId})");

                if (settings.ConversionType == Conversion.noConversion) {
                    continue;
                }

                if (debugMode) {
                    Log.Message("A");
                }

                // 팩션 제국화
                // copy royal faction settings from the Empire
                factionDef.royalFavorLabel = royalFavorLabels[settings.Archetype];
                factionDef.royalFavorIconPath = empireFactionDef.royalFavorIconPath;
                //Log.Warning("[DEBUG] path: " + empireFactionDef.royalFavorIconPath);
                //factionDef.royalFavorIconPath = $"F2E/Icon_{factionDef.defName}";
                factionDef.raidLootMaker = empireFactionDef.raidLootMaker;

                //var royalFavorIcon = GraphicDatabase.Get<Graphic>(factionDef.royalFavorIconPath);


                factionDef.royalTitleInheritanceRelations = empireFactionDef.royalTitleInheritanceRelations;
                factionDef.royalTitleInheritanceWorkerClass = empireFactionDef.royalTitleInheritanceWorkerClass;
                //faction.minTitleForBladelinkWeapons = baseF.minTitleForBladelinkWeapons;

                TechLevel techLevel = EmpireHelpers.GetTechLevel(settings, factionDef);

                // Sets royal title tags based on tech level. Returns true if the tech level matches a valid tech level.
                // requires valid FactionDef.techLevel
                // EmpireMaker.RoyalTitles.cs
                if (!SetRoyalTitleTags(settings, factionDef, techLevel)) {
                    Log.Error($"Faction {factionDef.defName} is marked for empire conversion but has an invalid tech level: {factionDef.techLevel}. Set the faction's tech level manually through the Empire : Force Conversion and tech level settings.");
                    // keep converting anyways, but will probably be bugged
                }

                if (factionDef.colorSpectrum == null) {
                    factionDef.colorSpectrum = new List<Color> { Color.white };
                }

                if (debugMode) {
                    Log.Message("B");
                }

                // sort pawn types into groups
                // EmpireMaker.SortPawnKinds.cs
                SortPawnKinds(settings, factionDef, out var allPawns, out var leaderPawns, out var nonLeaderPawns);

                // get royal permits & pawn types:
                //SortPermitPawnsLegacy(settings, factionDef, fighterPawns, out var permitPawns);
                //CopyEmpirePermits(factionDef, permitPawns);
                // -- or --

                // EmpireMaker.Permits.cs
                if (!GenerateRoyalPermits(settings, factionDef, techLevel, allPawns, baseRoyalPermits[settings.Archetype], out var newPermits)) {
                    Log.Error($"Faction {factionDef.defName} is marked for empire conversion but failed trade and reinforcement permit generation.");
                    // keep converting anyways, but will probably be bugged
                }

                // 커스텀 작위
                // EmpireMaker.RoyalTitles.cs
                if (!GenerateRoyalTitleDefs(settings, factionDef, baseRoyalTitles, newPermits, out var royalTitles)) {
                    Log.Error($"Faction {factionDef.defName} is marked for empire conversion but failed royal title def generation.");
                    // keep converting anyways, but will probably be bugged
                }

                if (debugMode) {
                    Log.Message("E");
                }


                // 귀족 PawnKind 생성
                var bugFixMode = settings.ConversionType == Conversion.bugFix;

                // 버그 해결 모드
                // EmpireMaker.PawnKinds.cs
                if (allPawns.Count > 0) {
                    if (bugFixMode) {
                        GeneratePawnKindsBugFix(settings, factionDef, royalTitles, allPawns, leaderPawns, nonLeaderPawns);
                    }
                    else {
                        GeneratePawnKinds(settings, factionDef, royalTitles, allPawns, leaderPawns, nonLeaderPawns);
                    }
                }
            }
        }


        public static void PatchRelation() {
            for (var factionDef = 0; factionDef < eligibleFactions.Count; factionDef++) {
                var faction = eligibleFactions[factionDef];


                // 관계
                switch (factionConversionSettings[factionDef].RelationshipType) {
                    case Relationship.basic:
                        break;
                    case Relationship.empire:
                        faction.permanentEnemy = false;
                        faction.mustStartOneEnemy = false;
                        break;
                    case Relationship.ally:
                        faction.permanentEnemy = false;
                        faction.mustStartOneEnemy = false;
                        break;
                    case Relationship.neutral:
                        faction.permanentEnemy = false;
                        faction.mustStartOneEnemy = false;
                        break;
                    case Relationship.enemy:
                        faction.permanentEnemy = false;
                        faction.mustStartOneEnemy = false;
                        break;
                    case Relationship.permanentEnemy:
                        faction.permanentEnemy = true;
                        break;
                }


                if (factionConversionSettings[factionDef].ConversionType == Conversion.noConversion) {
                    continue;
                }

                // 적대 세력일 경우
                if (!faction.permanentEnemy) {
                    continue;
                }

                faction.permanentEnemy = false;
                faction.mustStartOneEnemy = false;
            }
        }



        public static bool IsViolatingRulesOf(Def implantOrWeapon, Pawn pawn, Faction faction, int implantLevel = 0) {
            if (faction.def.royalImplantRules == null || faction.def.royalImplantRules.Count == 0) {
                return true;
            }

            var minTitleToUse =
                ThingRequiringRoyalPermissionUtility.GetMinTitleToUse(implantOrWeapon, faction, implantLevel);
            var currentTitle = pawn.royalty.GetCurrentTitle(faction);
            if (currentTitle == null) {
                return true;
            }

            var num = faction.def.RoyalTitlesAwardableInSeniorityOrderForReading.IndexOf(currentTitle);
            if (num < 0) {
                return true;
            }

            var num2 = faction.def.RoyalTitlesAwardableInSeniorityOrderForReading.IndexOf(minTitleToUse);
            return num < num2;
        }
    }
}