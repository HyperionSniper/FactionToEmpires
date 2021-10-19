using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace empireMaker {
    public partial class EmpireMaker {
        public static void GeneratePawnKindsBugFix(ConversionParams settings, FactionDef factionDef, List<RoyalTitleDef> royalTitles, List<PawnKindDef> allPawns, List<PawnKindDef> leaderPawns, List<PawnKindDef> nonLeaderPawns) {

            Log.Message($" - {factionDef.defName} : make pawnkind with bugFix mode");

            var needMakeLeader = allPawns.Count >= 6 || leaderPawns.Count > 0;

            // pawn을 귀족으로 변환
            // 
            for (var n = 0; n < nonLeaderPawns.Count; n++) {
                var pawnKindDef = allPawns[n];
                pawnKindDef.titleSelectOne = royalTitles;

                pawnKindDef.royalTitleChance = 1f;
                pawnKindDef.allowRoyalApparelRequirements = false; // 복장 요구 여부
                if (pawnKindDef.techHediffsTags == null) {
                    pawnKindDef.techHediffsTags = new List<string>();
                }

                pawnKindDef.techHediffsTags.AddRange(new List<string> {"Advanced", "ImplantEmpireRoyal", "ImplantEmpireCommon"});
            }


            // pawn을 팩션리더 귀족으로 변환

            if (needMakeLeader) {
                var pawnKindDef = leaderPawns.Count > 0
                    ? leaderPawns.RandomElement()
                    : allPawns[allPawns.Count - 1];

                pawnKindDef.titleSelectOne = new List<RoyalTitleDef>();
                pawnKindDef.titleRequired = royalTitles[royalTitles.Count - 1]; // TODO: hmmm
                pawnKindDef.royalTitleChance = 1f;
                pawnKindDef.allowRoyalApparelRequirements = false;
                if (pawnKindDef.techHediffsTags == null) {
                    pawnKindDef.techHediffsTags = new List<string>();
                }

                pawnKindDef.techHediffsTags.AddRange(new List<string>
                    {"Advanced", "ImplantEmpireRoyal", "ImplantEmpireCommon"});
            }
        }


        private static List<PawnKindDef> royalPawnKindDefList = new List<PawnKindDef> {
            PawnKindDef.Named("Empire_Royal_Yeoman"),
            PawnKindDef.Named("Empire_Royal_Acolyte"),
            PawnKindDef.Named("Empire_Royal_Knight"),
            PawnKindDef.Named("Empire_Royal_Praetor"),
            PawnKindDef.Named("Empire_Royal_Baron"),
            PawnKindDef.Named("Empire_Royal_Count"),
            PawnKindDef.Named("Empire_Royal_Duke"),
            PawnKindDef.Named("Empire_Royal_Consul"),
            PawnKindDef.Named("Empire_Royal_Stellarch")
        };

        private static void GeneratePawnKinds(ConversionParams settings, FactionDef factionDef, List<RoyalTitleDef> royalTitles, List<PawnKindDef> allPawns, List<PawnKindDef> leaderPawns, List<PawnKindDef> nonLeaderPawns) {

            Log.Message($" - {factionDef.defName} : make pawnkind");

            for (var n = 1; n < royalTitles.Count - 1; n++) {
                var royalTitleDef = royalTitles[n];
                var randomPawn = nonLeaderPawns[nonLeaderPawns.Count - 1];
                var newPawn = new PawnKindDef();

                // default pawn
                var defaultPawn = royalPawnKindDefList[n - 1];

                newPawn.defName = $"{defaultPawn.defName}_{factionDef.defName}"; //
                newPawn.label = defaultPawn.label; // 라벨
                newPawn.titleRequired = royalTitles[n]; // 정해진 계급

                newPawn.aiAvoidCover = defaultPawn.aiAvoidCover;

                switch (settings.WantsApparelType) // 귀족 폰이 의복을 요구할지 여부
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
                            defaultPawn.allowRoyalApparelRequirements; // 복장 요구 여부
                        newPawn.apparelTags = new List<string> { "IndustrialBasic" };
                        newPawn.apparelDisallowTags = defaultPawn.apparelDisallowTags; // 금지 복장
                        newPawn.specificApparelRequirements =
                            defaultPawn.specificApparelRequirements; // 귀족 의상 강제
                        newPawn.apparelColor = defaultPawn.apparelColor;
                        break;
                    default:
                        // 모드아이템
                        newPawn.allowRoyalApparelRequirements =
                            defaultPawn.allowRoyalApparelRequirements; // 복장 요구 여부
                        newPawn.apparelTags = randomPawn.apparelTags; // 허용 복장
                        newPawn.specificApparelRequirements =
                            defaultPawn.specificApparelRequirements; // 귀족 의상 강제
                        newPawn.apparelColor = randomPawn.apparelColor;
                        break;
                }

                newPawn.allowRoyalRoomRequirements = defaultPawn.allowRoyalRoomRequirements;

                newPawn.alternateGraphicChance = randomPawn.alternateGraphicChance;
                newPawn.alternateGraphics = randomPawn.alternateGraphics;
                newPawn.apparelAllowHeadgearChance = defaultPawn.apparelAllowHeadgearChance; //


                newPawn.apparelIgnoreSeasons = defaultPawn.apparelIgnoreSeasons; //
                newPawn.apparelMoney = defaultPawn.apparelMoney; // 복장 가격
                newPawn.apparelRequired = defaultPawn.apparelRequired; // 복장 강제


                newPawn.backstoryCategories = randomPawn.backstoryCategories;
                newPawn.backstoryCryptosleepCommonality = randomPawn.backstoryCryptosleepCommonality;
                newPawn.backstoryFilters = randomPawn.backstoryFilters;
                newPawn.backstoryFiltersOverride = randomPawn.backstoryFiltersOverride;
                newPawn.baseRecruitDifficulty = defaultPawn.baseRecruitDifficulty; //
                newPawn.biocodeWeaponChance = defaultPawn.biocodeWeaponChance; //
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
                newPawn.factionLeader = defaultPawn.factionLeader; // 팩션리더?
                newPawn.fileName = randomPawn.fileName; // 파일명
                newPawn.fixedInventory = randomPawn.fixedInventory;
                newPawn.fleeHealthThresholdRange = randomPawn.fleeHealthThresholdRange;
                newPawn.forceNormalGearQuality = randomPawn.forceNormalGearQuality;
                newPawn.gearHealthRange = randomPawn.gearHealthRange;
                newPawn.generated = defaultPawn.generated; // 생성완료?
                newPawn.acceptArrestChanceFactor = defaultPawn.acceptArrestChanceFactor;
                newPawn.ignoreConfigErrors = randomPawn.ignoreConfigErrors;
                newPawn.index = defaultPawn.index; // 인덱스?
                newPawn.inventoryOptions = defaultPawn.inventoryOptions;
                newPawn.invFoodDef = defaultPawn.invFoodDef;
                newPawn.invNutrition = defaultPawn.invNutrition;
                newPawn.isFighter = defaultPawn.isFighter;
                newPawn.itemQuality = defaultPawn.itemQuality;

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

                newPawn.techHediffsChance = defaultPawn.techHediffsChance;

                newPawn.techHediffsDisallowTags = randomPawn.techHediffsDisallowTags == null
                    ? new List<string>()
                    : randomPawn.techHediffsDisallowTags.ListFullCopy();

                newPawn.techHediffsDisallowTags.Add("PainCauser");

                newPawn.techHediffsMaxAmount = defaultPawn.techHediffsMaxAmount;
                newPawn.techHediffsMoney = defaultPawn.techHediffsMoney;
                newPawn.techHediffsRequired = defaultPawn.techHediffsRequired;

                newPawn.techHediffsTags = randomPawn.techHediffsTags == null
                    ? new List<string>()
                    : randomPawn.techHediffsTags.ListFullCopy();

                newPawn.techHediffsTags.AddRange(new List<string>
                    {"Advanced", "ImplantEmpireRoyal", "ImplantEmpireCommon"});


                newPawn.trader = false; // 상인
                newPawn.weaponMoney = defaultPawn.weaponMoney;
                newPawn.weaponTags = randomPawn.weaponTags;
                newPawn.wildGroupSize = defaultPawn.wildGroupSize;

                DefDatabase<PawnKindDef>.Add(newPawn);
            }
        }
    }
}
