#nullable enable
using System;
using System.IO;
using Quintessence.Game;
using Quintessence.Game.Clash;
using Quintessence.Game.Network;

namespace Quintessence.UI.Network
{
    // Manual encode/decode for NetworkAction's five variants, each a small,
    // fully-known closed set of int/enum fields (confirmed when NetworkAction
    // was designed) - avoids pulling in a general serializer as a second new
    // dependency for a payload this small and this fixed. Takes no
    // Steamworks types, so it's testable without Steam at all; a real
    // transport (SteamNetworkBridge) is the only caller.
    public static class NetworkActionWireFormat
    {
        private const byte DraftTag = 0;
        private const byte ForfeitTag = 1;
        private const byte DeclareTag = 2;
        private const byte WardTag = 3;
        private const byte DeclineWardTag = 4;
        private const byte MatchStartTag = 5;

        private const byte NoFavorTag = 0;
        private const byte AdjustTag = 1;
        private const byte RerollTag = 2;
        private const byte DefyTag = 3;

        private const byte ScorchTag = 0;
        private const byte RiptideTag = 1;
        private const byte GustTag = 2;
        private const byte PetrifyTag = 3;
        private const byte EclipseNullifyBandTag = 4;
        private const byte EclipseCancelTag = 5;

        public static byte[] Encode(NetworkAction action)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(action.ActingPlayer);
            writer.Write(action.SequenceNumber);

            switch (action)
            {
                case NetworkAction.Draft draft:
                    writer.Write(DraftTag);
                    WriteDraftChoice(writer, draft.Choice);
                    break;
                case NetworkAction.Forfeit:
                    writer.Write(ForfeitTag);
                    break;
                case NetworkAction.Declare declare:
                    writer.Write(DeclareTag);
                    writer.Write((byte)declare.Kind);
                    WriteInterventionParams(writer, declare.Params);
                    break;
                case NetworkAction.Ward:
                    writer.Write(WardTag);
                    break;
                case NetworkAction.DeclineWard:
                    writer.Write(DeclineWardTag);
                    break;
                case NetworkAction.MatchStart matchStart:
                    writer.Write(MatchStartTag);
                    writer.Write(matchStart.Seed);
                    writer.Write(matchStart.PlayerCount);
                    writer.Write(matchStart.IsClash);
                    writer.Write((byte)matchStart.Seats.Count);
                    foreach (var seat in matchStart.Seats)
                    {
                        writer.Write((byte)seat);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown NetworkAction variant.");
            }

            return stream.ToArray();
        }

        public static NetworkAction Decode(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            int actingPlayer = reader.ReadInt32();
            int sequenceNumber = reader.ReadInt32();
            byte tag = reader.ReadByte();

            NetworkAction action = tag switch
            {
                DraftTag => new NetworkAction.Draft(ReadDraftChoice(reader)),
                ForfeitTag => new NetworkAction.Forfeit(),
                DeclareTag => ReadDeclare(reader),
                WardTag => new NetworkAction.Ward(),
                DeclineWardTag => new NetworkAction.DeclineWard(),
                MatchStartTag => ReadMatchStart(reader),
                _ => throw new ArgumentOutOfRangeException(nameof(bytes), tag, "Unknown NetworkAction wire tag."),
            };

            return action with { ActingPlayer = actingPlayer, SequenceNumber = sequenceNumber };
        }

        private static void WriteDraftChoice(BinaryWriter writer, DraftChoice choice)
        {
            writer.Write((byte)choice.Source);
            writer.Write(choice.Index);
            writer.Write(choice.Row);
            writer.Write(choice.Col);

            switch (choice.Favor)
            {
                case null:
                    writer.Write(NoFavorTag);
                    break;
                case FavorAction.Adjust adjust:
                    writer.Write(AdjustTag);
                    writer.Write(adjust.Delta);
                    break;
                case FavorAction.Reroll:
                    writer.Write(RerollTag);
                    break;
                case FavorAction.Defy:
                    writer.Write(DefyTag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(choice), choice.Favor, "Unknown FavorAction variant.");
            }
        }

