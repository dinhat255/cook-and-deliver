namespace CookAndDeliver.Cooking.Core
{
    // May trang thai cua mot o nau nhiet
    public enum HeatSlotState
    {
        Idle,       // O trong
        Cooking,    // Dang duoc lam chin (0s -> cookDuration)
        Perfect,    // Chin vang hoan hao (cookDuration -> warnDuration)
        Warning,    // Canh bao sap chay (warnDuration -> burnDuration)
        Burnt       // Chay den (> burnDuration)
    }
}
