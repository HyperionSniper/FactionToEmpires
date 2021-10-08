using RimWorld;
using System.Collections.Generic;
using Verse;

namespace empireMaker
{
    public partial class EmpireMaker
    {
        /// <summary>
        /// Sets royal title tags based on tech level. Returns true if the tech level matches a valid tech level.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factionDef"></param>
        /// <returns></returns>
        public static bool SetRoyalTitleTags(ConversionSettings settings, FactionDef factionDef, TechLevel techLevel)
        {
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

        public static void SortRoyalTitleDefs(Dictionary<string, List<RoyalTitleDef>> royalTitleTagMap)
        {
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

        public static bool GenerateRoyalTitleDefs(ConversionSettings settings, FactionDef factionDef, Dictionary<string, List<RoyalTitleDef>> royalTitleTagMap, Dictionary<string, RoyalTitlePermitDef> generatedPermitDefs)
        {
            foreach (string tag in factionDef.royalTitleTags) {
                foreach (var defaultTitleDef in royalTitleTagMap[tag]) {
                    var newRoyalTitleDef = new RoyalTitleDef {
                        defName = $"{defaultTitleDef.defName}_{factionDef.defName}",
                        tags = new List<string>()
                    };
                    newRoyalTitleDef.tags.AddRange(factionDef.royalTitleTags);

                    newRoyalTitleDef.awardThought = defaultTitleDef.awardThought;
                    newRoyalTitleDef.bedroomRequirements = defaultTitleDef.bedroomRequirements;
                    newRoyalTitleDef.changeHeirQuestPoints = defaultTitleDef.changeHeirQuestPoints;
                    newRoyalTitleDef.commonality = defaultTitleDef.commonality;
                    newRoyalTitleDef.debugRandomId = defaultTitleDef.debugRandomId; // 디버그 랜덤아이디?
                    newRoyalTitleDef.decreeMentalBreakCommonality = defaultTitleDef.decreeMentalBreakCommonality;
                    newRoyalTitleDef.decreeMinIntervalDays = defaultTitleDef.decreeMinIntervalDays;
                    newRoyalTitleDef.decreeMtbDays = defaultTitleDef.decreeMtbDays;
                    newRoyalTitleDef.decreeTags = defaultTitleDef.decreeTags;
                    newRoyalTitleDef.description = defaultTitleDef.description;
                    newRoyalTitleDef.descriptionHyperlinks = defaultTitleDef.descriptionHyperlinks;
                    newRoyalTitleDef.disabledJoyKinds = defaultTitleDef.disabledJoyKinds;
                    newRoyalTitleDef.disabledWorkTags = defaultTitleDef.disabledWorkTags;
                    newRoyalTitleDef.favorCost = defaultTitleDef.favorCost;
                    newRoyalTitleDef.fileName = defaultTitleDef.fileName; // 파일명?
                    newRoyalTitleDef.foodRequirement = defaultTitleDef.foodRequirement;
                    newRoyalTitleDef.generated = defaultTitleDef.generated; // 생성완료?
                    newRoyalTitleDef.ignoreConfigErrors = defaultTitleDef.ignoreConfigErrors;
                    newRoyalTitleDef.index = defaultTitleDef.index; // 인덱스?
                    newRoyalTitleDef.inheritanceWorkerOverrideClass = defaultTitleDef.inheritanceWorkerOverrideClass;
                    newRoyalTitleDef.label = defaultTitleDef.label;
                    newRoyalTitleDef.labelFemale = defaultTitleDef.labelFemale;
                    newRoyalTitleDef.lostThought = defaultTitleDef.lostThought;
                    newRoyalTitleDef.minExpectation = defaultTitleDef.minExpectation;
                    newRoyalTitleDef.modContentPack = defaultTitleDef.modContentPack;
                    newRoyalTitleDef.modExtensions = defaultTitleDef.modExtensions;
                    //n.needFallPerDayAuthority = b.needFallPerDayAuthority;
                    if (defaultTitleDef.permits != null) {
                        newRoyalTitleDef.permits = new List<RoyalTitlePermitDef>();

                        // for each permit in the default title definition, 
                        // replace with new generated permits.
                        // generatedPermits maps default defNames to new permits.
                        // if a generated permit can't be found, simply create a copy and set the faction.
                        for (var j = 0; j < defaultTitleDef.permits.Count; j++) {
                            var oldPermit = defaultTitleDef.permits[j];
                            RoyalTitlePermitDef newPermit;

                            if (!generatedPermitDefs.TryGetValue(oldPermit.defName, out newPermit)) {
                                newPermit = EmpireHelpers.CreatePermitDefCopy(factionDef, oldPermit);
                                newPermit.faction = factionDef;
                            }

                            newRoyalTitleDef.permits.Add(newPermit);
                        }
                    }

                    newRoyalTitleDef.recruitmentResistanceOffset = defaultTitleDef.recruitmentResistanceOffset;
                    newRoyalTitleDef.replaceOnRecruited = defaultTitleDef.replaceOnRecruited;
                    newRoyalTitleDef.requiredApparel =
                        settings.WantsApparelType == WantsApparel.off ? null : defaultTitleDef.requiredApparel;

                    newRoyalTitleDef.requiredMinimumApparelQuality = defaultTitleDef.requiredMinimumApparelQuality;
                    newRoyalTitleDef.rewards = defaultTitleDef.rewards;
                    newRoyalTitleDef.seniority = defaultTitleDef.seniority;
                    newRoyalTitleDef.shortHash = defaultTitleDef.shortHash;
                    newRoyalTitleDef.suppressIdleAlert = defaultTitleDef.suppressIdleAlert;
                    newRoyalTitleDef.throneRoomRequirements = defaultTitleDef.throneRoomRequirements;

                    DefDatabase<RoyalTitleDef>.Add(newRoyalTitleDef);
                }
            }

            return true;
        }
    }
}
