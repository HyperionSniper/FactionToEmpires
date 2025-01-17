﻿using RimWorld;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Verse;
using static empireMaker.EmpireMaker;

namespace empireMaker {
    public class EmpireHelpers {
        public static void PrintObj(object obj) {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj)) {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                Log.Message(string.Format("{0}={1}", name, value));
            }
        }

        public static bool IsRaiderFaction(FactionDef factionDef) {
            bool isPirate = false;

            List<string> pirateKeywords = new List<string>() {
                "pirate", "raider", "marauder",
            };

            foreach (var backstoryFilter in factionDef.backstoryFilters) {
                if (backstoryFilter.categories.Contains("Pirate")) {
                    isPirate = true;
                    break;
                }

                foreach (var category in backstoryFilter.categories) {
                    foreach (var keyword in pirateKeywords) {
                        if (category.ToLowerInvariant().Contains(keyword)) {
                            isPirate = true;
                            break;
                        }
                    }
                    if (isPirate) break;
                }
            }

            return isPirate;
        }

        public static TechLevel GetTechLevel(ConversionParams settings, FactionDef factionDef) {
            TechLevel techLevel;
            bool forceConversion = settings.ConversionType == Conversion.forceConversion;

            factionDef.royalTitleTags = new List<string>();

            if (forceConversion) {
                techLevel = settings.ForcedTechLevel;
            }
            else {
                techLevel = factionDef.techLevel;
            }

            return techLevel;
        }


        public static List<string> hardModsToConvert = new List<string> { "projectjedi.factions", "kikohi.forsakens" };
        public static bool IsHardModToConvert(string modName) {
            return hardModsToConvert.Contains(modName);
        }

        static Dictionary<TechLevel, string> permitMaps = new Dictionary<TechLevel, string>() {
            { TechLevel.Neolithic, "f2e_Tribal_" },
            { TechLevel.Medieval, "f2e_Medieval_" },
            { TechLevel.Industrial, "f2e_Outlander_" },
            { TechLevel.Spacer, "f2e_Spacer_" },
            { TechLevel.Ultra, "f2e_Spacer_" },
        };
        public static string GetDefNamePrefix(TechLevel techLevel) {
            return permitMaps[techLevel];
        }

        public static RoyalTitlePermitDef ClonePermitDef(FactionDef factionDef, RoyalTitlePermitDef derivedFrom)
        {
            RoyalTitlePermitDef def = new RoyalTitlePermitDef();

            Debug.Assert(factionDef != null, "Attempted to assign permit clone to null faction");
            Debug.Assert(derivedFrom != null, "Attempted to derive permit from null permit");

            def.defName = $"{derivedFrom.defName}_{factionDef.defName}";
            def.label = derivedFrom.label;
            def.description = derivedFrom.description;
            def.workerClass = derivedFrom.workerClass;
            def.minTitle = derivedFrom.minTitle;
            def.faction = factionDef;
            def.permitPointCost = derivedFrom.permitPointCost;
            def.uiPosition = derivedFrom.uiPosition;
            def.cooldownDays = derivedFrom.cooldownDays;

            def.generated = true;

            if (derivedFrom.royalAid != null) {
                def.royalAid = new RoyalAid {
                    favorCost = derivedFrom.royalAid.favorCost,

                    itemsToDrop = derivedFrom.royalAid.itemsToDrop,

                    points = derivedFrom.royalAid.points,
                    pawnCount = derivedFrom.royalAid.pawnCount,
                    pawnKindDef = derivedFrom.royalAid.pawnKindDef,

                    aidDurationDays = derivedFrom.royalAid.aidDurationDays,

                    targetingRange = derivedFrom.royalAid.targetingRange,
                    targetingRequireLOS = derivedFrom.royalAid.targetingRequireLOS,
                    radius = derivedFrom.royalAid.radius,
                    intervalTicks = derivedFrom.royalAid.intervalTicks,
                    explosionCount = derivedFrom.royalAid.explosionCount,
                    warmupTicks = derivedFrom.royalAid.warmupTicks,
                    explosionRadiusRange = derivedFrom.royalAid.explosionRadiusRange,
                };
            }

            def.usableOnWorldMap = derivedFrom.usableOnWorldMap;
            def.prerequisite = derivedFrom.prerequisite;

            return def;
        }

        public static PawnKindDef CopyPawnKind(PawnKindDef pawnKindDef) {
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

        public static TraderKindDef CopyTraderKind(TraderKindDef traderKindDef) {
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

    }
}
