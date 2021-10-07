﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static empireMaker.EmpireMaker;

namespace empireMaker {
    public class EmpireHelpers {
        public static TechLevel GetTechLevel(ConversionSettings settings, FactionDef factionDef) {
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


        public static List<string> unmoddedFactionDefNames = new List<string> {
            // vanilla
            "TribeCivil", // neolithic
            "TribeRough",
            "TribeSavage",

            "OutlanderCivil", // industrial
            "OutlanderRough",

            "Pirate", // spacer

            // royalty - already empire

            // ideology
            "TribeCannibal", // neolithic
            "NudistTribe",

            "CannibalPirate",  // spacer
        };
        public static bool IsUnmoddedFaction(string defName) {
            return unmoddedFactionDefNames.Contains(defName);
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
    }
}
