using System;
using System.Collections.Generic;

namespace KrakenNet
{
    public class OrderInfo
    {
        public OrderInfo(
            string transactionId,
            string referralId,
            int? userReferenceId,
            OrderStatus status,
            DateTimeOffset openTime,
            DateTimeOffset? startTime,
            DateTimeOffset? expireTime,
            string pair,
            OrderKind kind,
            OrderType type,
            OrderPrice? price,
            OrderPrice? price2,
            string leverage,
            string description,
            string closeDescription,
            decimal volume,
            decimal volumeExecuted,
            decimal cost,
            decimal fee,
            decimal averagePrice,
            decimal stopPrice,
            decimal limitPrice,
            bool stopped,
            bool touched,
            bool liquidated,
            bool partial,
            bool volumeInQuoteCurrency,
            bool preferFeeInBaseCurrency,
            bool preferFeeInQuoteCurrency,
            bool noMarketPriceProtection,
            IReadOnlyList<string> tradeIds,
            DateTimeOffset? closeTime,
            string closeReason)
        {
            TransactionId = transactionId;
            ReferralId = referralId;
            UserReferenceId = userReferenceId;
            Status = status;
            OpenTime = openTime;
            StartTime = startTime;
            ExpireTime = expireTime;
            Pair = pair;
            Kind = kind;
            Type = type;
            Price = price;
            Price2 = price2;
            Leverage = leverage;
            Description = description;
            CloseDescription = closeDescription;
            Volume = volume;
            VolumeExecuted = volumeExecuted;
            Cost = cost;
            Fee = fee;
            AveragePrice = averagePrice;
            StopPrice = stopPrice;
            LimitPrice = limitPrice;
            Stopped = stopped;
            Touched = touched;
            Liquidated = liquidated;
            Partial = partial;
            VolumeInQuoteCurrency = volumeInQuoteCurrency;
            PreferFeeInBaseCurrency = preferFeeInBaseCurrency;
            PreferFeeInQuoteCurrency = preferFeeInQuoteCurrency;
            NoMarketPriceProtection = noMarketPriceProtection;
            TradeIds = tradeIds;
            CloseTime = closeTime;
            CloseReason = closeReason;
        }

        public string TransactionId { get; }
        public string ReferralId { get; }
        public int? UserReferenceId { get; }
        public OrderStatus Status { get; }
        public DateTimeOffset OpenTime { get; }
        public DateTimeOffset? StartTime { get; }
        public DateTimeOffset? ExpireTime { get; }
        public string Pair { get; }
        public OrderKind Kind { get; }
        public OrderType Type { get; }
        public OrderPrice? Price { get; }
        public OrderPrice? Price2 { get; }
        public string Leverage { get; }
        public string Description { get; }
        public string CloseDescription { get; }
        public decimal Volume { get; }
        public decimal VolumeExecuted { get; }
        public decimal Cost { get; }
        public decimal Fee { get; }
        public decimal AveragePrice { get; }
        public decimal StopPrice { get; }
        public decimal LimitPrice { get; }
        public bool Stopped { get; }
        public bool Touched { get; }
        public bool Liquidated { get; }
        public bool Partial { get; }
        public bool VolumeInQuoteCurrency { get; }
        public bool PreferFeeInBaseCurrency { get; }
        public bool PreferFeeInQuoteCurrency { get; }
        public bool NoMarketPriceProtection { get; }
        public IReadOnlyList<string> TradeIds { get; }

        public DateTimeOffset? CloseTime { get; }
        public string CloseReason { get; }

        public override string ToString()
        {
            return Description;
        }
    }

    public enum OrderStatus
    {
        Pending,
        Open,
        Closed,
        Canceled,
        Expired,
    }

    internal static class OrderStatusExtensions
    {
        public static OrderStatus FromApiString(string value)
        {
            switch (value)
            {
                case "pending": return OrderStatus.Pending;
                case "open": return OrderStatus.Open;
                case "closed": return OrderStatus.Closed;
                case "canceled": return OrderStatus.Canceled;
                case "expired": return OrderStatus.Expired;
                default: throw new ArgumentException($"Value {value} does not represent an order status", nameof(value));
            }
        }
    }
}
