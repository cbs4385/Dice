using System.Collections.Generic;
using Quintessence.Engine;

namespace Quintessence.Game.Clash
{
    // Enumerates a representative (not exhaustively pip-varying) set of legal
    // intervention declarations for an actor - enough for AI/self-play/property
    // tests to only ever pick from provably-legal candidates, mirroring
    // LegalDrafts.EnumerateSimple's role for ordinary drafting.
    public static class ClashLegalMoves
    {
        public static IReadOnlyList<(InterventionKind Kind, InterventionParams Params)> EnumerateDeclarations(GameState state, int actor)
        {
            var results = new List<(InterventionKind, InterventionParams)>();
            var clash = state.Clash;
            if (clash is null || clash.Pending is not null || clash.Storm[actor] < clash.Config.InterventionCost)
            {
                return results;
            }

            foreach (var kind in clash.InterventionsAvailable)
            {
                switch (kind)
                {
                    case InterventionKind.Scorch:
                        AddScorchCandidates(state, actor, clash, results);
                        break;
                    case InterventionKind.Riptide:
                        AddRiptideCandidates(state, actor, results);
                        break;
                    case InterventionKind.Gust:
                        AddGustCandidates(state, actor, results);
                        break;
                    case InterventionKind.Petrify:
                        AddPetrifyCandidates(state, actor, results);
                        break;
                    case InterventionKind.Eclipse:
                        AddEclipseCandidates(state, actor, results);
                        break;
                }
            }

            return results;
        }

        private static void AddScorchCandidates(GameState state, int actor, ClashState clash, List<(InterventionKind, InterventionParams)> results)
        {
            for (int p = 0; p < state.Players.Count; p++)
            {
                if (p == actor)
                {
                    continue;
                }

                var board = state.Players[p].Board;
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        if (board.DieAt(r, c) is Die die)
                        {
                            int maxPips = System.Math.Min(clash.Config.ScorchMaxPips, die.Face - 1);
                            if (maxPips >= 1)
                            {
                                results.Add((InterventionKind.Scorch, new InterventionParams.Scorch(p, r, c, maxPips)));
                            }
                        }
                    }
                }
            }
        }

        private static void AddRiptideCandidates(GameState state, int actor, List<(InterventionKind, InterventionParams)> results)
        {
            var actorBoard = state.Players[actor].Board;
            foreach (var entry in state.Firmament)
            {
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        if (Legality.IsLegalPlacement(actorBoard, new Placement(r, c, entry.Die)).IsLegal)
                        {
                            results.Add((InterventionKind.Riptide, new InterventionParams.Riptide(entry.Id, r, c)));
                        }
                    }
                }
            }
        }

        private static void AddGustCandidates(GameState state, int actor, List<(InterventionKind, InterventionParams)> results)
        {
            if (state.CurrentPhase is not RoundPhase phase)
            {
                return;
            }

            var actorBoard = state.Players[actor].Board;
            for (int i = 0; i < phase.Pool.Count; i++)
            {
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        if (Legality.IsLegalPlacement(actorBoard, new Placement(r, c, phase.Pool[i])).IsLegal)
                        {
                            results.Add((InterventionKind.Gust, new InterventionParams.Gust(i, r, c)));
                        }
                    }
                }
            }
        }

        private static void AddPetrifyCandidates(GameState state, int actor, List<(InterventionKind, InterventionParams)> results)
        {
            for (int p = 0; p < state.Players.Count; p++)
            {
                if (p == actor)
                {
                    continue;
                }

                var board = state.Players[p].Board;
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        if (board.DieAt(r, c) is null)
                        {
                            results.Add((InterventionKind.Petrify, new InterventionParams.Petrify(p, r, c)));
                        }
                    }
                }
            }
        }

        private static void AddEclipseCandidates(GameState state, int actor, List<(InterventionKind, InterventionParams)> results)
        {
            for (int p = 0; p < state.Players.Count; p++)
            {
                if (p == actor)
                {
                    continue;
                }

                var board = state.Players[p].Board;
                for (int r = 0; r < Board.Rows; r++)
                {
                    for (int c = 0; c < Board.Columns; c++)
                    {
                        if (board.CellAt(r, c) is Cell.BandCell)
                        {
                            results.Add((InterventionKind.Eclipse, new InterventionParams.EclipseNullifyBand(p, r, c)));
                        }
                    }
                }
            }
        }
    }
}
