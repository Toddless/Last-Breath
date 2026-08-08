namespace Battle.Source.UIElements.PassiveWheel
{
    /// <summary>
    /// The localisation keys the wheel speaks in, and the placeholder names its templates carry. In one
    /// place because two of them are said from two spots — the price of a route is printed under the
    /// cursor while hovering and on the card once a node is selected, and a key spelled twice is a line
    /// that goes silently missing in one of them.
    /// </summary>
    public static class PassiveWheelText
    {
        public const string KindPrefix = "UI_PassiveTree_Kind_";
        public const string Taken = "UI_PassiveTree_Cost_Taken";
        public const string Granted = "UI_PassiveTree_Cost_Granted";
        public const string PathCost = "UI_PassiveTree_Cost_Path";
        public const string SocketPromise = "UI_PassiveTree_SocketPromise";
        public const string NoPoints = "UI_PassiveTree_NoPoints";
        public const string AugmentsWaiting = "UI_PassiveTree_AugmentsWaiting";
        public const string RespecConfirm = "UI_PassiveTree_Respec_Confirm";

        public const string PointsValue = "points";
        public const string TierValue = "tier";
        public const string CountValue = "count";
    }
}
