using Microsoft.Xna.Framework;

namespace Playable_Piano
{
    internal class startPlayback
    {
        public Vector2 performerTilePos;
        public List<Note> notation = new List<Note>();
        public string sound = string.Empty;
        public long playerId;

        internal startPlayback() { }

        internal startPlayback(Vector2 performerTile, List<Note> notationToSend, string soundName, long playerId)
        {
            performerTilePos = performerTile;
            notation = notationToSend;
            sound = soundName;
            this.playerId = playerId;
        }
    }
    
    internal class stopPlayback
    {
        public long playerId;
        
        internal stopPlayback() { }
        
        internal stopPlayback(long playerId)
        {
            this.playerId = playerId;
        }
    }

    internal class playNote
    {
        public string sound = string.Empty;
        public int pitch;
        public Vector2 performerTile;

        internal playNote() {}

        internal playNote(string soundName, int playedPitch, Vector2 tile)
        {
            sound = soundName;
            pitch = playedPitch;
            performerTile = tile;
        }
    }
}