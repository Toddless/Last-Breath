namespace Core.Save
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Pure System.IO implementation — testable without the Godot runtime. The Godot side only
    /// supplies the root path (ProjectSettings.GlobalizePath("user://saves")) at registration.
    /// </summary>
    public class SaveStorage(string rootPath) : ISaveStorage
    {
        private const string SaveFileName = "save.json";
        private const string BackupFileName = "save.json.bak";
        private const string TempFileName = "save.json.tmp";
        private const string MetaFileName = "meta.json";

        public bool Exists(int slot) => File.Exists(SavePath(slot));

        public SaveFile? Load(int slot) => TryRead(SavePath(slot)) ?? TryRead(BackupPath(slot));

        public SaveMetadata? ReadMetadata(int slot)
        {
            string metaPath = Path.Combine(SlotDirectory(slot), MetaFileName);
            if (File.Exists(metaPath))
            {
                try
                {
                    var metadata = JsonConvert.DeserializeObject<SaveMetadata>(File.ReadAllText(metaPath));
                    if (metadata != null) return metadata;
                }
                catch (Exception e) when (e is JsonException or IOException)
                {
                }
            }

            return Load(slot)?.Metadata;
        }

        public void Write(int slot, SaveFile file)
        {
            Directory.CreateDirectory(SlotDirectory(slot));
            string tempPath = Path.Combine(SlotDirectory(slot), TempFileName);
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(file, Formatting.Indented));

            if (File.Exists(SavePath(slot)))
                File.Replace(tempPath, SavePath(slot), BackupPath(slot));
            else
                File.Move(tempPath, SavePath(slot));

            File.WriteAllText(Path.Combine(SlotDirectory(slot), MetaFileName),
                JsonConvert.SerializeObject(file.Metadata, Formatting.Indented));
        }

        public void Delete(int slot)
        {
            if (Directory.Exists(SlotDirectory(slot)))
                Directory.Delete(SlotDirectory(slot), recursive: true);
        }

        private string SlotDirectory(int slot) => Path.Combine(rootPath, $"slot_{slot}");
        private string SavePath(int slot) => Path.Combine(SlotDirectory(slot), SaveFileName);
        private string BackupPath(int slot) => Path.Combine(SlotDirectory(slot), BackupFileName);

        private static SaveFile? TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(path));
            }
            catch (Exception e) when (e is JsonException or IOException)
            {
                return null;
            }
        }
    }
}
