using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MidiParser;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;

namespace Playable_Piano.UI
{
    internal class PlaybackUI : BaseUI
    {
        TrackPlayer songPlayer;
        private string sound;
        private string soundLow;
        private string soundHigh;
        protected override PlayablePiano mainMod { get; set; }

        public PlaybackUI(PlayablePiano mod, string fileName, int trackNumber)
        {
            this.mainMod = mod;
            this.sound = mainMod.sound;
            this.soundLow = mainMod.soundLow;
            this.soundHigh = mainMod.soundHigh;
            mainMod.Monitor.Log($"reading file: {fileName}");
            MidiFile midiFile = new MidiFile(Path.Combine(mainMod.Helper.DirectoryPath, "assets", "songs", fileName));
            mainMod.Monitor.Log("converting midi to Notes");
            List<Note> notes = new MidiConverter(midiFile, trackNumber, mainMod).convertToNotes();

            foreach (Note note in notes)
            {
                note.octave = ((note.octave == Octave.low && !mainMod.lowerOctaves) || (note.octave == Octave.high && !mainMod.upperOctaves)) ? Octave.normal : note.octave;
            }
            int playTimeInSec = notes.Last<Note>().gameTick / 60;
            int playTimeInMin = playTimeInSec / 60;
            playTimeInSec = playTimeInSec % 60;
            mainMod.Monitor.Log($"now Playing: {fileName}");
            mainMod.Monitor.Log($"Nr. of Notes: {notes.Count}");
            mainMod.Monitor.Log($"last Note will be playing on gameTick: {notes.Last<Note>().gameTick}");
            mainMod.Monitor.Log($"Estimated Playtime: {playTimeInMin} Minutes {playTimeInSec} seconds");
            
            songPlayer = new TrackPlayer(notes);
            mainMod.Helper.Events.GameLoop.UpdateTicking += playSong;
            Game1.musicCategory.SetVolume(0f);
            startPlayback startPerformanceMessage = new startPlayback(Game1.player.Tile, notes, sound);
            List<long> playersAtLocation = Game1.currentLocation.farmers.Where(player => player.currentLocation == Game1.currentLocation).Select(player => player.UniqueMultiplayerID).ToList();
            playersAtLocation.Remove(Game1.player.UniqueMultiplayerID);
            mainMod.Helper.Multiplayer.SendMessage<startPlayback>(startPerformanceMessage, "startPlayback", new string[] {mainMod.ModManifest.UniqueID}, playersAtLocation.ToArray());
        }


        private void playSong(object? sender, UpdateTickingEventArgs e)
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
                    // RPC controlled sounds have auto pitch, non controlled have to be set manually
                    if (!Game1.soundBank.GetCue(playedSoundCue).IsPitchBeingControlledByRPC)
                    {
                        Game1.soundBank.GetCueDefinition(playedSoundCue).sounds.First<XactSoundBankSound>().pitch = (playedNote.pitch - 1200) / 1200f;
                    }
                    Game1.currentLocation.localSound(playedSoundCue, Game1.player.Tile, playedNote.pitch);


                }
                else // Song finish marked by two invalid -200 Pitch notes
                {
                    mainMod.Monitor.Log("playBack finished");
                    mainMod.Helper.Events.GameLoop.UpdateTicking -= playSong;
                    Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
                    MainMenu menu = new MainMenu(mainMod);
                    mainMod.setActiveMenu(menu);
                }
            }
        }
        
        public override void draw(SpriteBatch b)
        {
            UIUtil.drawExitInstructions(b);
        }

        public override void handleButton(SButton button)
        {
            string input = button.ToString();
            if (input == "Escape" || input == "MouseRight" || button == SButton.ControllerB)
            {
                mainMod.Monitor.Log("playBack stopped");
                mainMod.Helper.Events.GameLoop.UpdateTicking -= playSong;
                List<long> playersAtLocation = Game1.currentLocation.farmers.Where(player => player.currentLocation == Game1.currentLocation).Select(player => player.UniqueMultiplayerID).ToList();
                mainMod.Helper.Multiplayer.SendMessage(new stopPlayback(), "stopPlayback", new string[] {mainMod.ModManifest.UniqueID});
                Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
                mainMod.Helper.Input.Suppress(button);
                exitThisMenu();
                TrackSelection menu = new TrackSelection(mainMod);
                mainMod.setActiveMenu(menu);
            }
        }
    }
}