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
                // REQUIRES VALID FactionDef.techLevel
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
                SortPawnKinds(settings, factionDef, out var allPawns, out var leaderPawns, out var nonLeaderPawns);
                SortFighterPawnKinds(settings, factionDef, allPawns, out var fighterPawns);

                // get royal permits & pawn types:
                //SortPermitPawnsLegacy(settings, factionDef, fighterPawns, out var permitPawns);
                //CopyEmpirePermits(factionDef, permitPawns);
                // -- or --

                if (!GenerateTradeAndMilitaryPermits(settings, factionDef, techLevel, fighterPawns)) {
                    Log.Error($"Faction {factionDef.defName} is marked for empire conversion but failed combat and trade permit generation.");
                    // keep converting anyways, but will probably be bugged
                }

                //if (!GenerateMiscPermits(settings, factionDef, techLevel)) {
                    
                //}

                // 커스텀 작위
                if (!GenerateRoyalTitleDefs(settings, factionDef, royalTitleTagMap)) {
                    Log.Error($"Faction {factionDef.defName} is marked for empire conversion but failed royal title def generation.");
                    // keep converting anyways, but will probably be bugged
                }

                if (debugMode) {
                    Log.Message("E");
                }


                // 귀족 PawnKind 생성


                var bugFixMode = settings.ConversionType == Conversion.bugFix;

                // 버그 해결 모드
                if (allPawns.Count > 0 && bugFixMode) {
                    Log.Message($" - {factionDef.defName} : make pawnkind with bugFix mode");

                    var needMakeLeader = allPawns.Count >= 6 || leaderPawns.Count > 0;

                    // pawn을 귀족으로 변환

                    for (var n = 0; n < nonLeaderPawns.Count; n++) {
                        var pawnKindDef = allPawns[n];
                        pawnKindDef.titleSelectOne =
                            needMakeLeader ? newRoyalTitleDefListNoStella : newRoyalTitleDefListNoEmperor;

                        pawnKindDef.royalTitleChance = 1f;
                        pawnKindDef.allowRoyalApparelRequirements = false; // 복장 요구 여부
                        if (pawnKindDef.techHediffsTags == null) {
                            pawnKindDef.techHediffsTags = new List<string>();
                        }

                        pawnKindDef.techHediffsTags.AddRange(new List<string>
                            {"Advanced", "ImplantEmpireRoyal", "ImplantEmpireCommon"});
                    }


                    // pawn을 팩션리더 귀족으로 변환

                    if (needMakeLeader) {
                        var pawnKindDef = leaderPawns.Count > 0
                            ? leaderPawns.RandomElement()
                            : allPawns[allPawns.Count - 1];

                        pawnKindDef.titleSelectOne = new List<RoyalTitleDef>();
                        pawnKindDef.titleRequired =
                            newRoyalTitleDefListNoEmperor[newRoyalTitleDefListNoEmperor.Count - 1];
                        pawnKindDef.royalTitleChance = 1f;
                        pawnKindDef.allowRoyalApparelRequirements = false;
                        if (pawnKindDef.techHediffsTags == null) {
                            pawnKindDef.techHediffsTags = new List<string>();
                        }

                        pawnKindDef.techHediffsTags.AddRange(new List<string>
                            {"Advanced", "ImplantEmpireRoyal", "ImplantEmpireCommon"});
                    }
                }


                // 일반 모드
                if (allPawns.Count > 0 && !bugFixMode) {
                    Log.Message($" - {factionDef.defName} : make pawnkind");

                    for (var n = 1; n < newRoyalTitleDefList.Count - 1; n++) {
                        var royalTitleDef = newRoyalTitleDefList[n];
                        var randomPawn = nonLeaderPawns[nonLeaderPawns.Count - 1];
                        var newPawn = new PawnKindDef();
                        var titleBasePawn = royalPawnKindDefList[n - 1];

                        if (n == newRoyalTitleDefList.Count - 2 && leaderPawns.Count > 0) {
                            randomPawn = leaderPawns.RandomElement(); // stellach 계급은 무조건 리더이어야하므로 리더 값을 베이스로 생성
                        }

                        newPawn.defName = $"royal_{royalTitleDef.defName}_{factionDef.defName}"; //
                        newPawn.label = titleBasePawn.label; // 라벨
                        newPawn.titleRequired = newRoyalTitleDefList[n]; // 정해진 계급

                        newPawn.aiAvoidCover = titleBasePawn.aiAvoidCover;

                        switch (useApparel) // 귀족 폰이 의복을 요구할지 여부
                        {
                            case WantsApparel.off:
                                // 사용안함
                                newPawn.allowRoyalApparelRequirements = false; // 복장 요구 여부
                                newPawn.apparelDisallowTags = randomPawn.apparelDisallowTags; // 금지 복장
                                newPawn.apparelTags = randomPawn.apparelTags; // 허용복장
                                newPawn.apparelColor = randomPawn.apparelColor;
                                break;
                            case WantsApparel.forcedRoyal:
                                // 바닐라
                                newPawn.allowRoyalApparelRequirements =
                                    titleBasePawn.allowRoyalApparelRequirements; // 복장 요구 여부
                                newPawn.apparelTags = new List<string> { "IndustrialBasic" };
                                newPawn.apparelDisallowTags = titleBasePawn.apparelDisallowTags; // 금지 복장
                                newPawn.specificApparelRequirements =
                                    titleBasePawn.specificApparelRequirements; // 귀족 의상 강제
                                newPawn.apparelColor = titleBasePawn.apparelColor;
                                break;
                            default:
                                // 모드아이템
                                newPawn.allowRoyalApparelRequirements =
                                    titleBasePawn.allowRoyalApparelRequirements; // 복장 요구 여부
                                newPawn.apparelTags = randomPawn.apparelTags; // 허용 복장
                                newPawn.specificApparelRequirements =
                                    titleBasePawn.specificApparelRequirements; // 귀족 의상 강제
                                newPawn.apparelColor = randomPawn.apparelColor;
                                break;
                        }

                        newPawn.allowRoyalRoomRequirements = titleBasePawn.allowRoyalRoomRequirements;

                        newPawn.alternateGraphicChance = randomPawn.alternateGraphicChance;
                        newPawn.alternateGraphics = randomPawn.alternateGraphics;
                        newPawn.apparelAllowHeadgearChance = titleBasePawn.apparelAllowHeadgearChance; //


                        newPawn.apparelIgnoreSeasons = titleBasePawn.apparelIgnoreSeasons; //
                        newPawn.apparelMoney = titleBasePawn.apparelMoney; // 복장 가격
                        newPawn.apparelRequired = titleBasePawn.apparelRequired; // 복장 강제


                        newPawn.backstoryCategories = randomPawn.backstoryCategories;
                        newPawn.backstoryCryptosleepCommonality = randomPawn.backstoryCryptosleepCommonality;
                        newPawn.backstoryFilters = randomPawn.backstoryFilters;
                        newPawn.backstoryFiltersOverride = randomPawn.backstoryFiltersOverride;
                        newPawn.baseRecruitDifficulty = titleBasePawn.baseRecruitDifficulty; //
                        newPawn.biocodeWeaponChance = titleBasePawn.biocodeWeaponChance; //
                        newPawn.canArriveManhunter = randomPawn.canArriveManhunter;
                        newPawn.canBeSapper = randomPawn.canBeSapper;
                        newPawn.chemicalAddictionChance = randomPawn.chemicalAddictionChance;
                        newPawn.combatEnhancingDrugsChance = randomPawn.combatEnhancingDrugsChance;
                        newPawn.combatEnhancingDrugsCount = randomPawn.combatEnhancingDrugsCount;
                        newPawn.combatPower = randomPawn.combatPower;
                        newPawn.debugRandomId = randomPawn.debugRandomId;
                        newPawn.defaultFactionType = factionDef; // 기본 팩션
                        newPawn.defendPointRadius = randomPawn.defendPointRadius;

                        newPawn.description = randomPawn.description;
                        newPawn.descriptionHyperlinks = randomPawn.descriptionHyperlinks;
                        newPawn.destroyGearOnDrop = randomPawn.destroyGearOnDrop;

                        newPawn.disallowedTraits = randomPawn.disallowedTraits == null
                            ? new List<TraitDef>()
                            : randomPawn.disallowedTraits.ListFullCopy();

                        newPawn.disallowedTraits.Add(TraitDefOf.Nudist);
                        newPawn.disallowedTraits.Add(TraitDefOf.Brawler);


                        newPawn.ecoSystemWeight = randomPawn.ecoSystemWeight;
                        newPawn.factionLeader = titleBasePawn.factionLeader; // 팩션리더?
                        newPawn.fileName = randomPawn.fileName; // 파일명
                        newPawn.fixedInventory = randomPawn.fixedInventory;
                        newPawn.fleeHealthThresholdRange = randomPawn.fleeHealthThresholdRange;
                        newPawn.forceNormalGearQuality = randomPawn.forceNormalGearQuality;
                        newPawn.gearHealthRange = randomPawn.gearHealthRange;
                        newPawn.generated = titleBasePawn.generated; // 생성완료?
                        newPawn.acceptArrestChanceFactor = titleBasePawn.acceptArrestChanceFactor;
                        newPawn.ignoreConfigErrors = randomPawn.ignoreConfigErrors;
                        newPawn.index = titleBasePawn.index; // 인덱스?
                        newPawn.inventoryOptions = titleBasePawn.inventoryOptions;
                        newPawn.invFoodDef = titleBasePawn.invFoodDef;
                        newPawn.invNutrition = titleBasePawn.invNutrition;
                        newPawn.isFighter = titleBasePawn.isFighter;
                        newPawn.itemQuality = titleBasePawn.itemQuality;

                        newPawn.labelFemale = randomPawn.labelFemale;
                        newPawn.labelFemalePlural = randomPawn.labelFemalePlural;
                        newPawn.labelMale = randomPawn.labelMale;
                        newPawn.labelMalePlural = randomPawn.labelMalePlural;
                        newPawn.labelPlural = randomPawn.labelPlural;
                        newPawn.lifeStages = randomPawn.lifeStages;
                        newPawn.maxGenerationAge = randomPawn.maxGenerationAge;
                        newPawn.minGenerationAge = randomPawn.minGenerationAge;
                        newPawn.modContentPack = randomPawn.modContentPack; // 모드?
                        newPawn.modExtensions = randomPawn.modExtensions;
                        newPawn.race = randomPawn.race; // 종족?
                        newPawn.royalTitleChance = 1f; // 귀족으로 전환할 확률
                        newPawn.shortHash = randomPawn.shortHash;
                        newPawn.skills = randomPawn.skills;

                        newPawn.techHediffsChance = titleBasePawn.techHediffsChance;

                        newPawn.techHediffsDisallowTags = randomPawn.techHediffsDisallowTags == null
                            ? new List<string>()
                            : randomPawn.techHediffsDisallowTags.ListFullCopy();

                        newPawn.techHediffsDisallowTags.Add("PainCauser");


                        newPawn.techHediffsMaxAmount = titleBasePawn.techHediffsMaxAmount;
                        newPawn.techHediffsMoney = titleBasePawn.techHediffsMoney;
                        newPawn.techHediffsRequired = titleBasePawn.techHediffsRequired;

                        newPawn.techHediffsTags = randomPawn.techHediffsTags == null
                            ? new List<string>()
                            : randomPawn.techHediffsTags.ListFullCopy();

                        newPawn.techHediffsTags.AddRange(new List<string>
                            {"Advanced", "ImplantEmpireRoyal", "ImplantEmpireCommon"});


                        newPawn.trader = false; // 상인
                        newPawn.weaponMoney = titleBasePawn.weaponMoney;
                        newPawn.weaponTags = randomPawn.weaponTags;
                        newPawn.wildGroupSize = titleBasePawn.wildGroupSize;

                        DefDatabase<PawnKindDef>.Add(newPawn);
                    }
                }


                // Pawn Kinds 생성 : SpaceRefugee_Clothed
                if (nonLeaderPawns.Count > 0) {
                    var spaceRefugeeClothedDef = PawnKindDef.Named("SpaceRefugee_Clothed");
                    var randomPawnCopy = CopyPawnDef(nonLeaderPawns[0]);
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
                    var randomPawnCopy = CopyPawnDef(spaceRefugeeClothedDef);
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
                        var newTraderKindDef = CopyTraderDef(traderKindDef);
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
                        var newCaravanTraderKindDef = CopyTraderDef(caravanTraderKindDef);
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


        public static PawnKindDef CopyPawnDef(PawnKindDef pawnKindDef) {
            var newPawnKindDef = new PawnKindDef {
                defName = pawnKindDef.defName,
                label = pawnKindDef.label,
                aiAvoidCover = pawnKindDef.aiAvoidCover,
                allowRoyalApparelRequirements = pawnKindDef.allowRoyalApparelRequirements,
                allowRoyalRoomRequirements = pawnKindDef.allowRoyalRoomRequirements,
                alternateGraphicChance = pawnKindDef.alternateGraphicChance,
                alternateGraphics = pawnKindDef.alternateGraphics,
                apparelAllowHeadgearChance = pawnKindDef.apparelAllowHeadgearChance, //
                apparelColor = pawnKindDef.apparelColor,
                apparelDisallowTags = pawnKindDef.apparelDisallowTags,
                apparelIgnoreSeasons = pawnKindDef.apparelIgnoreSeasons,
                apparelMoney = pawnKindDef.apparelMoney,
                apparelRequired = pawnKindDef.apparelRequired,
                apparelTags = pawnKindDef.apparelTags,
                backstoryCategories = pawnKindDef.backstoryCategories,
                backstoryCryptosleepCommonality = pawnKindDef.backstoryCryptosleepCommonality,
                backstoryFilters = pawnKindDef.backstoryFilters,
                backstoryFiltersOverride = pawnKindDef.backstoryFiltersOverride,
                baseRecruitDifficulty = pawnKindDef.baseRecruitDifficulty, //
                biocodeWeaponChance = pawnKindDef.biocodeWeaponChance, //
                canArriveManhunter = pawnKindDef.canArriveManhunter,
                canBeSapper = pawnKindDef.canBeSapper,
                chemicalAddictionChance = pawnKindDef.chemicalAddictionChance,
                combatEnhancingDrugsChance = pawnKindDef.combatEnhancingDrugsChance,
                combatEnhancingDrugsCount = pawnKindDef.combatEnhancingDrugsCount,
                combatPower = pawnKindDef.combatPower,
                debugRandomId = pawnKindDef.debugRandomId,
                defaultFactionType = pawnKindDef.defaultFactionType,
                defendPointRadius = pawnKindDef.defendPointRadius,
                description = pawnKindDef.description,
                descriptionHyperlinks = pawnKindDef.descriptionHyperlinks,
                destroyGearOnDrop = pawnKindDef.destroyGearOnDrop,
                disallowedTraits = pawnKindDef.disallowedTraits,
                ecoSystemWeight = pawnKindDef.ecoSystemWeight,
                factionLeader = pawnKindDef.factionLeader, // 팩션리더?
                fileName = pawnKindDef.fileName,
                fixedInventory = pawnKindDef.fixedInventory,
                fleeHealthThresholdRange = pawnKindDef.fleeHealthThresholdRange,
                forceNormalGearQuality = pawnKindDef.forceNormalGearQuality,
                gearHealthRange = pawnKindDef.gearHealthRange,
                generated = pawnKindDef.generated, // 생성완료?
                acceptArrestChanceFactor = pawnKindDef.acceptArrestChanceFactor,
                ignoreConfigErrors = pawnKindDef.ignoreConfigErrors,
                index = pawnKindDef.index, // 인덱스?
                inventoryOptions = pawnKindDef.inventoryOptions,
                invFoodDef = pawnKindDef.invFoodDef,
                invNutrition = pawnKindDef.invNutrition,
                isFighter = pawnKindDef.isFighter,
                itemQuality = pawnKindDef.itemQuality,
                labelFemale = pawnKindDef.labelFemale,
                labelFemalePlural = pawnKindDef.labelFemalePlural,
                labelMale = pawnKindDef.labelMale,
                labelMalePlural = pawnKindDef.labelMalePlural,
                labelPlural = pawnKindDef.labelPlural,
                lifeStages = pawnKindDef.lifeStages,
                maxGenerationAge = pawnKindDef.maxGenerationAge,
                minGenerationAge = pawnKindDef.minGenerationAge,
                modContentPack = pawnKindDef.modContentPack, // 모드?
                modExtensions = pawnKindDef.modExtensions,
                race = pawnKindDef.race, // 종족?
                royalTitleChance = 1f,
                shortHash = pawnKindDef.shortHash,
                skills = pawnKindDef.skills,
                specificApparelRequirements = pawnKindDef.specificApparelRequirements, // 귀족 의상
                techHediffsChance = pawnKindDef.techHediffsChance,
                techHediffsDisallowTags = pawnKindDef.techHediffsDisallowTags,
                techHediffsMaxAmount = pawnKindDef.techHediffsMaxAmount,
                techHediffsMoney = pawnKindDef.techHediffsMoney,
                techHediffsRequired = pawnKindDef.techHediffsRequired,
                techHediffsTags = pawnKindDef.techHediffsTags,
                titleRequired = pawnKindDef.titleRequired, // 정해진 계급
                trader = pawnKindDef.trader,
                weaponMoney = pawnKindDef.weaponMoney,
                weaponTags = pawnKindDef.weaponTags,
                wildGroupSize = pawnKindDef.wildGroupSize
            };

            return newPawnKindDef;
        }

        public static TraderKindDef CopyTraderDef(TraderKindDef traderKindDef) {
            var newTraderKindDef = new TraderKindDef {
                defName = traderKindDef.defName,
                permitRequiredForTrading = traderKindDef.permitRequiredForTrading,
                faction = traderKindDef.faction,
                category = traderKindDef.category,
                commonality = traderKindDef.commonality,
                commonalityMultFromPopulationIntent = traderKindDef.commonalityMultFromPopulationIntent,
                description = traderKindDef.description,
                descriptionHyperlinks = traderKindDef.descriptionHyperlinks,
                hideThingsNotWillingToTrade = traderKindDef.hideThingsNotWillingToTrade,
                ignoreConfigErrors = traderKindDef.ignoreConfigErrors,
                label = traderKindDef.label,
                modContentPack = traderKindDef.modContentPack,
                modExtensions = traderKindDef.modExtensions,
                orbital = traderKindDef.orbital,
                requestable = traderKindDef.requestable,
                shortHash = traderKindDef.shortHash,
                stockGenerators = traderKindDef.stockGenerators,
                tradeCurrency = traderKindDef.tradeCurrency
            };

            return newTraderKindDef;
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