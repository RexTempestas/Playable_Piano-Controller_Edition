using Microsoft.Xna.Framework;

namespace Playable_Piano
{
    internal class startPlayback
    {
        public Vector2 performerTilePos;
        public List<Note> notation;
        public string sound;

        internal startPlayback() { }

        internal startPlayback(Vector2 performerTile, List<Note> notationToSend, string soundName)
        {
            performerTilePos = performerTile;
            notation = notationToSend;
            sound = soundName;
        }
    }
    internal class stopPlayback
    {
        internal stopPlayback(){ }
    }

    internal class playNote
    {
        public string sound;
        public int pitch;
        public Vector2 performerTile;

        playNote() {}

        internal playNote(string soundName, int playedPitch, Vector2 tile)
        {
            sound = soundName;
            pitch = playedPitch;
            performerTile = tile;
        }
        
    }
}