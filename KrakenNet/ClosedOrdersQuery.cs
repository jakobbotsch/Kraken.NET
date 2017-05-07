using System;

namespace KrakenNet
{
    public class ClosedOrdersQuery
    {
        public bool IncludeTrades { get; set; }
        public int? UserReferenceId { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public string StartTransactionId { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string EndTransactionId { get; set; }
        public int Offset { get; set; }
        public CloseTimeType? CloseTime { get; set; }
    }

    public enum CloseTimeType
    {
        Open,
        Close,
        Both,
    }

    internal static class CloseTimeTypeExtensions
    {
        public static string ToApiString(this CloseTimeType type)
        {
            switch (type)
            {
                case CloseTimeType.Open: return "open";
                case CloseTimeType.Close: return "close";
                case CloseTimeType.Both: return "both";
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
