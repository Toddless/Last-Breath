namespace Core.Save
{
    public interface ISaveStorage
    {
        bool Exists(int slot);

        /// <summary>Reads the slot; a corrupt/missing main file falls back to the backup generation.
        /// Returns null when neither is readable.</summary>
        SaveFile? Load(int slot);

        /// <summary>Slot list data without parsing the full save (falls back to it if meta is missing).</summary>
        SaveMetadata? ReadMetadata(int slot);

        /// <summary>Atomic write: temp file + swap; the previous generation becomes the backup.</summary>
        void Write(int slot, SaveFile file);

        void Delete(int slot);
    }
}
