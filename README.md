# BebopPlayer
The Bebop player

A music player that plays locally downloaded music files via playlists.

### Usage:

Only works on Windows, although maybe a linux version coming soon because I don't wanna use windows 11.

Music is played via locally downloaded audio files, which are loaded into the program via playlists (or a file dialog).
The playlists are loaded from a lua file in the playlists folder called main.lua.
The file basically returns a list of categories which can contain functions that must return arrays of absoulute file paths to audio files. For the specifics on this functionality please see the file itself.
You can only go two categories deep due to the number of combo boxes being three. 

The lua file is run via [NLua](https://github.com/NLua/NLua) and has no sandboxing of any kind, so feel free to get crazy with it.

### Hotkeys:

These are all in the code somewhere, so I recommend changing them if they don't suit you.
* F8: Play/Pause
* F9: Skip
* F10: Volume down
* ~F11: Volume up~ (removed because f11 is actually a pretty useful key, will likely end up adding the volume up control back at some point)

### Building:
You'll probably need something to compile the C#, I used Visual Studio.

Dependencies:

* [NLua](https://github.com/NLua/NLua)
* [NAudio](https://github.com/naudio/NAudio)

Uses WinForms, which is why it only works on windows.
