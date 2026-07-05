#nullable enable
using System.IO;
using UnityEngine;

namespace Quintessence.UI.SaveGame
{
    // Single-slot file I/O for a local match save - the simplest honest
    // scope for a vertical slice (M5 DoD: "save/resume works"), not a
    // save-slot system. Pure file I/O; the actual GameState<->byte[]
    // encoding lives in GameStateWireFormat.
    public static class SaveGameService
    {
        private const string FileName = "savegame.bin";

        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(Path);

        public static void Save(byte[] bytes) => File.WriteAllBytes(Path, bytes);

        public static byte[]? Load() => Exists() ? File.ReadAllBytes(Path) : null;

        public static void Delete()
        {
            if (Exists())
            {
                File.Delete(Path);
            }
        }
    }
}
