using System;
using UnityEngine;
using Steamworks;

namespace Quintessence.UI.Network
{
    // The only place SteamClient.Init/RunCallbacks/Shutdown are ever called.
    // Never added to any existing scene's active hierarchy today, and never
    // constructed by GameSessionController.Awake() - it only exists once a
    // player actually chooses to host/join a network match (a future
    // slice's UI hook wires this in). TryInit wraps SteamClient.Init in
    // try/catch per the Facepunch wiki's own documented failure mode (Steam
    // not running, DLL missing, no permission) and returns a bool rather
    // than letting the exception propagate - this keeps a Steamworks
    // failure from ever being able to crash anything that exists today.
    public sealed class SteamService : MonoBehaviour
    {
        // Steam's own standard placeholder app ID ("Spacewar") for pre-launch
        // development - this game has no real Steam app ID yet. Swapping
        // this for a real one is a one-line, human-gated change later, not
        // an architecture change.
        public const uint PlaceholderAppId = 480;

        public bool IsInitialized { get; private set; }

        public bool TryInit(uint appId)
        {
            if (IsInitialized)
            {
                return true;
            }

            try
            {
                SteamClient.Init(appId);
                IsInitialized = true;
                return true;
            }
            catch (Exception exception)
            {
                // Expected on a machine/CI runner with no Steam client
                // running - a warning, not an error, since this is the
                // routine, anticipated failure mode this method exists to
                // absorb, not a bug.
                Debug.LogWarning($"SteamService.TryInit({appId}) failed: {exception.Message}");
                IsInitialized = false;
                return false;
            }
        }

        private void Update()
        {
            if (IsInitialized)
            {
                SteamClient.RunCallbacks();
            }
        }

        private void OnDestroy()
        {
            if (IsInitialized)
            {
                SteamClient.Shutdown();
                IsInitialized = false;
            }
        }
    }
}
