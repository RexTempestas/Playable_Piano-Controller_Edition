namespace Playable_Piano
{
    public enum Octave
    {
        low,
        normal,
        high
    }
    public class Note
    {
        public int pitch;
        public int gameTick;
        public Octave octave;


        // C4 = 60; C5 = 72; C6 = 84; C7 = 96
        public Note(int midiNote, int Tick)
        {
            // special case C5 % 24 would be 0 but should have pitch 0 not 2400
            if (midiNote % 24 == 0 && midiNote != 72) 
            {
                this.pitch = 2400;
            }
            else
            {
                this.pitch = midiNote < 0 ? -200 : (midiNote % 24) * 100;
            }
            this.gameTick = Tick;

            if (midiNote < 72)
            {
                this.octave = Octave.low;
            }
            else if (midiNote > 96)
            {
                this.octave = Octave.high;
            }
            else
            {
                this.octave = Octave.normal;
            }
        }

    }
}
