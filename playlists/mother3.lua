import("System");
import("System.IO");
import("System.Text");

local function recurseRegisterDirectory(dir)
	local fileExtensions = {
		".mp3", ".ogg", ".wma", ".webm", ".m4a"
	}

	local list = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
	
	for q=0, list.Length-1 do
		local fullPath = list[q];
		local goodFile = false;
		for i, ext in pairs(fileExtensions) do
			if fullPath:find(ext) then
				goodFile = true;
				break;
			end
		end
		if not goodFile then
			list[q] = "";
		end
	end
end

return Directory.GetFiles("F:\\Music\\Mother 3", "*.*", SearchOption.AllDirectories);