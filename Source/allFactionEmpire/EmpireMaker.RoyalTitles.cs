using RimWorld;
using System.Collections.Generic;
using Verse;

namespace empireMaker {
    public partial class EmpireMaker {
        /// <summary>
        /// Sets royal title tags based on tech level. Returns true if the tech level matches a valid tech level.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factionDef"></param>
        /// <returns></returns>
        public static bool SetRoyalTitleTags(ConversionSettings settings, FactionDef factionDef, TechLevel techLevel) {
            // set royal titles based on tech level
            bool useMercenaryTitles = !settings.DisableMercTitles;

            switch (techLevel) {
                case TechLevel.Neolithic:
                    factionDef.royalTitleTags.Add("OutlanderTitle"); // TODO
                    break;

                case TechLevel.Medieval:
                    factionDef.royalTitleTags.Add("OutlanderTitle"); // TODO
                    break;

                case TechLevel.Industrial:
                    factionDef.royalTitleTags.Add("OutlanderTitle");

                    if (useMercenaryTitles) {
                        factionDef.royalTitleTags.Add("OutlanderMercenaryTitle");
                    }
                    break;

                case TechLevel.Spacer:
                    factionDef.royalTitleTags.Add("OutlanderTitle"); // TODO

                    if (useMercenaryTitles) {
                        factionDef.royalTitleTags.Add("OutlanderMercenaryTitle");
                    }
                    break;

                case TechLevel.Ultra:
                    factionDef.royalTitleTags.Add("OutlanderTitle"); // TODO
                    break;

                default:
                    factionDef.royalTitleTags.Add($"{factionDef.defName}Title");
                    return false;
            }

            return true;
        }

        public static void SortRoyalTitleDefs(Dictionary<string, List<RoyalTitleDef>> royalTitleTagMap) {
            // sort all royal title defs into title types
            foreach (var title in DefDatabase<RoyalTitleDef>.AllDefs) {
                // for each individual royal title, assign it to a single List in the royalTitleTagMap.
                foreach (var titleType in royalTitleTagMap) {
                    if (title.tags.Contains(titleType.Key)) {
                        titleType.Value.Add(title);
                        break;
                    }
                }
            }

            foreach (var list in royalTitleTagMap.Values) {
                // sort by favor cost
                list.SortBy(t => t.favorCost);
            }
        }

        public static bool GenerateRoyalTitleDefs(ConversionSettings settings, FactionDef factionDef, Dictionary<string, List<RoyalTitleDef>> royalTitleTagMap) {
            foreach (string tag in factionDef.royalTitleTags) {
                foreach (var royalTitleDef in royalTitleTagMap[tag]) {
                    var newRoyalTitleDef = new RoyalTitleDef {
                        defName = $"{royalTitleDef.defName}_{factionDef.defName}",
                        tags = new List<string>()
                    };
                    newRoyalTitleDef.tags.AddRange(factionDef.royalTitleTags);

                    newRoyalTitleDef.awardThought = royalTitleDef.awardThought;
                    newRoyalTitleDef.bedroomRequirements = royalTitleDef.bedroomRequirements;
                    newRoyalTitleDef.changeHeirQuestPoints = royalTitleDef.changeHeirQuestPoints;
                    newRoyalTitleDef.commonality = royalTitleDef.commonality;
                    newRoyalTitleDef.debugRandomId = royalTitleDef.debugRandomId; // 디버그 랜덤아이디?
                    newRoyalTitleDef.decreeMentalBreakCommonality = royalTitleDef.decreeMentalBreakCommonality;
                    newRoyalTitleDef.decreeMinIntervalDays = royalTitleDef.decreeMinIntervalDays;
                    newRoyalTitleDef.decreeMtbDays = royalTitleDef.decreeMtbDays;
                    newRoyalTitleDef.decreeTags = royalTitleDef.decreeTags;
                    newRoyalTitleDef.description = royalTitleDef.description;
                    newRoyalTitleDef.descriptionHyperlinks = royalTitleDef.descriptionHyperlinks;
                    newRoyalTitleDef.disabledJoyKinds = royalTitleDef.disabledJoyKinds;
                    newRoyalTitleDef.disabledWorkTags = royalTitleDef.disabledWorkTags;
                    newRoyalTitleDef.favorCost = royalTitleDef.favorCost;
                    newRoyalTitleDef.fileName = royalTitleDef.fileName; // 파일명?
                    newRoyalTitleDef.foodRequirement = royalTitleDef.foodRequirement;
                    newRoyalTitleDef.generated = royalTitleDef.generated; // 생성완료?
                    newRoyalTitleDef.ignoreConfigErrors = royalTitleDef.ignoreConfigErrors;
                    newRoyalTitleDef.index = royalTitleDef.index; // 인덱스?
                    newRoyalTitleDef.inheritanceWorkerOverrideClass = royalTitleDef.inheritanceWorkerOverrideClass;
                    newRoyalTitleDef.label = royalTitleDef.label;
                    newRoyalTitleDef.labelFemale = royalTitleDef.labelFemale;
                    newRoyalTitleDef.lostThought = royalTitleDef.lostThought;
                    newRoyalTitleDef.minExpectation = royalTitleDef.minExpectation;
                    newRoyalTitleDef.modContentPack = royalTitleDef.modContentPack;
                    newRoyalTitleDef.modExtensions = royalTitleDef.modExtensions;
                    //n.needFallPerDayAuthority = b.needFallPerDayAuthority;
                    if (royalTitleDef.permits != null) {
                        newRoyalTitleDef.permits = new List<RoyalTitlePermitDef>();
                        for (var j = 0; j < royalTitleDef.permits.Count; j++) {
                            var newPermit = royalTitleDef.permits[j];


                            newRoyalTitleDef.permits.Add(newPermit);
                        }
                    }

                    newRoyalTitleDef.recruitmentResistanceOffset = royalTitleDef.recruitmentResistanceOffset;
                    newRoyalTitleDef.replaceOnRecruited = royalTitleDef.replaceOnRecruited;
                    newRoyalTitleDef.requiredApparel =
                        useApparel != WantsApparel.off ? royalTitleDef.requiredApparel : null;

                    newRoyalTitleDef.requiredMinimumApparelQuality = royalTitleDef.requiredMinimumApparelQuality;
                    newRoyalTitleDef.rewards = royalTitleDef.rewards;
                    newRoyalTitleDef.seniority = royalTitleDef.seniority;
                    newRoyalTitleDef.shortHash = royalTitleDef.shortHash;
                    newRoyalTitleDef.suppressIdleAlert = royalTitleDef.suppressIdleAlert;
                    newRoyalTitleDef.throneRoomRequirements = royalTitleDef.throneRoomRequirements;

                    DefDatabase<RoyalTitleDef>.Add(newRoyalTitleDef);
                }
            }

            return true;
        }
    }
}
