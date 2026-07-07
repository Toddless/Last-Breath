namespace Battle.Source.Save
{
    using Core.Save;

    /// <summary>Checkpoint-facing orchestration over SaveManager + SaveStorage: slots, metadata
    /// peeking for the UI, and the two-step load (request → scene reload → apply).</summary>
    public interface ISaveGameService
    {
        int SlotCount { get; }

        /// <summary>Saving is forbidden in battle (checkpoints are unreachable there, this is the backstop).</summary>
        bool CanSave { get; }

        /// <summary>True between a load request and its application in the freshly loaded scene.</summary>
        bool HasPendingLoad { get; }

        bool HasSave(int slot);
        SaveMetadata? PeekSlot(int slot);
        void SaveToSlot(int slot);
        void DeleteSlot(int slot);

        /// <summary>Starts the load: reads the slot, resets the NPC population counter and
        /// schedules a scene reload. False when the slot is empty/corrupt or no player exists.</summary>
        bool RequestLoad(int slot);

        /// <summary>Applies the pending file. The SaveDirector in the fresh scene calls this
        /// once every node's _Ready (and the deferred spawn fills) have run.</summary>
        void ApplyPendingLoad();
    }
}
