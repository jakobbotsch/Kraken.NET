using System;

namespace KrakenNet
{
    public enum LedgerType
    {
        Deposit,
        Withdrawal,
        Trade,
        Margin,
    }

    internal static class LedgerTypeExtensions
    {
        public static string ToApiString(this LedgerType type)
        {
            switch (type)
            {
                case LedgerType.Deposit: return "deposit";
                case LedgerType.Withdrawal: return "withdrawal";
                case LedgerType.Trade: return "trade";
                case LedgerType.Margin: return "margin";
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        internal static LedgerType FromApiString(string value)
        {
            switch (value)
            {
                case "deposit": return LedgerType.Deposit;
                case "withdrawal": return LedgerType.Withdrawal;
                case "trade": return LedgerType.Trade;
                case "margin": return LedgerType.Margin;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}
