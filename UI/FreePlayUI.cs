using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Sickhead.Engine.Util;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Playable_Piano.UI
{
    internal class FreePlayUI : BaseUI
    {
        enum ButtonToPitches : int
        {
            // lower Octave
            Z = 0,
            S = 100,
            X = 200,
            D = 300,
            C = 400,
            V = 500,
            G = 600,
            B = 700,
            H = 800,
            N = 900,
            J = 1000,
            M = 1100,

            // uppper Octave
            Q = 1200,
            D2 = 1300,
            W = 1400,
            D3 = 1500,
            E = 1600,
            R = 1700,
            D5 = 1800,
            T = 1900,
            D6 = 2000,
            Y = 2100,
            D7 = 2200,
            U = 2300,

            I = 2400
        }
        private enum Octave
        {
            lower,
            normal,
            upper
        }
        private const int BUTTONHEIGHT = 16;
        private const int BUTTONWIDTH = 48;
        private const int BUTTONMARGIN = 5;

        private string sound;
        private string soundLow;
        private string soundHigh;
        private string selectedSoundCue;
        private Octave selectedOctave;
        protected override PlayablePiano mainMod { get; set; }
        private Texture2D pitchSelection;
        

        public FreePlayUI(PlayablePiano mod)
        {
            mainMod = mod;
            this.sound = mainMod.sound;
            this.soundLow = mainMod.soundLow;
            this.soundHigh = mainMod.soundHigh;
            this.selectedSoundCue = sound;
            this.selectedOctave = Octave.normal;
            this.pitchSelection = mainMod.Helper.ModContent.Load<Texture2D>("assets/UI/Pitch_UI.png");
            Game1.musicCategory.SetVolume(0f);
        }

        public override void draw(SpriteBatch b)
        {
            UIUtil.drawExitInstructions(b);
            drawControls(b);
        }

        private void drawControls(SpriteBatch b)
        {
            int xPos = 50;
            int yPos = Game1.viewport.Height - 50 - BUTTONHEIGHT * Game1.pixelZoom;
            Rectangle trebleTexture = new Rectangle((selectedOctave == Octave.upper ? 48 : 0),0,48,16);
            Rectangle altoTexture = new Rectangle((selectedOctave == Octave.normal ? 48 : 0), 16, 48, 16);
            Rectangle bassTexture = new Rectangle((selectedOctave == Octave.lower ? 48 : 0), 32, 48, 16);
            if (mainMod.lowerOctaves) new ClickableTextureComponent(new Rectangle(xPos, yPos, BUTTONWIDTH, BUTTONHEIGHT), pitchSelection, bassTexture, Game1.pixelZoom).draw(b);
            yPos -= (BUTTONMARGIN + BUTTONHEIGHT) * Game1.pixelZoom;
            if (mainMod.lowerOctaves || mainMod.upperOctaves) new ClickableTextureComponent(new Rectangle(xPos, yPos, BUTTONWIDTH, BUTTONHEIGHT), pitchSelection, altoTexture, Game1.pixelZoom).draw(b);
            yPos -= (BUTTONMARGIN + BUTTONHEIGHT) * Game1.pixelZoom;
            if (mainMod.upperOctaves) new ClickableTextureComponent(new Rectangle(xPos, yPos, BUTTONWIDTH, BUTTONHEIGHT), pitchSelection, trebleTexture, Game1.pixelZoom).draw(b);
        }

        public override void handleButton(SButton button)
        {
            mainMod.Helper.Input.Suppress(button);
            string input = button.ToString();
            ButtonToPitches playedNote;
            if (ButtonToPitches.TryParse(input, out playedNote))
            {
                int playedPitch = (int)playedNote;
                GameLocation location = Game1.currentLocation;
                Vector2 tileCords = Game1.player.Tile;
                if (!Game1.soundBank.GetCue(selectedSoundCue).IsPitchBeingControlledByRPC)
                {
                    Game1.soundBank.GetCueDefinition(selectedSoundCue).sounds.First().pitch = (playedPitch - 1200) / 1200f;
                    location.localSound(selectedSoundCue, tileCords, playedPitch);
                    if (Game1.IsMultiplayer)
                    {
                        List<long> playersAtLocation = Game1.currentLocation.farmers.Where(player => player.currentLocation == Game1.currentLocation).Select(player => player.UniqueMultiplayerID).ToList();
                        playersAtLocation.Remove(Game1.player.UniqueMultiplayerID);
                        mainMod.Helper.Multiplayer.SendMessage(new playNote(selectedSoundCue, playedPitch, tileCords), "playNote", new string[] {mainMod.ModManifest.UniqueID}, playersAtLocation.ToArray());
                    }
                }
                else
                {
                    //RPC Controlled sound pitching works in Multiplayer, thus no extra message needed.
                    location.playSound(selectedSoundCue, tileCords, playedPitch);
                }
                
            }
            else if (input == "LeftControl" && mainMod.lowerOctaves)
            {
                selectedSoundCue = soundLow;
                selectedOctave = Octave.lower;
            }
            else if (input == "LeftShift")
            {
                selectedSoundCue = sound;
                selectedOctave = Octave.normal;
            }
            else if (input == "Tab" && mainMod.upperOctaves)
            {
                selectedSoundCue = soundHigh;
                selectedOctave = Octave.upper;
            }
            else if (input == "MouseRight" || input == "Escape" || button == SButton.ControllerB)
            {
                Game1.musicCategory.SetVolume(Game1.options.musicVolumeLevel);
                exitThisMenu();
                MainMenu menu = new MainMenu(mainMod);
                mainMod.setActiveMenu(menu);
            }
        }
    }
}