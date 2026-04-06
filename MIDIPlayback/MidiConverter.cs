using MidiParser;
using StardewModdingAPI;
namespace Playable_Piano
{
    public class MidiConverter
    {
        private MidiFile midiFile;
        private int mainTrackNumber;
        private int TicksPerQuarterNote;
        internal PlayablePiano mainMod;

        internal MidiConverter(MidiFile midiFile, int mainTrackNumber, PlayablePiano mainMod)
        {
            this.midiFile = midiFile;
            this.mainTrackNumber = mainTrackNumber;
            TicksPerQuarterNote = midiFile.TicksPerQuarterNote == 0 ? 24 : midiFile.TicksPerQuarterNote; // standard value
            mainMod.Monitor.Log($"Converter: Ticks per quarter Note: {midiFile.TicksPerQuarterNote}");
            this.mainMod = mainMod;
        }

        public List<Note> convertToNotes()
        {
            List<Note> notes = new List<Note>();
            // extract tempo data
            // (Tick, BPM) Tuples
            List<(int, int)> BPMIntervals = new List<(int, int)>();
            foreach (MidiTrack track in midiFile.Tracks)
            {
                foreach (MidiEvent midiEvent in track.MidiEvents)
                {
                    if (midiEvent.MetaEventType == MetaEventType.Tempo)
                    {
                        mainMod.Monitor.Log($"Converter: BPM changes to {midiEvent.Arg2} at midi Tick {midiEvent.Time}");
                        BPMIntervals.Add((midiEvent.Time, calculateTickRatio(midiEvent.Arg2)));
                        mainMod.Monitor.Log($"Converter: New TickRatio: {BPMIntervals[0].Item2}");
                    }
                }
            }
            // in case Tempo changes are spread out between tracks
            BPMIntervals.Sort((x,y) => {if (x.Item1 < y.Item1) {return -1;} else if (x.Item1 > y.Item1) {return 1;} else {return 0;}});
            int currentBPMInterval = 0;
            int midiTicksPerGameTick;
            if (BPMIntervals.Count > 0)
            {
                midiTicksPerGameTick = BPMIntervals[0].Item2;
            } 
            else
            {
                midiTicksPerGameTick = calculateTickRatio(120);
            }

            // extract note data
            if (mainTrackNumber == -1) // all tracks
            {
                foreach (MidiTrack track in midiFile.Tracks)
                {
                    foreach (MidiEvent midiEvent in track.MidiEvents) 
                    {
                        if (midiEvent.MidiEventType == MidiEventType.NoteOn)
                        {
                            // if current Note in new BPM Interval         AND note is played after next Interval starts
                            if (currentBPMInterval + 1 < BPMIntervals.Count && BPMIntervals[currentBPMInterval+1].Item1 < midiEvent.Time)
                            {
                                currentBPMInterval++;
                                midiTicksPerGameTick = BPMIntervals[currentBPMInterval].Item2;
                            }
                            notes.Add(new Note(midiEvent.Arg2, midiEvent.Time / midiTicksPerGameTick));
                        }
                    }
                }
            }
            else
            {
                // one specified track
                foreach (MidiEvent midiEvent in midiFile.Tracks[mainTrackNumber].MidiEvents)
                {
                    if (midiEvent.MidiEventType == MidiEventType.NoteOn)
                    {
                        // if current Note in new BPM Interval         AND note is played after next Interval starts
                        if (currentBPMInterval + 1 < BPMIntervals.Count && BPMIntervals[currentBPMInterval+1].Item1 < midiEvent.Time)
                        {
                            currentBPMInterval++;
                            midiTicksPerGameTick = BPMIntervals[currentBPMInterval].Item2;
                        }
                        notes.Add(new Note(midiEvent.Arg2, midiEvent.Time / midiTicksPerGameTick));
                        
                    }
                }
            }
            notes.Sort((x, y) => { if (x.gameTick < y.gameTick) { return -1; } else if (x.gameTick > y.gameTick) { return 1; } else { return 0; } });

            // negative Note Pitch marks end of song,
            // during Playback for each played Note the Index gets incremented, and checked if it should be played (chords)
            // this causes an IndexOutOfBound on the last note, when only one End Note is existant
            notes.Add(new Note(-200,notes.Last().gameTick + 10));
            notes.Add(new Note(-200, notes.Last().gameTick + 10));
            return notes;
        }

        /// <summary>
        /// calculates, how many midiTicks fit into one Ingame Tick, depending on the current BPM. 
        /// </summary>
        /// <param name="BPM"></param>
        /// <returns></returns>
        private int calculateTickRatio(int BPM)
        {
            int ratio = BPM * TicksPerQuarterNote / 3600;
            if (ratio == 0)
            {
                // if both BPM and TicksPerQuarterNote are very small, result is less than zero
                mainMod.Monitor.Log($"Converter: BPM and Ticks per quarter Note are too small, defaulting to a ratio of 1");
                return 1;
            }
            else
            {
                return ratio;
            }
        }

    }

}