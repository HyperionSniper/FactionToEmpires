using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace empireMaker
{
    public partial class EmpireMaker
    {
        private static Dictionary<TechLevel, string[]> s_RoyalTitleMap = new Dictionary<TechLevel, string[]> {
            { TechLevel.Neolithic, new string[] { "OutlanderTitle" } },
            { TechLevel.Medieval, new string[] { "OutlanderTitle" } },
            { TechLevel.Industrial, new string[] { "OutlanderTitle", "OutlanderMercenaryTitle" } },
            { TechLevel.Spacer, new string[] { "OutlanderTitle", "OutlanderMercenaryTitle" } },
            { TechLevel.Ultra, new string[] { "OutlanderTitle" } },
        };

        /// <summary>
        /// Sets royal title tags based on tech level. Returns true if the tech level matches a valid tech level.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="factionDef"></param>
        /// <returns></returns>
        private static bool SetRoyalTitleTags(ConversionSettings settings, FactionDef factionDef, TechLevel techLevel)
        {
            // set royal titles based on tech level
            bool useMercTitles = !settings.DisableMercTitles;

            switch (techLevel) {
                case TechLevel.Neolithic:
                case TechLevel.Medieval:
                case TechLevel.Industrial:
                case TechLevel.Spacer:
                case TechLevel.Ultra:
                    factionDef.royalTitleTags.AddRange(
                        s_RoyalTitleMap[techLevel]
                        .Take(useMercTitles ? 2 : 1));
                    break;

                default:
                    factionDef.royalTitleTags.Add($"OutlanderTitle");
                    return false;
            }

            return true;
        }

        private static void SortRoyalTitleDefs(Dictionary<string, List<RoyalTitleDef>> royalTitleTagMap)
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

        private static bool GenerateRoyalTitleDefs(ConversionSettings settings, FactionDef factionDef, Dictionary<string, List<RoyalTitleDef>> royalTitleTagMap, Dictionary<string, RoyalTitlePermitDef> generatedPermitDefs, out List<RoyalTitleDef> royalTitles)
        {
            royalTitles = new List<RoyalTitleDef>();

            foreach (string tag in factionDef.royalTitleTags) {
                foreach (var defaultTitleDef in royalTitleTagMap[tag]) {
                    var newRoyalTitle = new RoyalTitleDef {
                        defName = $"{defaultTitleDef.defName}_{factionDef.defName}"
                    };
                    newRoyalTitle.tags = new List<string>(
                        defaultTitleDef.tags.Select(title => title + "_" + factionDef.defName));

                    newRoyalTitle.awardThought = defaultTitleDef.awardThought;
                    newRoyalTitle.bedroomRequirements = defaultTitleDef.bedroomRequirements;
                    newRoyalTitle.changeHeirQuestPoints = defaultTitleDef.changeHeirQuestPoints;
                    newRoyalTitle.commonality = defaultTitleDef.commonality;
                    newRoyalTitle.debugRandomId = defaultTitleDef.debugRandomId; // 디버그 랜덤아이디?
                    newRoyalTitle.decreeMentalBreakCommonality = defaultTitleDef.decreeMentalBreakCommonality;
                    newRoyalTitle.decreeMinIntervalDays = defaultTitleDef.decreeMinIntervalDays;
                    newRoyalTitle.decreeMtbDays = defaultTitleDef.decreeMtbDays;
                    newRoyalTitle.decreeTags = defaultTitleDef.decreeTags;
                    newRoyalTitle.description = defaultTitleDef.description;
                    newRoyalTitle.descriptionHyperlinks = defaultTitleDef.descriptionHyperlinks;
                    newRoyalTitle.disabledJoyKinds = defaultTitleDef.disabledJoyKinds;
                    newRoyalTitle.disabledWorkTags = defaultTitleDef.disabledWorkTags;
                    newRoyalTitle.favorCost = defaultTitleDef.favorCost;
                    newRoyalTitle.fileName = defaultTitleDef.fileName; // 파일명?
                    newRoyalTitle.foodRequirement = defaultTitleDef.foodRequirement;
                    newRoyalTitle.generated = defaultTitleDef.generated; // 생성완료?
                    newRoyalTitle.ignoreConfigErrors = defaultTitleDef.ignoreConfigErrors;
                    newRoyalTitle.index = defaultTitleDef.index; // 인덱스?
                    newRoyalTitle.inheritanceWorkerOverrideClass = defaultTitleDef.inheritanceWorkerOverrideClass;
                    newRoyalTitle.label = defaultTitleDef.label;
                    newRoyalTitle.labelFemale = defaultTitleDef.labelFemale;
                    newRoyalTitle.lostThought = defaultTitleDef.lostThought;
                    newRoyalTitle.minExpectation = defaultTitleDef.minExpectation;
                    newRoyalTitle.modContentPack = defaultTitleDef.modContentPack;
                    newRoyalTitle.modExtensions = defaultTitleDef.modExtensions;
                    //n.needFallPerDayAuthority = b.needFallPerDayAuthority;
                    if (defaultTitleDef.permits != null) {
                        newRoyalTitle.permits = new List<RoyalTitlePermitDef>();

                        // for each permit in the default title definition, 
                        // replace with new generated permits.
                        // generatedPermits maps default defNames to new permits.
                        // if a generated permit can't be found, simply create a copy and set the faction.
                        for (var j = 0; j < defaultTitleDef.permits.Count; j++) {
                            var oldPermit = defaultTitleDef.permits[j];
                            RoyalTitlePermitDef newPermit;

                            if (!generatedPermitDefs.TryGetValue(oldPermit.defName, out newPermit)) {
                                newPermit = EmpireHelpers.ClonePermitDef(factionDef, oldPermit);
                                newPermit.faction = factionDef;
                            }

                            newRoyalTitle.permits.Add(newPermit);
                        }
                    }

                    newRoyalTitle.recruitmentResistanceOffset = defaultTitleDef.recruitmentResistanceOffset;
                    newRoyalTitle.replaceOnRecruited = defaultTitleDef.replaceOnRecruited;
                    newRoyalTitle.requiredApparel =
                        settings.WantsApparelType == WantsApparel.off ? null : defaultTitleDef.requiredApparel;

                    newRoyalTitle.requiredMinimumApparelQuality = defaultTitleDef.requiredMinimumApparelQuality;
                    newRoyalTitle.rewards = defaultTitleDef.rewards;
                    newRoyalTitle.seniority = defaultTitleDef.seniority;
                    newRoyalTitle.shortHash = defaultTitleDef.shortHash;
                    newRoyalTitle.suppressIdleAlert = defaultTitleDef.suppressIdleAlert;
                    newRoyalTitle.throneRoomRequirements = defaultTitleDef.throneRoomRequirements;

                    DefDatabase<RoyalTitleDef>.Add(newRoyalTitle);
                    royalTitles.Add(newRoyalTitle);
                }
            }

            return true;
        }
    }
}
