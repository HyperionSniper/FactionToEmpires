using RimWorld;
using System.Collections.Generic;

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
    }
}
