using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using Playable_Piano.UI;
using Microsoft.Xna.Framework.Audio;
using StardewValley.Triggers;
using StardewValley.Delegates;
using StardewModdingAPI.Utilities;
using System.Linq;

namespace Playable_Piano
{
    internal sealed class PlayablePiano : Mod
    {
        private BaseUI? activeMenu;
        private ModConfig config = null!;
        // Local split-screen state (separate for each player on same PC)
        internal readonly PerScreen<LocalPianoState> LocalState = new PerScreen<LocalPianoState>(() => new LocalPianoState());
        internal readonly PerScreen<TrackPlayer?> CurrentTrackPlayer = new PerScreen<TrackPlayer?>(() => null);
        internal readonly PerScreen<ScreenTrackPlayer?> ScreenPlayer = new PerScreen<ScreenTrackPlayer?>();

        // Online multiplayer state (players on different PCs)
        private Dictionary<long, OnlinePlayer> remotePlayers = new Dictionary<long, OnlinePlayer>();

        public class LocalPianoState
        {
            public long? OwnerId { get; set; } = null;
            public string? OccupiedPosition { get; set; } = null;
            public bool IsPlaying { get; set; } = false;  
        }

        public string sound = "Mushroomy.PlayablePiano_Piano";
        public string soundLow = "Mushroomy.PlayablePiano_PianoLow";
        public string soundHigh = "Mushroomy.PlayablePiano_PianoHigh";
        public bool lowerOctaves = false;
        public bool upperOctaves = false;


        #region public Methods
        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            TriggerActionManager.RegisterAction("Mushroomy.PlayablePiano_AddSound", this.addInstrument);
            TriggerActionManager.RegisterTrigger("Mushroomy.PlayablePiano_SaveLoaded");
            if (config == null)
            {
                this.Monitor.Log("Could not load Instrument Data, check whether the Mods config.json exists and the file permissions. Using default config", LogLevel.Warn);
                config = new ModConfig();
                config.InstrumentData = new Dictionary<string, string> { { "Dark Piano", "Mushroomy.PlayablePiano_Piano" }, { "UprightPiano", "Mushroomy.PlayablePiano_Piano" } };
                helper.WriteConfig<ModConfig>(config);
            }
            loadDefaultSounds();
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.CPIntegration;
            helper.Events.Multiplayer.ModMessageReceived += this.receiveMessage;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Multiplayer.PeerDisconnected += this.OnPeerDisconnected;
            helper.ConsoleCommands.Add("reset_instrument_sounds", "resets all instruments to their initial base sound", this.reset_instrument_sounds);
        }

        public override object? GetApi()
        {
            return new PianoApi(this);
        }

        public void setActiveMenu(BaseUI? newMenu)
        {
            if (this.activeMenu != null && this.activeMenu != newMenu)
            {
                this.activeMenu.exitThisMenu();
            }

            this.activeMenu = newMenu;

            if (newMenu is not null)
            {
                Game1.activeClickableMenu = newMenu;

                // Lower music volume when entering playback UI
                if (newMenu is PlaybackUI)
                {
                    Game1.musicCategory.SetVolume(0f);
                }
            }
            else
            {
                // Menu closed - restore music volume
                Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);

                LocalState.Value.OwnerId = null;
                LocalState.Value.OccupiedPosition = null;
                LocalState.Value.IsPlaying = false;
            }
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (activeMenu != null && e.NewMenu == null)
            {
                activeMenu.handleButton(SButton.Escape);
                activeMenu = null;

                // Restore music volume when menu is closed externally
                Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);

