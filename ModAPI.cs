using StardewValley;

namespace Playable_Piano
{
    public class PianoApi
    {
        readonly internal PlayablePiano mainMod;
        internal PianoApi(PlayablePiano mod)
        {
            mainMod = mod;
        }
        public void playInstrument(string baseSoundName)
        {
            if (Game1.soundBank.Exists(baseSoundName))
            {
                mainMod.openInstrumentMenu(baseSoundName);
            }
            else
            {
                mainMod.Monitor.Log($"Sound {baseSoundName} does not exist in the soundBank", StardewModdingAPI.LogLevel.Trace);
            }
        }
    }
}