using System;
using System.Collections.Generic;
using System.Linq;
using Quintessence.Engine;

namespace Quintessence.Game.Clash
{
    public static class ClashReducer
    {
        // Called from GameReducer.ApplyDraft only when state.Clash is not null and
        // the placement attuned a band cell - a small, explicitly-gated touch-point
        // on the core reducer, never a branch inside shared placement logic.
        public static ClashState ChargeStormOnAttune(ClashState clash, int player)
        {
            var storm = new List<int>(clash.Storm);
            storm[player] = Math.Min(storm[player] + clash.Config.StormPerAttune, clash.Config.StormCap);
            return clash with { Storm = storm };
        }

        public static bool IsCellPetrified(ClashState? clash, int player, int row, int col, int currentRound) =>
            clash is not null && clash.PetrifyTokens.Any(t => t.Player == player && t.Row == row && t.Col == col && t.ExpiresRound > currentRound);

        public static GameState DeclareIntervention(GameState state, int actor, InterventionKind kind, InterventionParams parameters, IRng rng)
        {
            var clash = RequireClash(state);

            // Eclipse can be declared as a reaction that cancels a pending
            // intervention, instead of a fresh declaration against a new target.
            if (kind == InterventionKind.Eclipse && parameters is InterventionParams.EclipseCancel)
            {
                return ResolveEclipseCancel(state, clash, actor);
            }

            if (clash.Pending is not null)
            {
                throw new InvalidOperationException("Another intervention is pending; resolve it (Ward/DeclineWard/Eclipse-cancel) first.");
            }

            if (!clash.InterventionsAvailable.Contains(kind))
            {
                throw new InvalidOperationException($"{kind} is not in this match's dealt intervention set.");
            }

            if (clash.Storm[actor] < clash.Config.InterventionCost)
            {
                throw new InvalidOperationException("Not enough Storm to declare an intervention.");
            }

            int target = ResolveTargetAndValidate(state, clash, actor, kind, parameters);

            var storm = new List<int>(clash.Storm);
            storm[actor] -= clash.Config.InterventionCost;

            var pending = new PendingIntervention(actor, target, kind, parameters);
            var log = Appended(clash.InterventionLog, new ClashLogEntry(state.Round, actor, target, kind, ClashLogOutcome.Declared));
            var updatedClash = clash with { Storm = storm, Pending = pending, InterventionLog = log };

            // Backlash: T immediately gains backlashFavor favor, paid whether or not
            // the intervention is ultimately Warded (docs/clash.md SS2.3).
            var updatedPlayers = state.Players.ToList();
            updatedPlayers[target] = updatedPlayers[target] with
            {
                FavorRemaining = updatedPlayers[target].FavorRemaining + clash.Config.BacklashFavor,
            };

            return state with { Players = updatedPlayers, Clash = updatedClash };
        }

        public static GameState Ward(GameState state, int target)
        {
            var clash = RequireClash(state);
            var pending = RequirePendingFor(clash, target);

            var playerState = state.Players[target];
            if (playerState.FavorRemaining < clash.Config.WardCost)
            {
                throw new InvalidOperationException("Not enough favor to Ward.");
            }

            var updatedPlayers = state.Players.ToList();
            updatedPlayers[target] = playerState with { FavorRemaining = playerState.FavorRemaining - clash.Config.WardCost };

            var log = Appended(clash.InterventionLog, new ClashLogEntry(state.Round, pending.Actor, target, pending.Kind, ClashLogOutcome.Warded));
            var updatedClash = clash with { Pending = null, InterventionLog = log };

            return state with { Players = updatedPlayers, Clash = updatedClash };
        }

        public static GameState DeclineWard(GameState state, int target)
        {
            var clash = RequireClash(state);
            var pending = RequirePendingFor(clash, target);

            var resolved = ApplyEffect(state, pending);
            var resolvedClash = resolved.Clash!;
            var log = Appended(resolvedClash.InterventionLog, new ClashLogEntry(state.Round, pending.Actor, target, pending.Kind, ClashLogOutcome.Applied));

            return resolved with { Clash = resolvedClash with { Pending = null, InterventionLog = log } };
        }

        public static GameState Shatter(GameState state, int owner, int row, int col)
        {
            var clash = RequireClash(state);
            var playerState = state.Players[owner];
            if (playerState.FavorRemaining < 1)
            {
                throw new InvalidOperationException("Not enough favor to Shatter.");
            }

            var token = clash.PetrifyTokens.FirstOrDefault(t => t.Player == owner && t.Row == row && t.Col == col);
            if (token is null)
            {
                throw new InvalidOperationException("No Petrify token at that cell.");
            }

            var updatedPlayers = state.Players.ToList();
            updatedPlayers[owner] = playerState with { FavorRemaining = playerState.FavorRemaining - 1 };

            var tokens = clash.PetrifyTokens.Where(t => t != token).ToList();

            return state with { Players = updatedPlayers, Clash = clash with { PetrifyTokens = tokens } };
        }

