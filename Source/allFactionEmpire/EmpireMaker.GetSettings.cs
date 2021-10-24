using HugsLib.Settings;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace empireMaker
{
    public partial class EmpireMaker : HugsLib.ModBase
    {

        public static bool psychicAll = true;
        public static bool delVanilla;
        public static float questAmount = 1f;
        public static bool debugMode;

        public class ConversionParams
        {
            public Conversion ConversionType;

            public TechLevel ForcedTechLevel;
            public TechLevel ActualTechLevel;

            public bool DisableMercTitles;
            public bool IsRaiderFaction;

            public Relationship RelationshipType;
            public WantsApparel WantsApparelType;
            public bool RequiresTradePermit;

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

        public override void SettingsChanged()
        {

            for (var i = 0; i < factionConversionSettings.Count; i++) {
                var settings = factionConversionSettings[i];

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

        public SettingHandle<Conversion> GetMakeType(FactionDef factionDef, Conversion defaultValue)
        {
            return Settings
                .GetHandle(
                    $"{factionDef.defName}ToEmpire", $"==== {factionDef.label.ToUpper()} ====", "make_d".Translate(),
                    defaultValue, null, "en_make_");
        }

        public void GetSettings()
        {
            // 초능력 공용화
            var psychicAllSetting = GetHandle("phychicAll", null, true);
            psychicAllSetting.ValueChanged += v => psychicAll = ((SettingHandle<bool>)v).Value;

            // 바닐라 제국 숨기기
            var delVanillaSetting = GetHandle("delVanilla", null, true);
            delVanillaSetting.NeverVisible = true;
            delVanillaSetting.ValueChanged += v => delVanilla = ((SettingHandle<bool>)v).Value;

            // 퀘스트 생성량
            var questAmountSetting = GetHandle("questAmount", null, 1);
            questAmountSetting.NeverVisible = true;
            questAmountSetting.ValueChanged += v => {
                var setting = ((SettingHandle<float>)v);

                if (setting.Value < 0.1f) {
                    setting.Value = 0.1f;
                }
                else if (setting.Value > 20f) {
                    setting.Value = 20f;
                }

                questAmount = questAmountSetting.Value;
            };

            // 디버그 모드
            var debugModeSetting = GetHandle("debugMode", null, true);
            debugModeSetting.ValueChanged += v => debugMode = ((SettingHandle<bool>)v).Value;

            for (var i = 0; i < eligibleFactions.Count; i++) {
                // 옵션 관리
                var factionDef = eligibleFactions[i];
                var isHardMod = EmpireHelpers.IsHardModToConvert(factionDef.modContentPack.PackageId);

                ConversionParams settings = new ConversionParams();

                // 제국화
                SettingHandle<Conversion> makeType;
                if (factionDef.permanentEnemy) {
                    makeType = GetMakeType(factionDef, Conversion.noConversion);
                }
                //else if (isHardMod) {
                //    makeType = GetMakeType(factionDef, Conversion.bugFix);

                //    if (makeType == Conversion.empire) {
                //        makeType.Value = Conversion.bugFix;
                //    }
                //}
                else {
                    makeType = GetMakeType(factionDef, Conversion.noConversion);
                }

                settings.ConversionType = makeType.Value;
                makeType.ValueChanged += s => settings.ConversionType = makeType.Value;

                var techLevel = GetHandle("techLevel", factionDef.defName, EmpireTechLevel.industrial);
                var disableMercTitles = GetHandle("disableMercTitles", factionDef.defName, false);

                var isRaider = GetHandle("isRaider", factionDef.defName, false);
                var trade = GetHandle("trade", factionDef.defName, true);
                var relation = GetHandle("relation", factionDef.defName, Relationship.basic);
                var apparel = GetHandle("apparel", factionDef.defName, WantsApparel.basic);

                // set visibility predicates
                SettingHandle.ShouldDisplay basePredicate = () => {
                    return settings.ConversionType != Conversion.noConversion;
                };

                SettingHandle.ShouldDisplay showTechLevelPredicate = () => {
                    return settings.ConversionType == Conversion.forceConversion;
                };

                SettingHandle.ShouldDisplay showMercTitlesPredicate = () => {
                    var validArchetypes = (from titles in s_RoyalTitleTagMap
                                          where titles.Value.Mercenary != null
                                          select titles.Key).ToList();

                    return settings.ConversionType != Conversion.noConversion
                    && validArchetypes.Contains(settings.Archetype);
                };

                techLevel.VisibilityPredicate = showTechLevelPredicate;
                disableMercTitles.VisibilityPredicate = showMercTitlesPredicate;

                isRaider.VisibilityPredicate = basePredicate;
                trade.VisibilityPredicate = basePredicate;
                relation.VisibilityPredicate = basePredicate;
                apparel.VisibilityPredicate = basePredicate;

                techLevel.ValueChanged += s => settings.ForcedTechLevel = (TechLevel)techLevel.Value;
                disableMercTitles.ValueChanged += s => settings.DisableMercTitles = disableMercTitles.Value;

                isRaider.ValueChanged += s => settings.IsRaiderFaction = isRaider.Value;
                // 거래제한과 권한
                trade.ValueChanged += s => settings.RequiresTradePermit = trade.Value;
                relation.ValueChanged += s => settings.RelationshipType = relation.Value;
                // 왕족 의상
                apparel.ValueChanged += s => settings.WantsApparelType = apparel.Value;

                // set values
                settings.ForcedTechLevel = (TechLevel)techLevel.Value;
                settings.ActualTechLevel = factionDef.techLevel;
                settings.DisableMercTitles = disableMercTitles.Value;

                settings.IsRaiderFaction = isRaider.Value;
                // 거래제한과 권한
                settings.RequiresTradePermit = trade.Value;
                settings.RelationshipType = relation.Value;
                // 왕족 의상
                settings.WantsApparelType = apparel.Value;

                factionConversionSettings.Add(settings);
            }
        }

        SettingHandle<T> GetHandle<T>(string title, string faction = null, T defaultValue = default)
        {

            return Settings.GetHandle<T>(
                $"{faction}_{title}",
                $"{title}_t".Translate(), $"{title}_d".Translate(),
                defaultValue,
                null,
                typeof(T).IsEnum ? $"en_{title}_" : null);
        }
    }
}
