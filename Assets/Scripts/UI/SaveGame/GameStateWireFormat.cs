#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Quintessence.Engine;
using Quintessence.Game;
using Quintessence.Game.Clash;
using Quintessence.Game.Network;
using Quintessence.UI.Network;

namespace Quintessence.UI.SaveGame
{
    // Manual binary save format for a full local (non-network) match -
    // consistent with NetworkActionWireFormat's established pattern (no new
    // serializer dependency, same tag-byte style already proven for this
    // project's polymorphic record types). Captures everything needed to
    // resume byte-identically: the RNG's exact stream position
    // (Rng.ExportState, not just the original seed - resuming from the seed
    // would replay from the start, diverging from what actually happened),
    // the seat configuration, and the full GameState tree. Board's Cell[,]
    // grid is written cell-by-cell rather than recording "which of the 4
    // named layouts this was" - simpler, and doesn't create a hidden
    // dependency on BoardLayouts' exact list never changing.
    public static class GameStateWireFormat
    {
        private const byte ElementCellTag = 0;
        private const byte BandCellTag = 1;
        private const byte WildCellTag = 2;

        private const byte NoDieTag = 0;
        private const byte DieTag = 1;

        private const byte NoPhaseTag = 0;
        private const byte HasPhaseTag = 1;

        private const byte NoClashTag = 0;
        private const byte HasClashTag = 1;

        private const byte NoPendingTag = 0;
        private const byte HasPendingTag = 1;

        public static byte[] Encode(ulong rngState, IReadOnlyList<SeatControl> seatControl, GameState state)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(rngState);
            writer.Write((byte)seatControl.Count);
            foreach (var seat in seatControl)
            {
                writer.Write((byte)seat);
            }

            writer.Write(state.Round);
            writer.Write(state.StartPlayerIndex);
            writer.Write((byte)state.Players.Count);
            foreach (var player in state.Players)
            {
                WritePlayer(writer, player);
            }

            WriteBag(writer, state.Bag);

            writer.Write(state.Firmament.Count);
            foreach (var firmamentDie in state.Firmament)
            {
                writer.Write(firmamentDie.Id);
                WriteDie(writer, firmamentDie.Die);
            }

            writer.Write((byte)state.Objective);
            writer.Write(state.NextFirmamentId);
            writer.Write(state.IsGameOver);

            if (state.CurrentPhase is null)
            {
                writer.Write(NoPhaseTag);
            }
            else
            {
                writer.Write(HasPhaseTag);
                WritePhase(writer, state.CurrentPhase);
            }

            if (state.Clash is null)
            {
                writer.Write(NoClashTag);
            }
            else
            {
                writer.Write(HasClashTag);
                WriteClash(writer, state.Clash);
            }

            return stream.ToArray();
        }

        public static (ulong RngState, SeatControl[] SeatControl, GameState State) Decode(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            ulong rngState = reader.ReadUInt64();
            byte seatCount = reader.ReadByte();
            var seatControl = new SeatControl[seatCount];
            for (int i = 0; i < seatCount; i++)
            {
                seatControl[i] = (SeatControl)reader.ReadByte();
            }

            int round = reader.ReadInt32();
            int startPlayerIndex = reader.ReadInt32();
            byte playerCount = reader.ReadByte();
            var players = new List<PlayerState>(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                players.Add(ReadPlayer(reader));
            }

            Bag bag = ReadBag(reader);

            int firmamentCount = reader.ReadInt32();
            var firmament = new List<FirmamentDie>(firmamentCount);
            for (int i = 0; i < firmamentCount; i++)
            {
                int id = reader.ReadInt32();
                Die die = ReadDie(reader);
                firmament.Add(new FirmamentDie(id, die));
            }

            var objective = (PublicObjective)reader.ReadByte();
            int nextFirmamentId = reader.ReadInt32();
            bool isGameOver = reader.ReadBoolean();

            byte phaseTag = reader.ReadByte();
            RoundPhase? phase = phaseTag == HasPhaseTag ? ReadPhase(reader) : null;

            byte clashTag = reader.ReadByte();
            ClashState? clash = clashTag == HasClashTag ? ReadClash(reader) : null;

            var state = new GameState(
                Round: round,
                StartPlayerIndex: startPlayerIndex,
                Players: players,
                Bag: bag,
                Firmament: firmament,
                Objective: objective,
                CurrentPhase: phase,
                NextFirmamentId: nextFirmamentId,
                IsGameOver: isGameOver,
                Clash: clash);

            return (rngState, seatControl, state);
        }

