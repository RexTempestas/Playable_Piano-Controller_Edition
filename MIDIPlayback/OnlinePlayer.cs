using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;

namespace Playable_Piano
{
    public class OnlinePlayer
    {
        private string sound;
        private string soundLow;
        private string soundHigh;
        private TrackPlayer songPlayer;
        private PlayablePiano mainMod;
        private Vector2 performerTile;
        private long playerId;
        private int ownerScreenId;

        internal OnlinePlayer(PlayablePiano mod, Vector2 performerTilePos, List<Note> receivedNotation, string receivedSound, long playerId)
        {
            mainMod = mod;
            performerTile = performerTilePos;
            this.playerId = playerId;
            this.ownerScreenId = Context.ScreenId;
            
            if (Game1.soundBank.Exists(receivedSound))
            {
                this.sound = receivedSound;
            }
            else
            {
                this.sound = "toyPiano";
            }
            soundLow = Game1.soundBank.Exists(sound + "Low") ? sound + "Low" : sound;
            soundHigh = Game1.soundBank.Exists(sound + "High") ? sound + "High" : sound;
            songPlayer = new TrackPlayer(receivedNotation);
            mainMod.Helper.Events.GameLoop.UpdateTicking += playSong;
            Game1.musicCategory.SetVolume(0f);
        }

        public void playSong(object? sender, UpdateTickingEventArgs e)
        {
            if (Context.ScreenId != ownerScreenId)
                return;
                
            if (mainMod.LocalState.Value.OwnerId != playerId)
            {
                return;
            }

            foreach (Note playedNote in songPlayer.GetNextNote())
            {
                if (playedNote.pitch >= 0)
                {
                    string playedSoundCue = sound;
                    switch (playedNote.octave)
                    {
                        case (Octave.normal):
                            break;
                        case (Octave.low):
                            playedSoundCue = soundLow;
                            break;
                        case (Octave.high):
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
                    mainMod.Monitor.Log($"playBack finished for player {playerId}");
                    mainMod.Helper.Events.GameLoop.UpdateTicking -= playSong;
                    Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
                }
            }
        }

        public void stopSong()
        {
            mainMod.Monitor.Log($"playBack stopped for player {playerId}");
            mainMod.Helper.Events.GameLoop.UpdateTicking -= playSong;
            Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
        }
        
        public bool IsFromPlayer(long id) => playerId == id;
    }
}