        private static DraftChoice ReadDraftChoice(BinaryReader reader)
        {
            var source = (DieSource)reader.ReadByte();
            int index = reader.ReadInt32();
            int row = reader.ReadInt32();
            int col = reader.ReadInt32();

            byte favorTag = reader.ReadByte();
            FavorAction? favor = favorTag switch
            {
                NoFavorTag => null,
                AdjustTag => new FavorAction.Adjust(reader.ReadInt32()),
                RerollTag => new FavorAction.Reroll(),
                DefyTag => new FavorAction.Defy(),
                _ => throw new ArgumentOutOfRangeException(nameof(reader), favorTag, "Unknown FavorAction wire tag."),
            };

            return new DraftChoice(source, index, row, col, favor);
        }

        private static void WriteInterventionParams(BinaryWriter writer, InterventionParams parameters)
        {
            switch (parameters)
            {
                case InterventionParams.Scorch scorch:
                    writer.Write(ScorchTag);
                    writer.Write(scorch.TargetPlayer);
                    writer.Write(scorch.Row);
                    writer.Write(scorch.Col);
                    writer.Write(scorch.Pips);
                    break;
                case InterventionParams.Riptide riptide:
                    writer.Write(RiptideTag);
                    writer.Write(riptide.FirmamentId);
                    writer.Write(riptide.Row);
                    writer.Write(riptide.Col);
                    break;
                case InterventionParams.Gust gust:
                    writer.Write(GustTag);
                    writer.Write(gust.PoolIndex);
                    writer.Write(gust.Row);
                    writer.Write(gust.Col);
                    break;
                case InterventionParams.Petrify petrify:
                    writer.Write(PetrifyTag);
                    writer.Write(petrify.TargetPlayer);
                    writer.Write(petrify.Row);
                    writer.Write(petrify.Col);
                    break;
                case InterventionParams.EclipseNullifyBand eclipseNullifyBand:
                    writer.Write(EclipseNullifyBandTag);
                    writer.Write(eclipseNullifyBand.TargetPlayer);
                    writer.Write(eclipseNullifyBand.Row);
                    writer.Write(eclipseNullifyBand.Col);
                    break;
                case InterventionParams.EclipseCancel:
                    writer.Write(EclipseCancelTag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameters), parameters, "Unknown InterventionParams variant.");
            }
        }

        private static NetworkAction.Declare ReadDeclare(BinaryReader reader)
        {
            var kind = (InterventionKind)reader.ReadByte();
            byte paramsTag = reader.ReadByte();

            InterventionParams parameters = paramsTag switch
            {
                ScorchTag => new InterventionParams.Scorch(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                RiptideTag => new InterventionParams.Riptide(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                GustTag => new InterventionParams.Gust(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                PetrifyTag => new InterventionParams.Petrify(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                EclipseNullifyBandTag => new InterventionParams.EclipseNullifyBand(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                EclipseCancelTag => new InterventionParams.EclipseCancel(),
                _ => throw new ArgumentOutOfRangeException(nameof(reader), paramsTag, "Unknown InterventionParams wire tag."),
            };

            return new NetworkAction.Declare(kind, parameters);
        }

        private static NetworkAction.MatchStart ReadMatchStart(BinaryReader reader)
        {
            long seed = reader.ReadInt64();
            int playerCount = reader.ReadInt32();
            bool isClash = reader.ReadBoolean();
            byte seatCount = reader.ReadByte();
            var seats = new SeatControl[seatCount];
            for (int i = 0; i < seatCount; i++)
            {
                seats[i] = (SeatControl)reader.ReadByte();
            }

            return new NetworkAction.MatchStart(seed, playerCount, seats, isClash);
        }
    }
}
