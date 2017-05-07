using System;
using System.Globalization;

namespace KrakenNet
{
    public class OrderRequest
    {
        public string Pair { get; set; }
        public OrderKind Kind { get; set; }
        public OrderType Type { get; set; }
        public OrderPrice? Price { get; set; }
        public OrderPrice? Price2 { get; set; }
        public decimal Volume { get; set; }
        public string Leverage { get; set; }
        public bool VolumeInQuoteCurrency { get; set; }
        public bool PreferFeeInBaseCurrency { get; set; }
        public bool PreferFeeInQuoteCurrency { get; set; }
        public bool NoMarketPriceProtection { get; set; }
        public bool PostOnlyOrder { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public int? RelativeStartTimeSeconds { get; set; }
        public DateTimeOffset? ExpireTime { get; set; }
        public int? RelativeExpireTimeSeconds { get; set; }
        public int? UserReferenceId { get; set; }
        public bool ValidateOnly { get; set; }

        public OrderType? CloseOrderType { get; set; }
        public OrderPrice? CloseOrderPrice { get; set; }
        public OrderPrice? CloseOrderPrice2 { get; set; }
    }

    public struct OrderPrice
    {
        public OrderPrice(OrderPriceType type, decimal amount)
        {
            Type = type;
            Amount = amount;
        }

        public OrderPriceType Type { get; }
        public decimal Amount { get; set; }

        internal string ToApiString()
        {
            string value = Amount.ToString(CultureInfo.InvariantCulture);

            switch (Type)
            {
                case OrderPriceType.Absolute: return value;
                case OrderPriceType.Add: return $"+{value}";
                case OrderPriceType.AddPercentage: return $"+{value}%";
                case OrderPriceType.Subtract: return $"-{value}";
                case OrderPriceType.SubtractPercentage: return $"-{value}%";
                case OrderPriceType.AddOrSubtract: return $"#{value}";
                case OrderPriceType.AddOrSubtractPercentage: return $"#{value}%";
                default: throw new InvalidOperationException("Current type is invalid");
            }
        }

        public override string ToString() => ToApiString();

        public static OrderPrice Absolute(decimal price)
            => new OrderPrice(OrderPriceType.Absolute, price);

        public static OrderPrice Add(decimal value)
            => new OrderPrice(OrderPriceType.Add, value);

        public static OrderPrice AddPercentage(decimal percentage)
            => new OrderPrice(OrderPriceType.AddPercentage, percentage);

        public static OrderPrice Subtract(decimal value)
            => new OrderPrice(OrderPriceType.Subtract, value);

        public static OrderPrice SubtractPercentage(decimal percentage)
            => new OrderPrice(OrderPriceType.SubtractPercentage, percentage);

        public static OrderPrice AddOrSubtract(decimal value)
            => new OrderPrice(OrderPriceType.AddOrSubtract, value);

        public static OrderPrice AddOrSubtractPercentage(decimal percentage)
            => new OrderPrice(OrderPriceType.AddOrSubtractPercentage, percentage);

        /// <summary>
        /// Parses a price in Kraken format.
        /// </summary>
        /// <example><c>&quot;123.123&quot;</c> parses to an absolute price of <c>123.123</c>.</example>
        /// <example><c>&quot;+5&quot;</c> parses to the relative price of +5.</example>
        /// <example><c>&quot;+5%&quot;</c> parses to the relative price of +5%.</example>
        /// <remarks>See the Kraken documentation for information about order price specifications.</remarks>
        public static OrderPrice Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value must not be empty or white-space only");

            value = value.Trim();

            OrderPriceType type = OrderPriceType.Absolute;
            switch (value[0])
            {
                case '+': type = OrderPriceType.Add; break;
                case '-': type = OrderPriceType.Subtract; break;
                case '#': type = OrderPriceType.AddOrSubtract; break;
            }

            bool pct = false;
            if (value[value.Length - 1] == '%')
            {
                if (type == OrderPriceType.Absolute)
                    throw new ArgumentException("Percentages can only be specified as relative values", nameof(value));

                type++;
                pct = true;
            }

            int start = type == OrderPriceType.Absolute ? 0 : 1;
            int end = pct ? value.Length - 1 : value.Length;

            string amountSubstring = value.Substring(start, end - start);
            if (!decimal.TryParse(amountSubstring, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal amount))
                throw new ArgumentException($"Could not parse value {amountSubstring} as a number", nameof(value));

            return new OrderPrice(type, amount);
        }

        public static implicit operator OrderPrice(decimal price)
            => Absolute(price);
    }

    public enum OrderPriceType
    {
        Absolute,
        Add,
        AddPercentage,
        Subtract,
        SubtractPercentage,
        AddOrSubtract,
        AddOrSubtractPercentage,
    }

    public enum OrderKind
    {
        None,
        Buy,
        Sell,
    }

    public enum OrderType
    {
        None,
        Market,
        Limit,
        StopLoss,
        TakeProfit,
        StopLossProfit,
        StopLossProfitLimit,
        StopLossLimit,
        TakeProfitLimit,
        TrailingStop,
        TrailingStopLimit,
        StopLossAndLimit,
        SettlePosition,
    }

    internal static class OrderKindExtensions
    {
        public static string ToApiString(this OrderKind kind)
        {
            switch (kind)
            {
                case OrderKind.Buy: return "buy";
                case OrderKind.Sell: return "sell";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static OrderKind FromApiString(string value)
        {
            switch (value)
            {
                case "buy": return OrderKind.Buy;
                case "sell": return OrderKind.Sell;
                default: throw new ArgumentException($"Value '{value}' does not represent an order kind", nameof(value));
            }
        }
    }

    internal static class OrderTypeExtensions
    {
        public static string ToApiString(this OrderType type)
        {
            switch (type)
            {
                case OrderType.Market: return "market";
                case OrderType.Limit: return "limit";
                case OrderType.StopLoss: return "stop-loss";
                case OrderType.TakeProfit: return "take-profit";
                case OrderType.StopLossProfit: return "stop-loss-profit";
                case OrderType.StopLossProfitLimit: return "stop-loss-profit-limit";
                case OrderType.StopLossLimit: return "stop-loss-limit";
                case OrderType.TakeProfitLimit: return "take-profit-limit";
                case OrderType.TrailingStop: return "trailing-stop";
                case OrderType.TrailingStopLimit: return "trailing-stop-limit";
                case OrderType.StopLossAndLimit: return "stop-loss-and-limit";
                case OrderType.SettlePosition: return "settle-position";
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static OrderType FromApiString(string value)
        {
            switch (value)
            {
                case "market": return OrderType.Market;
                case "limit": return OrderType.Limit;
                case "stop-loss": return OrderType.StopLoss;
                case "take-profit": return OrderType.TakeProfit;
                case "stop-loss-profit": return OrderType.StopLossProfit;
                case "stop-loss-profit-limit": return OrderType.StopLossProfitLimit;
                case "stop-loss-limit": return OrderType.StopLossLimit;
                case "take-profit-limit": return OrderType.TakeProfitLimit;
                case "trailing-stop": return OrderType.TrailingStop;
                case "trailing-stop-limit": return OrderType.TrailingStopLimit;
                case "stop-loss-and-limit": return OrderType.StopLossAndLimit;
                case "settle-position": return OrderType.SettlePosition;
                default: throw new ArgumentException($"Value '{value}' does not represent an order type", nameof(value));
            }
        }
    }
}
