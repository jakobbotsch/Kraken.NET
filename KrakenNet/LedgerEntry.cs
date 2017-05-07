using System;

namespace KrakenNet
{
    public class LedgerEntry
    {
        public LedgerEntry(
            string ledgerId,
            string refId,
            DateTimeOffset timestamp,
            LedgerType type,
            string assetClass,
            string asset,
            decimal amount,
            decimal fee,
            decimal balance)
        {
            LedgerId = ledgerId;
            RefId = refId;
            Timestamp = timestamp;
            Type = type;
            AssetClass = assetClass;
            Asset = asset;
            Amount = amount;
            Fee = fee;
            Balance = balance;
        }

        public string LedgerId { get; }
        public string RefId { get; }
        public DateTimeOffset Timestamp { get; }
        public LedgerType Type { get; }
        public string AssetClass { get; }
        public string Asset { get; }
        public decimal Amount { get; }
        public decimal Fee { get; }
        public decimal Balance { get; }

        public override string ToString()
        {
            if (Fee != 0)
                return $"[{Timestamp}] {Type} {Amount} of {Asset} (Fee: {Fee})";

            return $"[{Timestamp}] {Type} {Amount} of {Asset} (no fee)";
        }
    }
}
