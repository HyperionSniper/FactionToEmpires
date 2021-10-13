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
        public static List<ConversionSettings> factionConversionSettings = new List<ConversionSettings>();

        public static List<FactionDef> willBeEmpireFactionDefList = new List<FactionDef>();


        // 비호환 하드모드 패키지 아이디

        private SettingHandle<bool> debugModeSetting;

        private SettingHandle<bool> delVanillaSetting;

        private SettingHandle<bool> phychicAllSetting;

        private SettingHandle<float> questAmountSetting;

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

            var royalTitleTagMap = new Dictionary<string, List<RoyalTitleDef>> {
                { "EmpireTitle", new List<RoyalTitleDef>() },
                { "OutlanderTitle", new List<RoyalTitleDef>() },
                { "OutlanderMercenaryTitle", new List<RoyalTitleDef>() },
                //{ "TribalTitle", new List<RoyalTitleDef>() },
            };

            // sort all royal title defs into title types
            // EmpireMaker.RoyalTitles.cs
            SortRoyalTitleDefs(royalTitleTagMap);

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

                var useApparel = settings.WantsApparelType;

                if (!factionDef.humanlikeFaction) {
                    useApparel = WantsApparel.off;
                }
                //bool useCollectorTrader = ar_faction_collector[z];
                var useTradePermit = settings.RequiresTradePermit;

                if (debugMode) {
                    Log.Message("A");
                }

                // 팩션 제국화
                // copy royal faction settings from the Empire
                factionDef.royalFavorLabel = empireFactionDef.royalFavorLabel;
                factionDef.royalFavorIconPath = empireFactionDef.royalFavorIconPath;
                factionDef.raidLootMaker = empireFactionDef.raidLootMaker;

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
                SortFighterPawnKinds(settings, factionDef, allPawns, out var fighterPawns);

                // get royal permits & pawn types:
                //SortPermitPawnsLegacy(settings, factionDef, fighterPawns, out var permitPawns);
                //CopyEmpirePermits(factionDef, permitPawns);
                // -- or --

                // EmpireMaker.Permits.cs
                if (!GeneratePermits(settings, factionDef, techLevel, fighterPawns, out var newPermits)) {
                    Log.Error($"Faction {factionDef.defName} is marked for empire conversion but failed permit generation.");
                    // keep converting anyways, but will probably be bugged
                }

                // 커스텀 작위
                // EmpireMaker.RoyalTitles.cs
                if (!GenerateRoyalTitleDefs(settings, factionDef, royalTitleTagMap, newPermits, out var royalTitles)) {
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

                // Pawn Kinds 생성 : SpaceRefugee_Clothed
                if (nonLeaderPawns.Count > 0) {
                    var spaceRefugeeClothedDef = PawnKindDef.Named("SpaceRefugee_Clothed");
                    var randomPawnCopy = EmpireHelpers.CopyPawnKind(nonLeaderPawns[0]);
                    randomPawnCopy.defName = $"SpaceRefugee_Clothed_{factionDef.defName}";
                    randomPawnCopy.apparelMoney = spaceRefugeeClothedDef.apparelMoney;
                    randomPawnCopy.gearHealthRange = spaceRefugeeClothedDef.gearHealthRange;
                    if (randomPawnCopy.disallowedTraits == null) {
                        randomPawnCopy.disallowedTraits = new List<TraitDef>();
                    }

                    randomPawnCopy.disallowedTraits.Add(TraitDefOf.Nudist);
                    randomPawnCopy.baseRecruitDifficulty = spaceRefugeeClothedDef.baseRecruitDifficulty;
                    randomPawnCopy.forceNormalGearQuality = spaceRefugeeClothedDef.forceNormalGearQuality;
                    randomPawnCopy.isFighter = spaceRefugeeClothedDef.isFighter;
                    randomPawnCopy.apparelAllowHeadgearChance = spaceRefugeeClothedDef.apparelAllowHeadgearChance;
                    randomPawnCopy.techHediffsMoney = spaceRefugeeClothedDef.techHediffsMoney;
                    randomPawnCopy.techHediffsTags = spaceRefugeeClothedDef.techHediffsTags;
                    randomPawnCopy.techHediffsChance = spaceRefugeeClothedDef.techHediffsChance;

                    DefDatabase<PawnKindDef>.Add(randomPawnCopy);
                }
                else {
                    var spaceRefugeeClothedDef = PawnKindDef.Named("SpaceRefugee_Clothed");
                    var randomPawnCopy = EmpireHelpers.CopyPawnKind(spaceRefugeeClothedDef);
                    randomPawnCopy.defName = $"SpaceRefugee_Clothed_{factionDef.defName}";

                    DefDatabase<PawnKindDef>.Add(randomPawnCopy);
                }

                if (debugMode) {
                    Log.Message("F");
                }

                // 커스텀 계급 규칙
                var royalImplantRuleList = new List<RoyalImplantRule>();
                if (empireFactionDef.royalImplantRules != null) {
                    for (var n = 0; n < empireFactionDef.royalImplantRules.Count; n++) {
                        // 복제
                        var baseRoyalImplantRule = empireFactionDef.royalImplantRules[n];
                        var newRoyalImplantRule = new RoyalImplantRule {
                            implantHediff = baseRoyalImplantRule.implantHediff,
                            maxLevel = baseRoyalImplantRule.maxLevel,
                            minTitle = newRoyalTitleDefList[n]
                        };

                        royalImplantRuleList.Add(newRoyalImplantRule);
                    }
                }

                factionDef.royalImplantRules = royalImplantRuleList;

                // 계급에 따른 거래제한
                if (useTradePermit) {
                    var unused = new RoyalTitlePermitDef();

                    // 기지
                    var traderKindDefList = new List<TraderKindDef>();
                    foreach (var traderKindDef in factionDef.baseTraderKinds) {
                        var newTraderKindDef = EmpireHelpers.CopyTraderKind(traderKindDef);
                        newTraderKindDef.defName = $"{traderKindDef.defName}_{factionDef.defName}_base";
                        newTraderKindDef.permitRequiredForTrading = tradeSettlementPermit;
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
                        newCaravanTraderKindDef.permitRequiredForTrading = tradeCaravanPermit;
                        newCaravanTraderKindDef.faction = factionDef;
                        DefDatabase<TraderKindDef>.Add(newCaravanTraderKindDef);
                        traderKindDefList.Add(newCaravanTraderKindDef);
                    }

                    factionDef.caravanTraderKinds = traderKindDefList;

                    // 궤도상선
                    foreach (var traderKindDef in from traders in DefDatabase<TraderKindDef>.AllDefs
                                                  where
                                                      traders.orbital && traders.faction != null && traders.faction == factionDef
                                                  select traders
                    ) {
                        traderKindDef.permitRequiredForTrading = tradeOrbitalPermit;
                    }
                }

                if (debugMode) {
                    Log.Message("G");
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