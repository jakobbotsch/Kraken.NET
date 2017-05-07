namespace KrakenNet
{
    public class AssetInfo
    {
        public AssetInfo(string name, string altName, string @class, int decimals, int displayDecimals)
        {
            Name = name;
            AltName = altName;
            Class = @class;
            Decimals = decimals;
            DisplayDecimals = displayDecimals;
        }

        public string Name { get; }
        public string AltName { get; }
        public string Class { get; }
        public int Decimals { get; }
        public int DisplayDecimals { get; }

        public override string ToString()
        {
            if (Name == AltName)
                return $"{Name} ({Class})";

            return $"{Name} or {AltName} ({Class})";
        }
    }
}
