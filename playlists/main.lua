import("System");
import("System.IO");
import("System.Text");

local function regDirectory(path, searchString, recursive)
	local enum;
	if recursive then enum = SearchOption.AllDirectories;
	else enum = SearchOption.TopDirectoryOnly; end
	return function() return Directory.GetFiles(path, searchString, enum) end;
end

return {
	{
		isCategory = true,
		name = "Video Game Soundtracks",
		{
			isCategory = true,
			name = "Mother Series",
			{
				isCategory = false,
				name = "Mother 3",
				playlistFunc = regDirectory("F:\\Music\\Mother 3", "*.*", true)
			},
			{
				isCategory = false,
				name = "Earthbound",
				playlistFunc = regDirectory("F:\\Music\\Earthbound", "*.*", true)
			}
		},
		{
			isCategory = false,
			name = "Sonic Mania",
			playlistFunc = regDirectory("F:\\Music\\SonicMania", "*.*", true)
		}
	},
	{
		isCategory = true,
		name = "Movie Soundtracks"
	}
};