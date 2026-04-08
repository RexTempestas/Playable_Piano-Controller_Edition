using Microsoft.Xna.Framework.Graphics;
using MidiParser;
using StardewModdingAPI;
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
        private bool isStopped = false;
        private bool hasNotifiedOthers = false;

        public PlaybackUI(PlayablePiano mod, string fileName, int trackNumber)
        {
            this.mainMod = mod;
            this.sound = mainMod.sound;
            this.soundLow = mainMod.soundLow;
            this.soundHigh = mainMod.soundHigh;
            
            mainMod.Monitor.Log($"Reading file: {fileName}");
            MidiFile midiFile = new MidiFile(Path.Combine(mainMod.Helper.DirectoryPath, "assets", "songs", fileName));
            mainMod.Monitor.Log("Converting MIDI to Notes");
            List<Note> notes = new MidiConverter(midiFile, trackNumber, mainMod).convertToNotes();

            // Adjust octaves based on available sound banks
            foreach (Note note in notes)
            {
                note.octave = ((note.octave == Octave.low && !mainMod.lowerOctaves) || 
                               (note.octave == Octave.high && !mainMod.upperOctaves)) 
                               ? Octave.normal : note.octave;
            }
            
            int playTimeInSec = notes.Last<Note>().gameTick / 60;
            int playTimeInMin = playTimeInSec / 60;
            playTimeInSec = playTimeInSec % 60;
            
            mainMod.Monitor.Log($"Now Playing: {fileName}");
            mainMod.Monitor.Log($"Number of Notes: {notes.Count}", LogLevel.Debug);
            mainMod.Monitor.Log($"Estimated Playtime: {playTimeInMin}m {playTimeInSec}s", LogLevel.Debug);

            // Create track player
            songPlayer = new TrackPlayer(notes);

            // Stop any existing playback for this screen
            if (mainMod.ScreenPlayer.Value != null)
            {
                mainMod.Monitor.Log("Stopping existing playback before starting new one", LogLevel.Debug);
                mainMod.ScreenPlayer.Value.Stop();
            }

            // Create screen-specific player for split-screen support
            var screenPlayer = new ScreenTrackPlayer(
                mainMod,
                songPlayer,
                sound,
                soundLow,
                soundHigh,
                Game1.player.Tile,  // Save position now, not during update
                Game1.player.UniqueMultiplayerID
            );
            
            mainMod.ScreenPlayer.Value = screenPlayer;
            screenPlayer.Start();
            
            mainMod.LocalState.Value.IsPlaying = true;
            mainMod.LocalState.Value.OwnerId = Game1.player.UniqueMultiplayerID;
            mainMod.CurrentTrackPlayer.Value = songPlayer;
            
            // Lower music volume while piano is playing
            Game1.musicCategory.SetVolume(0f);

            // Send start playback message to other players in multiplayer
            startPlayback startPerformanceMessage = new startPlayback(
                Game1.player.Tile,
                notes,
                sound,
                Game1.player.UniqueMultiplayerID
            );

            List<long> playersAtLocation = Game1.currentLocation.farmers
                .Where(player => player.currentLocation == Game1.currentLocation && !player.IsLocalPlayer)
                .Select(player => player.UniqueMultiplayerID).ToList();

            if (playersAtLocation.Any())
            {
                mainMod.Helper.Multiplayer.SendMessage<startPlayback>(
                    startPerformanceMessage, 
                    "startPlayback", 
                    new string[] { mainMod.ModManifest.UniqueID }, 
                    playersAtLocation.ToArray()
                );
            }
        }

        private void StopPlayback(bool notifyOthers = true)
        {
            if (isStopped) return;
            isStopped = true;

            mainMod.ScreenPlayer.Value?.Stop();
            mainMod.ScreenPlayer.Value = null;

            mainMod.LocalState.Value.IsPlaying = false;
            mainMod.CurrentTrackPlayer.Value = null;
            
            mainMod.Monitor.Log("Playback stopped");
            
            Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);

            if (notifyOthers && !hasNotifiedOthers && Game1.IsMultiplayer)
            {
                hasNotifiedOthers = true;
                List<long> playersAtLocation = Game1.currentLocation.farmers
                    .Where(player => player.currentLocation == Game1.currentLocation)
                    .Select(player => player.UniqueMultiplayerID).ToList();
                    
                mainMod.Helper.Multiplayer.SendMessage(
                    new stopPlayback(Game1.player.UniqueMultiplayerID),
                    "stopPlayback",
                    new string[] { mainMod.ModManifest.UniqueID }
                );
            }
        }

        public override void draw(SpriteBatch b)
        {
            UIUtil.drawExitInstructions(b);
        }

        public override void handleButton(SButton button)
        {
            string input = button.ToString();

            if (input == "Escape" || input == "MouseRight" || button == SButton.ControllerB ||
                button == SButton.ControllerY || input == "Y" ||
                button == SButton.ControllerX || input == "X" ||
                button == SButton.ControllerA || input == "A")
            {
                mainMod.Helper.Input.Suppress(button);

                if (!isStopped)
                {
                    StopPlayback(true);
                }

                exitThisMenu();
                TrackSelection menu = new TrackSelection(mainMod);
                mainMod.setActiveMenu(menu);
            }
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();
            if (!isStopped)
            {
                StopPlayback(true);
            }
        }
    }
}