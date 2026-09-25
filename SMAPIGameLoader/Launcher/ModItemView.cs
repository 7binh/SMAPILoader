using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace SMAPIGameLoader.Launcher;

public class ModItemView
{
    public string NameText = "Unknow";
    public string VersionText = "Unknow";
    public string FolderPathText = "Unknow";

    public readonly string modName = "unknow";
    public readonly string modVersion = "unknow";
    public readonly string modFolderPath = "unknow";
    public DateTime InstalledDate { get; set; } = DateTime.MinValue;

    public ModItemView(string manifestFilePath, int modListIndex)
    {
        try
        {
            var manifestText = File.ReadAllText(manifestFilePath);
            var manifest = JObject.Parse(manifestText);

            this.modName = manifest["Name"]?.ToString() ?? "Unknown";
            this.modVersion = manifest["Version"]?.ToString() ?? "Unknown";

            this.modFolderPath = Path.GetDirectoryName(manifestFilePath) ?? string.Empty;
            
            var modsIndex = modFolderPath.IndexOf("/Mods", StringComparison.OrdinalIgnoreCase);
            if (modsIndex < 0)
                modsIndex = modFolderPath.IndexOf("\\Mods", StringComparison.OrdinalIgnoreCase);

            var relativeModDir = modsIndex >= 0 ? modFolderPath.Substring(modsIndex + 5) : modFolderPath;
            FolderPathText = $"Folder: {relativeModDir}";

            DateTime date = DateTime.MinValue;
            if (!string.IsNullOrEmpty(modFolderPath) && Directory.Exists(modFolderPath))
            {
                var dirWrite = Directory.GetLastWriteTime(modFolderPath);
                var dirCreate = Directory.GetCreationTime(modFolderPath);
                date = dirWrite > dirCreate ? dirWrite : dirCreate;
            }
            if (File.Exists(manifestFilePath))
            {
                var fileWrite = File.GetLastWriteTime(manifestFilePath);
                if (fileWrite > date)
                    date = fileWrite;
            }
            InstalledDate = date;
        }
        catch (Exception ex)
        {
            this.modFolderPath = Path.GetDirectoryName(manifestFilePath) ?? string.Empty;
            FolderPathText = modFolderPath;
            ErrorDialogTool.Show(ex, "Error try parser mod folder path: " + this.modFolderPath);
        }

        UpdateIndex(modListIndex);
        this.VersionText = $"Version: {modVersion}";
    }

    public void UpdateIndex(int newIndex)
    {
        this.NameText = $"[{newIndex + 1}]: {modName}";
    }
}
