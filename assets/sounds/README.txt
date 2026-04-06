If you want to add a custom sound to an existing instrument, add a '.wav' file into this folder, and add an entry to the config.
for extended the pitch ranges, add a file named "<sound>Low.wav" and/or "<sound>High.wav" into this folder (this also works for base game sounds), each file will give you 2 additional octaves.

The pitches of the files can be arbitrary, however to ensure correct Playback of MIDI Files, the pitches should be:
    C6 for the base sound,
    C4 for the low pitched sound,
    C8 for the high pitched sound

To avoid "breaks" between the Octaves, the low and High sounds should speedup/slowed down versions of the base sound. The game uses this method to pitch the sounds and thus this will ensure, that the transition between the octaves will be seamless.
Thus the low sound should be the base sound at 0.25x Speed, and the high sound should be at 4x speed.