        private static void WritePlayer(BinaryWriter writer, PlayerState player)
        {
            writer.Write((byte)player.PrivateElement);
            writer.Write(player.FavorRemaining);
            WriteBoard(writer, player.Board);
        }

        private static PlayerState ReadPlayer(BinaryReader reader)
        {
            var element = (Element)reader.ReadByte();
            int favor = reader.ReadInt32();
            Board board = ReadBoard(reader);
            return new PlayerState(board, favor, element);
        }

        private static void WriteBoard(BinaryWriter writer, Board board)
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    WriteCell(writer, board.CellAt(r, c));
                    WriteNullableDie(writer, board.DieAt(r, c));
                }
            }
        }

        private static Board ReadBoard(BinaryReader reader)
        {
            var cells = new Cell[Board.Rows, Board.Columns];
            var dice = new Die?[Board.Rows, Board.Columns];
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Columns; c++)
                {
                    cells[r, c] = ReadCell(reader);
                    dice[r, c] = ReadNullableDie(reader);
                }
            }

            return new Board(cells, dice);
        }

        private static void WriteCell(BinaryWriter writer, Cell cell)
        {
            switch (cell)
            {
                case Cell.ElementCell elementCell:
                    writer.Write(ElementCellTag);
                    writer.Write((byte)elementCell.Element);
                    break;
                case Cell.BandCell bandCell:
                    writer.Write(BandCellTag);
                    writer.Write((byte)bandCell.Band);
                    break;
                case Cell.WildCell:
                    writer.Write(WildCellTag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cell), cell, "Unknown Cell variant.");
            }
        }

        private static Cell ReadCell(BinaryReader reader)
        {
            byte tag = reader.ReadByte();
            return tag switch
            {
                ElementCellTag => new Cell.ElementCell((Element)reader.ReadByte()),
                BandCellTag => new Cell.BandCell((Band)reader.ReadByte()),
                WildCellTag => new Cell.WildCell(),
                _ => throw new ArgumentOutOfRangeException(nameof(reader), tag, "Unknown Cell wire tag."),
            };
        }

        private static void WriteDie(BinaryWriter writer, Die die)
        {
            writer.Write((byte)die.Element);
            writer.Write(die.Face);
        }

        private static Die ReadDie(BinaryReader reader) => new((Element)reader.ReadByte(), reader.ReadInt32());

        private static void WriteNullableDie(BinaryWriter writer, Die? die)
        {
            if (die is null)
            {
                writer.Write(NoDieTag);
            }
            else
            {
                writer.Write(DieTag);
                WriteDie(writer, die);
            }
        }

        private static Die? ReadNullableDie(BinaryReader reader)
        {
            byte tag = reader.ReadByte();
            return tag == DieTag ? ReadDie(reader) : null;
        }

        private static void WriteBag(BinaryWriter writer, Bag bag)
        {
            foreach (var element in Elements.All)
            {
                writer.Write(bag.Remaining.TryGetValue(element, out int count) ? count : 0);
            }
        }

        private static Bag ReadBag(BinaryReader reader)
        {
            var remaining = new Dictionary<Element, int>();
            foreach (var element in Elements.All)
            {
                remaining[element] = reader.ReadInt32();
            }

            return new Bag(remaining);
        }

        private static void WritePhase(BinaryWriter writer, RoundPhase phase)
        {
            writer.Write(phase.PickNumber);
            writer.Write(phase.PickOrder.Count);
            foreach (int p in phase.PickOrder)
            {
                writer.Write(p);
            }

            writer.Write(phase.PickOrderIndex);
            writer.Write(phase.Pool.Count);
            foreach (var die in phase.Pool)
            {
                WriteDie(writer, die);
            }
        }

        private static RoundPhase ReadPhase(BinaryReader reader)
        {
            int pickNumber = reader.ReadInt32();
            int pickOrderCount = reader.ReadInt32();
            var pickOrder = new List<int>(pickOrderCount);
            for (int i = 0; i < pickOrderCount; i++)
            {
                pickOrder.Add(reader.ReadInt32());
            }

            int pickOrderIndex = reader.ReadInt32();
            int poolCount = reader.ReadInt32();
            var pool = new List<Die>(poolCount);
            for (int i = 0; i < poolCount; i++)
            {
                pool.Add(ReadDie(reader));
            }

            return new RoundPhase(pickNumber, pickOrder, pickOrderIndex, pool);
        }

        private static void WriteClash(BinaryWriter writer, ClashState clash)
        {
            WriteClashConfig(writer, clash.Config);

            writer.Write(clash.Storm.Count);
            foreach (int s in clash.Storm)
            {
                writer.Write(s);
            }

            writer.Write(clash.InterventionsAvailable.Count);
            foreach (var kind in clash.InterventionsAvailable)
            {
                writer.Write((byte)kind);
            }

            writer.Write(clash.PetrifyTokens.Count);
            foreach (var token in clash.PetrifyTokens)
            {
                writer.Write(token.Player);
                writer.Write(token.Row);
                writer.Write(token.Col);
                writer.Write(token.ExpiresRound);
            }

            if (clash.Pending is null)
            {
                writer.Write(NoPendingTag);
            }
            else
            {
                writer.Write(HasPendingTag);
                writer.Write(clash.Pending.Actor);
                writer.Write(clash.Pending.Target);
                writer.Write((byte)clash.Pending.Kind);
                NetworkActionWireFormat.WriteInterventionParams(writer, clash.Pending.Params);
            }

            writer.Write(clash.InterventionLog.Count);
            foreach (var entry in clash.InterventionLog)
            {
                writer.Write(entry.Round);
                writer.Write(entry.Actor);
                writer.Write(entry.Target);
                writer.Write((byte)entry.Kind);
                writer.Write((byte)entry.Outcome);
            }

            writer.Write(clash.NullifiedBandCells.Count);
            foreach (var nullifiedCell in clash.NullifiedBandCells)
            {
                writer.Write(nullifiedCell.Player);
                writer.Write(nullifiedCell.Row);
                writer.Write(nullifiedCell.Col);
            }
        }

        private static ClashState ReadClash(BinaryReader reader)
        {
            ClashConfig config = ReadClashConfig(reader);

            int stormCount = reader.ReadInt32();
            var storm = new List<int>(stormCount);
            for (int i = 0; i < stormCount; i++)
            {
                storm.Add(reader.ReadInt32());
            }

            int availableCount = reader.ReadInt32();
            var available = new List<InterventionKind>(availableCount);
            for (int i = 0; i < availableCount; i++)
            {
                available.Add((InterventionKind)reader.ReadByte());
            }

            int tokenCount = reader.ReadInt32();
            var tokens = new List<PetrifyToken>(tokenCount);
            for (int i = 0; i < tokenCount; i++)
            {
                tokens.Add(new PetrifyToken(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
            }

            byte pendingTag = reader.ReadByte();
            PendingIntervention? pending = null;
            if (pendingTag == HasPendingTag)
            {
                int actor = reader.ReadInt32();
                int target = reader.ReadInt32();
                var kind = (InterventionKind)reader.ReadByte();
                InterventionParams parameters = NetworkActionWireFormat.ReadInterventionParams(reader);
                pending = new PendingIntervention(actor, target, kind, parameters);
            }

            int logCount = reader.ReadInt32();
            var log = new List<ClashLogEntry>(logCount);
            for (int i = 0; i < logCount; i++)
            {
                log.Add(new ClashLogEntry(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    (InterventionKind)reader.ReadByte(),
                    (ClashLogOutcome)reader.ReadByte()));
            }

            int nullifiedCount = reader.ReadInt32();
            var nullified = new List<NullifiedCell>(nullifiedCount);
            for (int i = 0; i < nullifiedCount; i++)
            {
                nullified.Add(new NullifiedCell(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
            }

            return new ClashState(config, storm, available, tokens, pending, log, nullified);
        }

        private static void WriteClashConfig(BinaryWriter writer, ClashConfig config)
        {
            writer.Write(config.StormPerAttune);
            writer.Write(config.StormCap);
            writer.Write(config.InterventionCost);
            writer.Write(config.BacklashFavor);
            writer.Write(config.WardCost);
            writer.Write(config.ScorchMaxPips);
            writer.Write(config.PetrifyDurationRounds);
            writer.Write(config.InterventionsPerMatch);
        }

        private static ClashConfig ReadClashConfig(BinaryReader reader) => new(
            StormPerAttune: reader.ReadInt32(),
            StormCap: reader.ReadInt32(),
            InterventionCost: reader.ReadInt32(),
            BacklashFavor: reader.ReadInt32(),
            WardCost: reader.ReadInt32(),
            ScorchMaxPips: reader.ReadInt32(),
            PetrifyDurationRounds: reader.ReadInt32(),
            InterventionsPerMatch: reader.ReadInt32());
    }
}
