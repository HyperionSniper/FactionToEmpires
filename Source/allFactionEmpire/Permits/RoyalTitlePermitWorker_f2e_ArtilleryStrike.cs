//// RimWorld.RoyalTitlePermitWorker_OrbitalStrike
//using System;
//using System.Collections.Generic;
//using RimWorld;
//using UnityEngine;
//using Verse;
//using Verse.AI;
//using Verse.Sound;

//namespace empireMaker.Permits
//{
//    public class RoyalTitlePermitWorker_ArtilleryStrike : RoyalTitlePermitWorker_OrbitalStrike
//    {
//        Faction faction;

//        public override void OrderForceTarget(LocalTargetInfo target)
//        {
//            CallBombardment(target.Thing.Map, target.Cell);
//        }

//        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
//        {
//            this.faction = faction;
//            return base.GetRoyalAidOptions(map, pawn, this.faction);
//        }

//        private void CallBombardment(Map map, IntVec3 targetCell)
//        {
//            VerbProperties mortarShootVerb = ThingDefOf.Turret_Mortar.Verbs[0];

//            Pawn pawn = PawnGenerator.GeneratePawn(def.royalAid.pawnKindDef, faction);
//            Building_Turret mortar = new Thing() as Building_Turret;
//            mortar.def = ThingDefOf.Turret_Mortar;
//            mortar.map = map;
//            mortar.Position = new IntVec3(
//                UnityEngine.Random.Range((int)0, map.Size.x - 1),
//                UnityEngine.Random.Range((int)0, 1) * (map.Size.y - 1),
//                0);
//            mortar.AllComps.Add(new CompMannable() {
//                parent = mortar,
//                props = new CompProperties() {
//                    compClass = typeof(CompProperties_Mannable)
//                }
//            });

//            Verb_Shoot verbShoot = new Verb_Shoot() {
//                verbProps = mortarShootVerb,
//                caster = mortar,
//            };

//            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera();

//            verbShoot.TryStartCastOn(new LocalTargetInfo(mortar), GetVerb.CurrentTarget);

//            caller.royalty.GetPermit(def, faction).Notify_Used();
//            if (!free) {
//                caller.royalty.TryRemoveFavor(faction, def.royalAid.favorCost);
//            }
//        }
//    }
//}