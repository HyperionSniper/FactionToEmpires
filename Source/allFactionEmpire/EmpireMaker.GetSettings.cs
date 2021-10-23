using HugsLib.Settings;
using RimWorld;
using Verse;

namespace empireMaker {
    public partial class EmpireMaker : HugsLib.ModBase
    {

        public static bool phychicAll = true;
        public static bool delVanilla;
        public static float questAmount = 1f;
        public static bool debugMode;

        public enum Conversion {
            noConversion,
            empire,
            bugFix,
            forceConversion
        }

        public enum Relationship {
            basic,
            empire,
            ally,
            neutral,
            enemy,
            permanentEnemy
        }
        public enum WantsApparel {
            off,
            forcedRoyal,
            basic
        }
        public enum EmpireTechLevel {
            neolithic = 2,
            medieval = 3,
            industrial = 4,
            spacer = 5,
            ultra = 6,
        }

        public enum EmpireArchetype {
            Neolithic, // all neolithic
            Medieval, // all medieval
            IndustrialRaider, // industrial raider
            IndustrialOutlander, // industrial non-raider
            SpacerRaider, // spacer raider
            Spacer, // spacer non-raider
            Ultra // ultra
        }

        public class ConversionParams {
            public Conversion ConversionType;

            public TechLevel ForcedTechLevel;
            public TechLevel ActualTechLevel;

            public bool DisableMercTitles;
            public bool IsRaiderFaction;

            public Relationship RelationshipType;
            public WantsApparel WantsApparelType;
            public bool RequiresTradePermit;

            public SettingHandle<Relationship> RelationshipHandle;

            public TechLevel EffectiveTechLevel {
                get {
                    if (ConversionType == Conversion.forceConversion) {
                        return ForcedTechLevel;
                    }
                    else {
                        return ActualTechLevel;
                    }
                }
            }

            public EmpireArchetype Archetype {
                get {
                    switch (EffectiveTechLevel) {
                        case TechLevel.Neolithic:
                            return EmpireArchetype.Neolithic;

                        case TechLevel.Medieval:
                            return EmpireArchetype.Medieval;

                        case TechLevel.Industrial:
                            if (IsRaiderFaction) {
                                return EmpireArchetype.IndustrialRaider;
                            }
                            else {
                                return EmpireArchetype.IndustrialOutlander;
                            }

                        case TechLevel.Spacer:
                            if (IsRaiderFaction) {
                                return EmpireArchetype.SpacerRaider;
                            }
                            else {
                                return EmpireArchetype.Spacer;
                            }

                        case TechLevel.Ultra:
                            return EmpireArchetype.Ultra;

                        default: return EmpireArchetype.IndustrialOutlander;
                    }
                }
            }
        }

        public override void SettingsChanged() {
            phychicAll = phychicAllSetting.Value;
            delVanilla = delVanillaSetting.Value;

            if (questAmountSetting.Value < 0.1f) {
                questAmountSetting.Value = 0.1f;
            }
            else if (questAmountSetting.Value > 20f) {
                questAmountSetting.Value = 20f;
            }

            questAmount = questAmountSetting.Value;

            debugMode = debugModeSetting.Value;

            for (var i = 0; i < factionConversionSettings.Count; i++) {
                var settings = factionConversionSettings[i];

                settings.RelationshipType = settings.RelationshipHandle.Value;

                // forced tech level must be a valid tech level
                if (settings.ConversionType == Conversion.forceConversion) {
                    switch (settings.ForcedTechLevel) {
                        case TechLevel.Neolithic:
                        case TechLevel.Medieval:
                        case TechLevel.Industrial:
                        case TechLevel.Spacer:
                        case TechLevel.Ultra:
                            continue;

                        default:
                            Log.Warning($"Setting forced tech level of faction to Industrial, was {settings.ForcedTechLevel}.");
                            settings.ForcedTechLevel = TechLevel.Industrial;
                            break;
                    }
                }
            }
        }

        public Conversion GetMakeType(FactionDef factionDef, Conversion defaultValue) {
            return Settings
                .GetHandle(
                    $"{factionDef.defName}ToEmpire", $"==== {factionDef.label.ToUpper()} ====", "make_d".Translate(),
                    defaultValue, null, "en_make_")
                .Value;
        }

