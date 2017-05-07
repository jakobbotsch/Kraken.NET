using KrakenNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KrakenCli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            string[] secrets = File.ReadAllLines("Z:\\Temp\\kraken.txt");
            using (var cli = new KrakenClient(secrets[0], secrets[1]))
            {
                await SuperOrderAsync(cli, OrderKind.Sell, "LTCEUR", 15);
            }
        }
        private static async Task SuperOrderAsync(KrakenClient cli, OrderKind kind, string pair, decimal amount)
        {
            OrderBook orderBook = await cli.GetOrderBookAsync(pair);
            var (price, volumeAbove) = GetBestCurrentPrice(orderBook, kind, -1, amount);
            string txid = await LimitOrderAsync(cli, kind, pair, amount, price);
            Console.WriteLine("Opened initial order to {0} {1} {2} @ {3} ({4} volume above)",
                kind == OrderKind.Buy ? "buy" : "sell", amount, pair, price, volumeAbove);

            while (true)
            {
                Task<Dictionary<string, OrderInfo>> orderReq = cli.QueryOrdersAsync(new[] { txid });
                Task<OrderBook> orderBookReq = cli.GetOrderBookAsync(pair);

                OrderInfo order = (await orderReq).Values.Single();
                orderBook = await orderBookReq;

                if (order.Status == OrderStatus.Closed)
                    break;

                decimal amountLeft = order.Volume - order.VolumeExecuted;
                var (newPrice, newVolumeAbove) = GetBestCurrentPrice(orderBook, kind, price, amountLeft);

                if (newPrice == price)
                {
                    Console.WriteLine("Positioned correctly. Current volume: {0}, {1} above", amountLeft, newVolumeAbove);
                    await Task.Delay(2000);
                    continue;
                }

                try
                {
                    await cli.CancelOrderAsync(txid);
                }
                catch (KrakenException)
                {
                    await Task.Delay(2000);
                    continue;
                }

                order = (await cli.QueryOrdersAsync(new[] { txid })).Values.Single();
                amountLeft = order.Volume - order.VolumeExecuted;
                if (order.Status == OrderStatus.Closed || amountLeft <= 0)
                    break;

                txid = await LimitOrderAsync(cli, kind, pair, amountLeft, newPrice);
                Console.WriteLine("Moved order from {0} to {1} ({2} above)", price, newPrice, newVolumeAbove);

                price = newPrice;
                await Task.Delay(4000);
            }
        }

        private static async Task<string> LimitOrderAsync(
            KrakenClient cli, OrderKind kind, string pair, decimal amount, decimal price)
        {
            var result = await cli.AddOrderAsync(new OrderRequest
            {
                Kind = kind,
                Pair = pair,
                Type = OrderType.Limit,
                Price = price,
                Volume = amount,
            });

            string txid = result.TransactionIds.Single();
            return txid;
        }

        private static (decimal price, decimal above) GetBestCurrentPrice(OrderBook orderBook, OrderKind kind, decimal myCurPrice, decimal myVolume)
        {
            // Allowed cumulative volume percentage above our order
            const decimal allowAbove = 0.15m;

            decimal volumeAllowedAbove = myVolume * allowAbove;

            IReadOnlyList<OrderBookEntry> entries = kind == OrderKind.Buy ? orderBook.Bids : orderBook.Asks;

            int placeAbove = 0;
            while (true)
            {
                OrderBookEntry entry = entries[placeAbove];
                decimal volume = entry.Volume - (entry.Price == myCurPrice ? myVolume : 0);
                if (volume > volumeAllowedAbove)
                    break;

                placeAbove++;
                volumeAllowedAbove -= volume;
            }

            decimal price = entries[placeAbove].Price;
            if (kind == OrderKind.Buy)
                price += 0.00001m;
            else
                price -= 0.00001m;

            return (price, myVolume * allowAbove - volumeAllowedAbove);
        }
    }
}