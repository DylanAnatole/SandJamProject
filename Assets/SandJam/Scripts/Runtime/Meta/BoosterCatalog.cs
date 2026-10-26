namespace SandJamTest
{
    // Every booster in one table: its id (the HUD button action and the save key) and its coin price. A new booster
    // is one row here plus its effect in BoosterController; inventory, prices and the save follow from the table.
    public static class BoosterCatalog
    {
        public const string Flare = "rocket", Swap = "swap", Select = "select";

        public sealed class Info
        {
            public readonly string Id; public readonly int Price;
            public Info(string id, int price) { Id = id; Price = price; }
        }

        // Original prices (HUD labels 150 / 500 / 700).
        public static readonly Info[] All = { new Info(Flare, 150), new Info(Swap, 500), new Info(Select, 700) };

        public static Info Find(string id)
        {
            foreach (var b in All) if (b.Id == id) return b;
            return null;
        }
    }
}
