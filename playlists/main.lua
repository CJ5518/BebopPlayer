import("System");
import("System.IO");
import("System.Text");

local function regDirectory(path, searchString, recursive)
	local enum;
	if recursive then enum = SearchOption.AllDirectories;
	else enum = SearchOption.TopDirectoryOnly; end
	return function() return Directory.GetFiles(path, searchString, enum) end;
end

local playlistTable = {
	{
		isCategory = true,
		name = "Video Games",
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
		name = "Movies/TV"
	},
	{
		isCategory = false,
		name = "Banger mix #1",
		playlistFunc = regDirectory("F:\\Music\\BangerMix#1", "*.mp3", false)
	},
	{
		isCategory = true,
		name = "Japanese Artists",
		{
			isCategory = true,
			name = "Tatsuro Yamashita"
		},
		{
			isCategory = true,
			name = "Mariya Takeuchi"
		}
	}
};

local function findTableByName(tab, name)
	for i, v in pairs(tab) do
		print(i,v);
		if type(v) == "table" then
			if v.name == name then
				return v;
			else
				local res = findTableByName(v, name);
				if res then return res; end
			end
		end
	end
end

local tatsuTable = findTableByName(playlistTable, "Tatsuro Yamashita");
for folder in luanet.each(Directory.GetDirectories("F:/Music/Tatsuro Yamashita/Albums")) do
	tatsuTable[#tatsuTable+1] = {name = Path.GetFileName(folder), isCategory = false, playlistFunc = regDirectory(folder, "*.mp3", false)}
end

return playlistTable;