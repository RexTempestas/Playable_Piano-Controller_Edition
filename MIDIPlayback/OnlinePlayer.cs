using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI.Events;
using StardewValley;

namespace Playable_Piano
{
    /// <summary>
    /// Plays a MIDI Song, from another player, without UI
    /// </summary>
    public class OnlinePlayer
    {
        private string sound;
        private string soundLow;
        private string soundHigh;
        private TrackPlayer songPlayer;
        private PlayablePiano mainMod;
        private Vector2 performerTile;
        internal OnlinePlayer(PlayablePiano mod, Vector2 performerTilePos, List<Note> receivedNotation, string receivedSound)
        {
            mainMod = mod;
            performerTile = performerTilePos;
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
                        Game1.soundBank.GetCueDefinition(playedSoundCue).sounds.First<XactSoundBankSound>().pitch = (playedNote.pitch - 1200) / 1200f;
                    }
                    Game1.currentLocation.localSound(playedSoundCue, performerTile, playedNote.pitch);
                }
                else
                {
                    mainMod.Monitor.Log("playBack finished");
                    mainMod.Helper.Events.GameLoop.UpdateTicking -= playSong;
                    Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
                }
            }
        }

        public void stopSong()
        {
            mainMod.Monitor.Log("playBack stopped");
            mainMod.Helper.Events.GameLoop.UpdateTicking -= playSong;
            Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
        }
    }
}