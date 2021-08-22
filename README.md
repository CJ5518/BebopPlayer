# BebopPlayer
The Bebop player

A music player that plays locally downloaded music files via playlists.
The playlists are Lua files that return an array of strings, those strings being absolute file paths to music files.
All of the current playlists are just Directory.GetFiles() over some directories on my computer, you'll have to make your own playlists to get any use out of this.
