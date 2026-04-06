namespace Playable_Piano
{
   
    public class TrackPlayer
    {
        private List<Note> notation;
        private int currentNote = 0;
        private int currentTick = 0;
        


        public TrackPlayer(List<Note> notes)
        {
            this.notation = notes;
        }

        /// <summary>
        /// Gets all Notes which are supposed to be played during the current game Tick.
        /// If no note is to be played on the current Game Tick an empty list gets returned.
        /// </summary>
        /// <returns> A List of Note Objects.
        /// </returns>
        public Note[] GetNextNote()
        {
            List<Note> notes = new List<Note>();
            while (currentTick == notation[currentNote].gameTick)
            {
                notes.Add(notation[currentNote]);
                currentNote++;
            }
            currentTick++;
            return notes.ToArray() ;
        }
    }
}