        private static GameState ResolveEclipseCancel(GameState state, ClashState clash, int actor)
        {
            if (clash.Pending is not PendingIntervention pending || pending.Target != actor)
            {
                throw new InvalidOperationException("Eclipse can only cancel a pending intervention targeting you.");
            }

            if (!clash.InterventionsAvailable.Contains(InterventionKind.Eclipse))
            {
                throw new InvalidOperationException("Eclipse is not in this match's dealt intervention set.");
            }

            if (clash.Storm[actor] < clash.Config.InterventionCost)
            {
                throw new InvalidOperationException("Not enough Storm to Eclipse-cancel.");
            }

            var storm = new List<int>(clash.Storm);
            storm[actor] -= clash.Config.InterventionCost;

            var log = Appended(clash.InterventionLog, new ClashLogEntry(state.Round, actor, pending.Actor, InterventionKind.Eclipse, ClashLogOutcome.Cancelled));
            var updatedClash = clash with { Storm = storm, Pending = null, InterventionLog = log };

            return state with { Clash = updatedClash };
        }

        // "Legal-by-construction" (docs/clash.md SS2.3): validated once, at declare
        // time. GameReducer additionally refuses any other action while a Pending
        // intervention exists, so no board state can change out from under it
        // before DeclineWard resolves the effect.
        // For Gust/Riptide (queue-jump / shared-resource-grab effects with no
        // opponent board to name), the target T for Backlash/Ward purposes is
        // whoever currently holds draft priority - their expected turn is what
        // gets disrupted. A reasonable, documented interpretation of an
        // underspecified interaction (see docs/progress.md), not a rules
        // contradiction.
        private static int ResolveTargetAndValidate(GameState state, ClashState clash, int actor, InterventionKind kind, InterventionParams parameters)
        {
            switch (kind)
            {
                case InterventionKind.Scorch when parameters is InterventionParams.Scorch scorch:
                    if (scorch.Pips < 1 || scorch.Pips > clash.Config.ScorchMaxPips)
                    {
                        throw new InvalidOperationException($"Scorch pips must be between 1 and {clash.Config.ScorchMaxPips}.");
                    }

                    if (state.Players[scorch.TargetPlayer].Board.DieAt(scorch.Row, scorch.Col) is null)
                    {
                        throw new InvalidOperationException("Scorch target cell is empty.");
                    }

                    return scorch.TargetPlayer;

                case InterventionKind.Riptide when parameters is InterventionParams.Riptide riptide:
                    {
                        var entry = state.Firmament.FirstOrDefault(f => f.Id == riptide.FirmamentId);
                        if (entry is null)
                        {
                            throw new InvalidOperationException("Riptide is illegal to declare: the Firmament is empty (or that die is not in it).");
                        }

                        var actorBoard = state.Players[actor].Board;
                        var legality = Legality.IsLegalPlacement(actorBoard, new Placement(riptide.Row, riptide.Col, entry.Die));
                        if (!legality.IsLegal)
                        {
                            throw new InvalidOperationException($"Illegal Riptide placement: {legality.Reason}");
                        }

                        return GameReducer.CurrentPlayer(state);
                    }

                case InterventionKind.Gust when parameters is InterventionParams.Gust gust:
                    {
                        var phase = state.CurrentPhase ?? throw new InvalidOperationException("Gust requires an active draft phase.");
                        if (gust.PoolIndex < 0 || gust.PoolIndex >= phase.Pool.Count)
                        {
                            throw new InvalidOperationException("Gust pool index out of range.");
                        }

                        var actorBoard = state.Players[actor].Board;
                        var legality = Legality.IsLegalPlacement(actorBoard, new Placement(gust.Row, gust.Col, phase.Pool[gust.PoolIndex]));
                        if (!legality.IsLegal)
                        {
                            throw new InvalidOperationException($"Illegal Gust placement: {legality.Reason}");
                        }

                        return GameReducer.CurrentPlayer(state);
                    }

                case InterventionKind.Petrify when parameters is InterventionParams.Petrify petrify:
                    if (state.Players[petrify.TargetPlayer].Board.DieAt(petrify.Row, petrify.Col) is not null)
                    {
                        throw new InvalidOperationException("Petrify can only target an empty cell.");
                    }

                    return petrify.TargetPlayer;

                case InterventionKind.Eclipse when parameters is InterventionParams.EclipseNullifyBand eclipse:
                    if (state.Players[eclipse.TargetPlayer].Board.CellAt(eclipse.Row, eclipse.Col) is not Cell.BandCell)
                    {
                        throw new InvalidOperationException("Eclipse can only nullify a band cell.");
                    }

                    return eclipse.TargetPlayer;

                default:
                    throw new InvalidOperationException($"Params type does not match intervention kind {kind}.");
            }
        }

