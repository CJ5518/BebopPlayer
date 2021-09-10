# BebopPlayer
The Bebop player

A music player that plays locally downloaded music files via playlists.

### Usage:

Only works on Windows.

Music is played via locally downloaded audio files, which are loaded into the program via playlists.
The playlists are Lua files that return an array of strings, those strings being the absolute file paths to your music files.
All of the current playlists are just Directory.GetFiles() over some directories on my computer, you'll have to make your own playlists to get any use out of this.

All of the lua files are run via [NLua](https://github.com/NLua/NLua) and have no sandboxing of any kind, so feel free to get crazy with them.

### Hotkeys:

These are all in the code somewhere, so I recommend changing them if they don't suit you.
* F8: Play/Pause
* F9: Skip
* F10: Volume down
* F11: Volume up

### Building:
You'll probably need something to compile the C#, I used Visual Studio.

Dependencies:

* [NLua](https://github.com/NLua/NLua)
* [NAudio](https://github.com/naudio/NAudio)

Uses WinForms, which is why it only works on windows.