                LocalState.Value.OwnerId = null;
                LocalState.Value.OccupiedPosition = null;
                LocalState.Value.IsPlaying = false;
            }
        }

        public void CPIntegration(object? sender, SaveLoadedEventArgs e)
        {
            checkConfigSounds();
            Monitor.Log("adding CP Instruments");
            TriggerActionManager.Raise("Mushroomy.PlayablePiano_SaveLoaded");
        }

        public bool addInstrument(string[] args, TriggerActionContext context, out string? error)
        {
            if (args.Length < 3)
            {
                error = "Invalid arguments: expected at least 3 arguments (trigger, instrumentName, soundName)";
                return false;
            }

            string instrumentName = args[1];
            string soundName = args[2];
            if (Game1.soundBank.Exists(soundName))
            {
                if (config.InstrumentData.TryAdd(instrumentName, soundName))
                {
                    Monitor.Log($"Added {instrumentName} with sound {soundName}");
                    Helper.WriteConfig<ModConfig>(config);
                }
                error = null;
                return true;
            }
            else
            {
                error = $"sound {soundName} for {instrumentName} couldn't be added, since it doesn't exist. Please contact the Mod's author.";
                return false;
            }
        }

        public void receiveMessage(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == this.ModManifest.UniqueID)
            {
                if (e.Type == "startPlayback")
                {
                    startPlayback message = e.ReadAs<startPlayback>();
                    long playerId = message.playerId;

                    if (remotePlayers.ContainsKey(playerId))
                    {
                        remotePlayers[playerId].stopSong();
                        remotePlayers.Remove(playerId);
                    }

                    if (LocalState.Value.OwnerId != playerId)
                    {
                        LocalState.Value.OwnerId = playerId;
                        LocalState.Value.OccupiedPosition = $"{message.performerTilePos}";
                    }

                    OnlinePlayer onlinePlayer = new OnlinePlayer(this, message.performerTilePos, message.notation, message.sound, playerId);
                    remotePlayers.Add(playerId, onlinePlayer);
                }
                else if (e.Type == "stopPlayback")
                {
                    stopPlayback message = e.ReadAs<stopPlayback>();
                    if (remotePlayers.ContainsKey(message.playerId))
                    {
                        remotePlayers[message.playerId].stopSong();
                        remotePlayers.Remove(message.playerId);

                        if (LocalState.Value.OwnerId == message.playerId)
                        {
                            LocalState.Value.OwnerId = null;
                            LocalState.Value.OccupiedPosition = null;
                        }
                    }
                }
                else if (e.Type == "playNote")
                {
                    playNote message = e.ReadAs<playNote>();
                    string receivedSound = Game1.soundBank.Exists(message.sound) ? message.sound : "toyPiano";
                    Game1.soundBank.GetCueDefinition(receivedSound).sounds.First().pitch = (message.pitch - 1200) / 1200f;
                    Game1.currentLocation.localSound(receivedSound, message.performerTile, message.pitch);
                }
            }
        }

        public void OnScreenPlaybackFinished(long playerId)
        {
            Monitor.Log($"OnScreenPlaybackFinished called - PlayerId: {playerId}");

            if (ScreenPlayer.Value != null)
            {
                ScreenPlayer.Value.Stop();
                ScreenPlayer.Value = null;
            }

            LocalState.Value.IsPlaying = false;
            CurrentTrackPlayer.Value = null;

            // Restore music volume
            Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);

            // Open main menu for this screen
            MainMenu menu = new MainMenu(this);
            setActiveMenu(menu);
        }

        #endregion

        #region private Methods
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            else if (activeMenu is not null && Game1.activeClickableMenu is not null)
            {
                activeMenu.handleButton(e.Button);
                return;
            }
            else if (Game1.activeClickableMenu is null && Game1.player.ActiveItem is not null && !Game1.player.ActiveItem.isPlaceable() && (e.Button.ToString() == "MouseLeft" || e.Button == SButton.ControllerX || e.Button == SButton.ControllerY))
            {
                string instrument = Game1.player.ActiveItem.Name;
                if (config.InstrumentData.ContainsKey(instrument))
                {
                    Helper.Input.Suppress(e.Button);
                    Monitor.Log($"started Playing on Instrument {instrument}");
                    openInstrumentMenu(config.InstrumentData[instrument]);
                    return;
                }
            }

            if (Game1.activeClickableMenu is null && Game1.player.IsSitting())
            {
                string input = e.Button.ToString();

                // ========================================
                // 1. LET THE GAME HANDLE STANDING UP
                // ========================================
                if (input == "MouseRight" || input == "Left" || input == "Right" || input == "Up" || input == "Down" ||
                    input == "ControllerLeftThumbstickLeft" || input == "ControllerLeftThumbstickRight" ||
                    input == "ControllerLeftThumbstickUp" || input == "ControllerLeftThumbstickDown" ||
                    e.Button == SButton.ControllerB)
                {
                    long playerId = Game1.player.UniqueMultiplayerID;

                    if (remotePlayers.ContainsKey(playerId))
                    {
                        remotePlayers[playerId].stopSong();
                        remotePlayers.Remove(playerId);
                    }

                    if (LocalState.Value.OwnerId == playerId)
                    {
                        LocalState.Value.OwnerId = null;
                        LocalState.Value.OccupiedPosition = null;
                    }

                    return;
                }

                // ========================================
                // 2. OPEN MENU with specific buttons
                // ========================================
                if (input == "MouseLeft" || e.Button == SButton.ControllerX || e.Button == SButton.ControllerY)
                {
                    GameLocation location = Game1.currentLocation;
                    Farmer player = Game1.player;
                    string tile_name;

                    try
                    {
                        tile_name = location.getObjectAtTile((int)player.Tile.X, (int)player.Tile.Y, true).Name;
                    }
                    catch (NullReferenceException)
                    {
                        return;
                    }

                    if (config.InstrumentData.ContainsKey(tile_name))
                    {
                        string pianoKey = $"{location.Name}_{(int)player.Tile.X}_{(int)player.Tile.Y}";

                        if (LocalState.Value.OwnerId.HasValue && LocalState.Value.OwnerId.Value != player.UniqueMultiplayerID)
                        {
                            Game1.addHUDMessage(new HUDMessage("Someone is already playing this piano!", 3));
                            return;
                        }

                        Helper.Input.Suppress(e.Button);
                        Monitor.Log($"Opening piano menu at {tile_name} with {e.Button}");
                        openInstrumentMenu(config.InstrumentData[tile_name]);
                    }
                    else
                    {
                        Monitor.LogOnce($"No instrument data found for '{tile_name}'", LogLevel.Debug);
                    }
                    return;
                }
                return;
            }
            else
            {
                activeMenu = null;
            }
        }

        public void openInstrumentMenu(string soundName)
        {
            LocalState.Value.OwnerId = Game1.player.UniqueMultiplayerID;
            LocalState.Value.OccupiedPosition = $"{Game1.currentLocation.Name}_{(int)Game1.player.Tile.X}_{(int)Game1.player.Tile.Y}";

            sound = soundName;
            soundLow = soundName + "Low";
            soundHigh = soundName + "High";
            lowerOctaves = Game1.soundBank.Exists(soundLow);
            upperOctaves = Game1.soundBank.Exists(soundHigh);
            Monitor.Log($"using sound {sound}");
            Monitor.Log($"extended Pitches lower: {lowerOctaves}, higher: {upperOctaves}");

            MainMenu pianoMenu = new MainMenu(this);
            activeMenu = pianoMenu;
            Game1.activeClickableMenu = pianoMenu;
        }

        private bool loadSoundData(string soundName)
        {
            try
            {
                SoundEffect audio;
                string audioPath = Path.Combine(Helper.DirectoryPath, "assets", "sounds", soundName + ".wav");
                using (var stream = new FileStream(audioPath, FileMode.Open))
                {
                    audio = SoundEffect.FromStream(stream);
                }
                CueDefinition cueDef = new CueDefinition(soundName, audio, Game1.audioEngine.GetCategoryIndex("Sound"));
                Game1.soundBank.AddCue(cueDef);
                return true;
            }
            catch
            {
                Monitor.Log($"Couldn't load {soundName}.wav", LogLevel.Debug);
                return false;
            }
        }

        private void loadDefaultSounds()
        {
            string[] defaultSounds = { "toyPianoLow", "toyPianoHigh", "fluteLow", "fluteHigh", "Mushroomy.PlayablePiano_Piano", "Mushroomy.PlayablePiano_PianoLow", "Mushroomy.PlayablePiano_PianoHigh" };
            foreach (string sound in defaultSounds)
            {
                loadSoundData(sound);
                Monitor.Log($"sound {sound} loaded", LogLevel.Debug);
            }
        }

        private void checkConfigSounds()
        {
            foreach (var entry in config.InstrumentData)
            {
                var sound = entry.Value;
                if (!Game1.soundBank.Exists(sound))
                {
                    if (!loadSoundData(sound))
                    {
                        Monitor.Log($"Couldn't load {sound} for {entry.Key}. Skipping Entry", LogLevel.Warn);
                        continue;
                    }
                    Monitor.Log($"sound {sound} loaded", LogLevel.Debug);
                }
                if (!Game1.soundBank.Exists(sound + "Low"))
                {
                    if (loadSoundData(sound + "Low"))
                    {
                        Monitor.Log($"  lower range for {sound} loaded", LogLevel.Debug);
                    }
                }
                else
                {
                    Monitor.Log($"  lower range for {sound} loaded", LogLevel.Debug);
                }
                if (!Game1.soundBank.Exists(sound + "High"))
                {
                    if (loadSoundData(sound + "High"))
                    {
                        Monitor.Log($"  upper range for {sound} loaded", LogLevel.Debug);
                    }
                }
                else
                {
                    Monitor.Log($"  upper range for {sound} loaded", LogLevel.Debug);
                }
            }
        }

        private void reset_instrument_sounds(string command, string[] args)
        {
            Monitor.Log("Reseting Trigger Action");
            foreach (var action in TriggerActionManager.GetActionsForTrigger("Mushroomy.PlayablePiano_SaveLoaded"))
            {
                if (Game1.player.triggerActionsRun.Contains(action.Data.Id))
                {
                    Monitor.Log($"{action.Data.Id} has Marked its AddSound Action as applied, please notify the Mod's author to set 'MarkActionApplied' as false.", LogLevel.Debug);
                    Game1.player.triggerActionsRun.Remove(action.Data.Id);
                }
            }
            Monitor.Log("Reseting Pianos");
            addInstrument(new string[] { "", "Dark Piano", "Mushroomy.PlayablePiano_Piano" }, new TriggerActionContext(), out _);
            addInstrument(new string[] { "", "UprightPiano", "Mushroomy.PlayablePiano_Piano" }, new TriggerActionContext(), out _);
            Monitor.Log("Rerunning CP Integration");
            CPIntegration(this, new SaveLoadedEventArgs());
        }

        private void OnPeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
        {
            if (remotePlayers.ContainsKey(e.Peer.PlayerID))
            {
                remotePlayers[e.Peer.PlayerID].stopSong();
                remotePlayers.Remove(e.Peer.PlayerID);
                Monitor.Log($"Cleaned up playback for disconnected player {e.Peer.PlayerID}", LogLevel.Debug);
            }
        }
        #endregion
    }
}