        private static GameState ApplyEffect(GameState state, PendingIntervention pending) => pending.Kind switch
        {
            InterventionKind.Scorch => ApplyScorch(state, (InterventionParams.Scorch)pending.Params),
            InterventionKind.Riptide => ApplyRiptide(state, pending.Actor, (InterventionParams.Riptide)pending.Params),
            InterventionKind.Gust => ApplyGust(state, pending.Actor, (InterventionParams.Gust)pending.Params),
            InterventionKind.Petrify => ApplyPetrify(state, (InterventionParams.Petrify)pending.Params),
            InterventionKind.Eclipse => ApplyEclipseNullify(state, (InterventionParams.EclipseNullifyBand)pending.Params),
            _ => throw new InvalidOperationException($"Unknown intervention kind {pending.Kind}."),
        };

        private static GameState ApplyScorch(GameState state, InterventionParams.Scorch scorch)
        {
            var targetPlayerState = state.Players[scorch.TargetPlayer];
            var die = targetPlayerState.Board.DieAt(scorch.Row, scorch.Col)
                ?? throw new InvalidOperationException("Scorch target cell became empty before resolution.");

            int reducedFace = Math.Max(1, die.Face - scorch.Pips);
            var newBoard = targetPlayerState.Board.WithPlacement(new Placement(scorch.Row, scorch.Col, die with { Face = reducedFace }));

            var updatedPlayers = state.Players.ToList();
            updatedPlayers[scorch.TargetPlayer] = targetPlayerState with { Board = newBoard };

            return state with { Players = updatedPlayers };
        }

        private static GameState ApplyRiptide(GameState state, int actor, InterventionParams.Riptide riptide)
        {
            var entry = state.Firmament.FirstOrDefault(f => f.Id == riptide.FirmamentId)
                ?? throw new InvalidOperationException("Riptide target Firmament die is no longer available.");

            var actorState = state.Players[actor];
            var placement = new Placement(riptide.Row, riptide.Col, entry.Die);
            var legality = Legality.IsLegalPlacement(actorState.Board, placement);
            if (!legality.IsLegal)
            {
                throw new InvalidOperationException($"Illegal Riptide placement at resolution: {legality.Reason}");
            }

            var updatedPlayers = state.Players.ToList();
            updatedPlayers[actor] = actorState with { Board = actorState.Board.WithPlacement(placement) };

            var newFirmament = state.Firmament.Where(f => f.Id != riptide.FirmamentId).ToList();

            return state with { Players = updatedPlayers, Firmament = newFirmament };
        }

        private static GameState ApplyGust(GameState state, int actor, InterventionParams.Gust gust)
        {
            var phase = state.CurrentPhase ?? throw new InvalidOperationException("Gust requires an active draft phase.");
            var die = phase.Pool[gust.PoolIndex];
            var actorState = state.Players[actor];
            var placement = new Placement(gust.Row, gust.Col, die);
            var legality = Legality.IsLegalPlacement(actorState.Board, placement);
            if (!legality.IsLegal)
            {
                throw new InvalidOperationException($"Illegal Gust placement at resolution: {legality.Reason}");
            }

            var updatedPlayers = state.Players.ToList();
            updatedPlayers[actor] = actorState with { Board = actorState.Board.WithPlacement(placement) };

            var remainingPool = phase.Pool.ToList();
            remainingPool.RemoveAt(gust.PoolIndex);

            // Total picks in the round are unchanged: PickOrderIndex is untouched,
            // so whoever's turn it is still gets their own pick, just from a
            // smaller pool.
            return state with { Players = updatedPlayers, CurrentPhase = phase with { Pool = remainingPool } };
        }

        private static GameState ApplyPetrify(GameState state, InterventionParams.Petrify petrify)
        {
            var clash = state.Clash!;
            if (state.Players[petrify.TargetPlayer].Board.DieAt(petrify.Row, petrify.Col) is not null)
            {
                throw new InvalidOperationException("Petrify target cell is no longer empty at resolution.");
            }

            var token = new PetrifyToken(petrify.TargetPlayer, petrify.Row, petrify.Col, state.Round + clash.Config.PetrifyDurationRounds);
            var tokens = clash.PetrifyTokens.ToList();
            tokens.Add(token);

            return state with { Clash = clash with { PetrifyTokens = tokens } };
        }

        private static GameState ApplyEclipseNullify(GameState state, InterventionParams.EclipseNullifyBand eclipse)
        {
            var clash = state.Clash!;
            var nullified = clash.NullifiedBandCells.ToList();
            nullified.Add(new NullifiedCell(eclipse.TargetPlayer, eclipse.Row, eclipse.Col));

            return state with { Clash = clash with { NullifiedBandCells = nullified } };
        }

        private static ClashState RequireClash(GameState state) =>
            state.Clash ?? throw new InvalidOperationException("This game is not in Clash mode.");

        private static PendingIntervention RequirePendingFor(ClashState clash, int target)
        {
            if (clash.Pending is not PendingIntervention pending || pending.Target != target)
            {
                throw new InvalidOperationException("No pending intervention targets that player.");
            }

            return pending;
        }

        private static IReadOnlyList<ClashLogEntry> Appended(IReadOnlyList<ClashLogEntry> log, ClashLogEntry entry)
        {
            var list = log.ToList();
            list.Add(entry);
            return list;
        }
    }
}
