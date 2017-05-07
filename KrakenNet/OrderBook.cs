using System;
using System.Collections.Generic;

namespace KrakenNet
{
    public class OrderBook
    {
        public OrderBook(IReadOnlyList<OrderBookEntry> asks, IReadOnlyList<OrderBookEntry> bids)
        {
            Asks = asks ?? throw new ArgumentNullException(nameof(asks));
            Bids = bids ?? throw new ArgumentNullException(nameof(bids));
        }

        public IReadOnlyList<OrderBookEntry> Asks { get; }
        public IReadOnlyList<OrderBookEntry> Bids { get; }

        public override string ToString()
        {
            return $"{Asks.Count} asks, {Bids.Count} bids";
        }
    }

    public class OrderBookEntry
    {
        public OrderBookEntry(decimal price, decimal volume, DateTimeOffset time)
        {
            Price = price;
            Volume = volume;
            Time = time;
        }

        public decimal Price { get; }
        public decimal Volume { get; }
        public DateTimeOffset Time { get; }

        public override string ToString()
        {
            return $"{Volume} @ {Price}";
        }
    }
}
