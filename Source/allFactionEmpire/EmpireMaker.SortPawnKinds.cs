using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace empireMaker {
    public partial class EmpireMaker {
        private static void SortPawnKinds(ConversionParams settings, FactionDef factionDef,
            out List<PawnKindDef> allPawns, out List<PawnKindDef> leaderPawns, out List<PawnKindDef> nonLeaderPawns)
        {
            allPawns = new List<PawnKindDef>();
            leaderPawns = new List<PawnKindDef>();
            nonLeaderPawns = new List<PawnKindDef>();

            foreach (var g in factionDef.pawnGroupMakers) {
                if (debugMode) {
                    Log.Message(g.options.Count.ToString());
                }

                foreach (var pawnGenOption in g.options) {
                    if (debugMode) {
                        Log.Message("b");
                    }

                    var p = pawnGenOption.kind;
                    if (allPawns.Contains(p)) {
                        continue;
                    }

                    if (debugMode) {
                        Log.Message($" - {factionDef.defName} : pawnkind : {p.defName}");
                    }

                    allPawns.Add(p);
                    if (p.factionLeader) {
                        leaderPawns.Add(p);
                    }
                    else {
                        nonLeaderPawns.Add(p);
                    }
                }
            }

            allPawns = allPawns.OrderBy(a => a.combatPower).ToList();
            if (nonLeaderPawns.Count <= 0) {
                nonLeaderPawns = allPawns;
            }

            nonLeaderPawns = nonLeaderPawns.OrderBy(a => a.combatPower).ToList();
            leaderPawns = leaderPawns.OrderBy(a => a.combatPower).ToList();
            Log.Message(
                $" - {factionDef.defName} : total pawn count : {allPawns.Count}, leader count : {leaderPawns.Count}");

            // 폰카인드 없음 스킵
            if (allPawns.Count == 0) {
                Log.Error(
                    $" - {factionDef.defName} : Can not read any pawnkind, Turn off the 'make empire' for this faction in the 'faction to empire mod' option.\nTurn off the 'make empire' for this faction in the 'faction to empire mod' option.\nAnd let the author know that this mod is not compatible.");
            }
        }

        private static void SortFighterPawnKinds(ConversionParams settings, FactionDef factionDef,
            List<PawnKindDef> allPawns,
            out List<PawnKindDef> fighterPawns)
        {
            fighterPawns = new List<PawnKindDef>();

            if (debugMode) {
                Log.Message("B1");
            }

            // fighter pawn 리스트
            foreach (var pawn in from pawn in allPawns
                                 where pawn.isFighter
                                 select pawn) {
                fighterPawns.Add(pawn);
            }

            fighterPawns = fighterPawns.OrderBy(a => a.combatPower).ToList();

            // set all pawns to fighter pawns if none are available
            if (fighterPawns.Count == 0) {
                fighterPawns = allPawns;
                Log.Message(
                    $" - {factionDef.defName} : total fighter pawn count : {fighterPawns.Count}, change to use allPawn");
            }
            else {
                Log.Message($" - {factionDef.defName} : total fighter pawn count : {fighterPawns.Count}");
            }

            if (debugMode) {
                Log.Message("B2");
            }
        }

        private static void SortPermitPawns(ConversionParams settings, FactionDef factionDef, 
            List<PawnKindDef> fighterPawns,
            out List<PawnKindDef> permitPawns)
        {
            permitPawns = new List<PawnKindDef>();

            // 호위병 소환을 위한 폰 등록

            if (fighterPawns.Count == 0) {
                permitPawns.Add(PawnKindDef.Named("Empire_Fighter_Trooper"));
                permitPawns.Add(PawnKindDef.Named("Empire_Fighter_Janissary"));
                permitPawns.Add(PawnKindDef.Named("Empire_Fighter_Cataphract"));
            }
            else if (fighterPawns.Count <= 3) {
                for (var i = 0; i < 3; i++) {
                    permitPawns.Add(fighterPawns[Mathf.Clamp(i, 0, fighterPawns.Count - 1)]);
                }
            }
            else {
                permitPawns.Add(fighterPawns[0]);
                permitPawns.Add(fighterPawns[Mathf.RoundToInt((fighterPawns.Count - 1) * 0.5f)]);
                permitPawns.Add(fighterPawns[fighterPawns.Count - 1]);
            }

            if (debugMode) {
                Log.Message("C");
            }
        }
    }
}
