using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using Microsoft.Xna.Framework;
using System;

namespace Playable_Piano
{
    /// <summary>
    /// Handles audio playback specific to ONE screen in split-screen mode
    /// Uses Game1.ticks to ensure exactly one tick per game frame
    /// </summary>
    internal class ScreenTrackPlayer
    {
        private readonly PlayablePiano mod;
        private TrackPlayer? trackPlayer;
        private readonly string sound;
        private readonly string soundLow;
        private readonly string soundHigh;
        private bool isStopped = false;
        private readonly Vector2 performerTile;
        private readonly long ownerId;
        
        private int lastGameTick = -1;
        private int tickCounter = 0;

        public bool IsPlaying => !isStopped && trackPlayer != null;

        public ScreenTrackPlayer(PlayablePiano mod, TrackPlayer trackPlayer, string sound, string soundLow, string soundHigh, Vector2 performerTile, long ownerId)
        {
            this.mod = mod;
            this.trackPlayer = trackPlayer;
            this.sound = sound;
            this.soundLow = soundLow;
            this.soundHigh = soundHigh;
            this.performerTile = performerTile;
            this.ownerId = ownerId;
        }

        public void Start()
        {
            mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        public void Stop()
        {
            if (isStopped) return;
            isStopped = true;
            mod.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            trackPlayer = null;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // Only process if current player is the owner
            if (Game1.player.UniqueMultiplayerID != ownerId)
                return;

            if (isStopped || trackPlayer == null)
                return;

            // Game1.ticks increments exactly ONCE per game frame globally
            int currentGameTick = Game1.ticks;
            
            if (currentGameTick == lastGameTick)
                return; // Already processed this frame
            
            lastGameTick = currentGameTick;
            
            // Process exactly one tick per game frame
            ProcessTick();
        }
        
        private void ProcessTick()
        {
            if (trackPlayer == null) return;
            
            tickCounter++;
            Note[] notes = trackPlayer.GetNextNote();
            
            foreach (Note playedNote in notes)
            {
                if (playedNote.pitch >= 0)
                {
                    string playedSoundCue = sound;
                    switch (playedNote.octave)
                    {
                        case Octave.low:
                            playedSoundCue = soundLow;
                            break;
                        case Octave.high:
                            playedSoundCue = soundHigh;
                            break;
                    }
                    
                    if (!Game1.soundBank.GetCue(playedSoundCue).IsPitchBeingControlledByRPC)
                    {
                        Game1.soundBank.GetCueDefinition(playedSoundCue).sounds.First().pitch = (playedNote.pitch - 1200) / 1200f;
                    }
                    
                    Game1.currentLocation.localSound(playedSoundCue, performerTile, playedNote.pitch);
                }
                else
                {
                    // End of song marker
                    Stop();
                    mod.OnScreenPlaybackFinished(ownerId);
                    return;
                }
            }
        }
    }
}