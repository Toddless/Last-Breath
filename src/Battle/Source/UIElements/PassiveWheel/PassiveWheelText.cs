namespace Battle.Source.UIElements.PassiveWheel
{
    /// <summary>
    /// The localisation keys the wheel speaks in, and the placeholder names its templates carry. In one
    /// place because several of them are said from more than one spot — the price of a return is printed
    /// in the node popup, on the confirmation button and in the guard that catches a plan on its way out
    /// — and a key spelled twice is a line that goes silently missing in one of them.
    /// </summary>
    public static class PassiveWheelText
    {
        public const string KindPrefix = "UI_PassiveTree_Kind_";
        public const string Title = "UI_PassiveTree_Title";

        // The counters above the wheel.
        public const string PointsCaption = "UI_PassiveTree_Points_Caption";
        public const string SpentCaption = "UI_PassiveTree_Spent_Caption";
        public const string TotalCaption = "UI_PassiveTree_Total_Caption";
        public const string PointsDeltaTake = "UI_PassiveTree_Points_Delta_Take";
        public const string PointsDeltaRefund = "UI_PassiveTree_Points_Delta_Refund";
        public const string NoPoints = "UI_PassiveTree_NoPoints";
        public const string AugmentsWaiting = "UI_PassiveTree_AugmentsWaiting";

        // The buttons.
        public const string ApplyTake = "UI_PassiveTree_Apply_Take";
        public const string ApplyRefund = "UI_PassiveTree_Apply_Refund";
        public const string ApplyEmpty = "UI_PassiveTree_Apply_Empty";
        public const string Cancel = "UI_PassiveTree_Cancel";
        public const string RespecMode = "UI_PassiveTree_Respec_Mode";
        public const string SummaryToggle = "UI_PassiveTree_Summary_Toggle";
        public const string Frame = "UI_PassiveTree_Frame";
        public const string Close = "UI_PassiveTree_Close";

        // What the hint line says about the last gesture.
        public const string PathCost = "UI_PassiveTree_Cost_Path";
        public const string ModeCleared = "UI_PassiveTree_ModeCleared";
        public const string RefundNeedsButton = "UI_PassiveTree_RefundNeedsButton";
        public const string UnmarkedAlso = "UI_PassiveTree_UnmarkedAlso";
        public const string NotEnoughGold = "UI_PassiveTree_NotEnoughGold";
        public const string RespecDone = "UI_PassiveTree_RespecDone";

        // What the node popup adds while a return is being planned.
        public const string RefundTail = "UI_PassiveTree_RefundTail";
        public const string RefundPrice = "UI_PassiveTree_RefundPrice";
        public const string StrandedAugments = "UI_PassiveTree_StrandedAugments";

        // The ability's slots, listed in its node's popup.
        public const string SlotsCaption = "UI_PassiveTree_Slots_Caption";
        public const string SlotOpen = "UI_PassiveTree_Slot_Open";
        public const string SlotFilled = "UI_PassiveTree_Slot_Filled";
        public const string SlotHeld = "UI_PassiveTree_Slot_Held";
        public const string SlotUnopened = "UI_PassiveTree_Slot_Unopened";

        public const string SlotOwner = "UI_PassiveTree_Slot_Owner";

        // The panel of totals.
        public const string SummaryTitle = "UI_PassiveTree_Summary_Title";
        public const string SummaryEmpty = "UI_PassiveTree_Summary_Empty";
        public const string SummaryPlanned = "UI_PassiveTree_Summary_Planned";
        public const string SummaryConditional = "UI_PassiveTree_Summary_Conditional";
        public const string SummaryUnlocks = "UI_PassiveTree_Summary_Unlocks";
        public const string SummaryParameters = "UI_PassiveTree_Summary_Parameters";
        public const string SummaryKnobs = "UI_PassiveTree_Summary_Knobs";
        public const string SummaryKeystones = "UI_PassiveTree_Summary_Keystones";
        public const string SummaryColumnFlat = "UI_PassiveTree_Summary_Col_Flat";
        public const string SummaryColumnIncrease = "UI_PassiveTree_Summary_Col_Increase";
        public const string SummaryColumnMore = "UI_PassiveTree_Summary_Col_More";
        public const string SummaryColumnTotal = "UI_PassiveTree_Summary_Col_Total";

        // The guard that catches an unapplied plan on the way out.
        public const string CloseTitle = "UI_PassiveTree_Close_Title";
        public const string CloseTake = "UI_PassiveTree_Close_Take";
        public const string CloseRefund = "UI_PassiveTree_Close_Refund";
        public const string CloseApply = "UI_PassiveTree_Close_Apply";
        public const string CloseDiscard = "UI_PassiveTree_Close_Discard";
        public const string CloseStay = "UI_PassiveTree_Close_Stay";

        public const string PointsValue = "points";
        public const string CountValue = "count";
        public const string GoldValue = "gold";
        public const string TierValue = "tier";
        public const string NameValue = "name";
    }
}