        public void GetSettings() {
            // 초능력 공용화
            phychicAllSetting =
                Settings.GetHandle("phychicAll", "phychicAll_t".Translate(), "phychicAll_d".Translate(), true);
            phychicAll = phychicAllSetting.Value;

            // 바닐라 제국 숨기기
            delVanillaSetting =
                Settings.GetHandle<bool>("delVanilla", "delVanilla_t".Translate(), "delVanilla_d".Translate());
            delVanillaSetting.NeverVisible = true;
            delVanilla = delVanillaSetting.Value;

            // 퀘스트 생성량
            questAmountSetting = Settings.GetHandle("questAmount", "questAmount_t".Translate(),
                "questAmount_d".Translate(), 1f);
            questAmountSetting.NeverVisible = true;
            questAmount = questAmountSetting.Value;

            for (var i = 0; i < eligibleFactions.Count; i++) {
                // 옵션 관리
                var factionDef = eligibleFactions[i];
                var isHardMod = EmpireHelpers.IsHardModToConvert(factionDef.modContentPack.PackageId);

                ConversionParams settings = new ConversionParams();

                // 제국화
                Conversion makeType;
                if (factionDef.permanentEnemy) {
                    makeType = GetMakeType(factionDef, Conversion.noConversion);
                }
                else if (isHardMod) {
                    makeType = GetMakeType(factionDef, Conversion.bugFix);

                    if (makeType == Conversion.empire) {
                        makeType = Conversion.bugFix;
                    }
                }
                else {
                    makeType = GetMakeType(factionDef, Conversion.noConversion);
                }

                settings.ConversionType = makeType;

                if (settings.ConversionType == Conversion.forceConversion) {
                    settings.ForcedTechLevel = (RimWorld.TechLevel) Settings.GetHandle($"{factionDef.defName}TechLevel", "techLevel_t".Translate(),
                    "techLevel_d".Translate(), EmpireTechLevel.industrial, null, "en_techLevel_").Value;
                }

                settings.ActualTechLevel = factionDef.techLevel;

                settings.IsRaiderFaction = Settings.GetHandle<bool>($"{factionDef.defName}IsRaider", "isRaider_t".Translate(),
                    "isRaider_d".Translate()).Value;

                settings.RelationshipHandle = Settings.GetHandle($"{factionDef.defName}Relation", "relation_t".Translate(),
                    "relation_d".Translate(), Relationship.basic, null, "en_relation_");

                // 관계
                // merc titles are only available for certain tech levels
                if (factionDef.techLevel == TechLevel.Industrial
                    || factionDef.techLevel == TechLevel.Spacer)
                {
                    settings.DisableMercTitles = Settings.GetHandle<bool>($"{factionDef.defName}Relation", "disableMercTitles_t".Translate(),
                        "disableMercTitles_d".Translate(), true);
                }

                settings.RelationshipType = settings.RelationshipHandle.Value;
                //ar_faction_relation.Add(Settings.GetHandle<en_relation>($"{f.defName}Relation", $"relation_t".Translate(), "relation_d".Translate(), en_relation.basic, null, "en_relation_").Value);


                // 왕족 의상
                settings.WantsApparelType = Settings.GetHandle($"{factionDef.defName}Apparel", "apparel_t".Translate(),
                    "apparel_d".Translate(), WantsApparel.basic, null, "en_apparel_").Value;


                // 거래제한과 권한
                settings.RequiresTradePermit = Settings.GetHandle<bool>($"{factionDef.defName}TradePermit", "trade_t".Translate(),
                    "trade_d".Translate()).Value;

                // 공물 징수원
                //ar_faction_collector.Add(Settings.GetHandle<bool>($"{f.defName}Collector", $"     - (not developed yet)use tribute collector", "A caravan trader selling royal favors.\n\n* Need to restart and new game", true).Value);

                factionConversionSettings.Add(settings);
            }



            // 디버그 모드
            debugModeSetting =
                Settings.GetHandle<bool>("debugMode", "debugMode_t".Translate(), "debugMode_d".Translate());
            debugMode = debugModeSetting.Value;
        }
    }
}
