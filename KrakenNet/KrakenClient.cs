using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KrakenNet
{
    /// <summary>
    /// Represents a client for accessing the Kraken API.
    /// </summary>
    public sealed class KrakenClient : IDisposable
    {
        private static readonly Stopwatch s_timer;
        private static readonly DateTime s_baseTime;

        static KrakenClient()
        {
            s_timer = Stopwatch.StartNew();
            s_baseTime = DateTime.UtcNow;
        }

        private readonly HttpClient _client;
        private readonly string _apiKey;
        private readonly byte[] _apiSecret;
        private readonly Func<Task<string>> _otpGenerator;

        /// <summary>
        /// Creates a client that allows access to only public Kraken APIs.
        /// </summary>
        public KrakenClient()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.kraken.com/0/"),
            };
        }

        /// <summary>
        /// Creates a client that allows access to both public and private Kraken APIs.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="apiSecret">The API secret.</param>
        public KrakenClient(string apiKey, string apiSecret, Func<Task<string>> otpGenerator = null) : this()
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret == null ? null : Convert.FromBase64String(apiSecret);
            _otpGenerator = otpGenerator;
        }

        public RateGate PrivateApiRateGate { get; set; }
        public RateGate OrderApiRateGate { get; set; }
        /// <summary>
        /// If <c>false</c>, the client will throw <see cref="KrakenException"/> on warnings.
        /// If <c>true</c>, warnings will be ignored.
        /// </summary>
        public bool IgnoreWarnings { get; set; } = false;
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            _client.Dispose();
        }

        public async Task<DateTimeOffset> GetServerTimeAsync()
        {
            double unixTime = (await QueryPublicAsync<JToken>("Time"))["unixtime"].Value<double>();
            return Util.FromUnixTime(unixTime);
        }

        private async Task<List<AssetInfo>> InternalGetAssetsAsync(string assets)
        {
            Dictionary<string, string> pms = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(assets))
                pms["asset"] = assets;

            JObject assetInfos = await QueryPublicAsync<JObject>("Assets", pms);

            List<AssetInfo> toReturn = new List<AssetInfo>(assetInfos.Count);
            foreach (KeyValuePair<string, JToken> pair in assetInfos)
            {
                string name = pair.Key;
                string altName = pair.Value.Value<string>("altname");
                string @class = pair.Value.Value<string>("aclass");
                int decimals  = pair.Value.Value<int>("decimals");
                int displayDecimals  = pair.Value.Value<int>("display_decimals");

                toReturn.Add(new AssetInfo(name, altName, @class, decimals, displayDecimals));
            }

            return toReturn;
        }

        public Task<List<AssetInfo>> GetAllAssetsAsync() => InternalGetAssetsAsync("");

        public Task<List<AssetInfo>> GetAssetsAsync(IEnumerable<string> assets)
            => InternalGetAssetsAsync(Util.ToCommaSeparatedChecked(assets, nameof(assets)));

        public Task<List<AssetInfo>> GetAssetsAsync(params string[] assets)
            => GetAssetsAsync((IEnumerable<string>)assets);

        public async Task<AssetInfo> GetAssetAsync(string asset)
            => (await GetAssetsAsync(asset))[0];

        public async Task<OrderBook> GetOrderBookAsync(string pair, int? count = null)
        {
            Dictionary<string, string> pms = new Dictionary<string, string>
            {
                ["pair"] = pair
            };

            if (count.HasValue)
                pms["count"] = count.Value.ToString();

            JObject obj = await QueryPublicAsync<JObject>("Depth", pms);
            // Returns a dictionary with pair name as key and order book as value
            JObject orderBook = (JObject)((JProperty)obj.First).Value;

            IEnumerable<OrderBookEntry> ParseEntries(JArray arr)
            {
                foreach (JToken val in arr)
                {
                    JArray entry = (JArray)val;
                    decimal price = entry[0].Value<decimal>();
                    decimal volume = entry[1].Value<decimal>();
                    DateTimeOffset time = Util.FromUnixTime(entry[2].Value<double>());
                    yield return new OrderBookEntry(price, volume, time);
                }
            }

            return new OrderBook(
                ParseEntries(orderBook.Value<JArray>("asks")).ToList(),
                ParseEntries(orderBook.Value<JArray>("bids")).ToList());
        }

        public Task<Dictionary<string, decimal>> GetAccountBalanceAsync()
        {
            return QueryPrivateAsync<Dictionary<string, decimal>>("Balance", 1, 0);
        }

        public async Task<(int totalCount, List<LedgerEntry> entries)> GetLedgerAsync(LedgerQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (query.StartTime.HasValue && query.StartLedgerId != null)
            {
                throw new ArgumentException(
                    $"Only one of {nameof(query.StartTime)} and " +
                    $"{nameof(query.StartLedgerId)} can be set", nameof(query));
            }

            if (query.EndTime.HasValue && query.EndLedgerId != null)
            {
                throw new ArgumentException(
                    $"Only one of {nameof(query.StartTime)} and " +
                    $"{nameof(query.StartLedgerId)} can be set", nameof(query));
            }

            Dictionary<string, string> pms = new Dictionary<string, string>();
            if (query.Assets != null && query.Assets.Any())
                pms["asset"] = Util.ToCommaSeparatedChecked(query.Assets, $"{nameof(query)}.{nameof(query.Assets)}");
            if (query.Type.HasValue)
                pms["type"] = query.Type.Value.ToApiString();
            if (query.StartTime.HasValue)
                pms["start"] = Util.ToUnixTime(query.StartTime.Value).ToString();
            if (query.StartLedgerId != null)
                pms["start"] = query.StartLedgerId;
            if (query.EndTime.HasValue)
                pms["end"] = Util.ToUnixTime(query.EndTime.Value).ToString();
            if (query.EndLedgerId != null)
                pms["end"] = query.EndLedgerId;

            pms["ofs"] = query.Offset.ToString();

            JToken info = await QueryPrivateAsync<JToken>("Ledgers", 2, 0, pms);
            int count = info.Value<int>("count");
            return (count, ParseLedgerEntries(info.Value<JObject>("ledger")));
        }

        private static List<LedgerEntry> ParseLedgerEntries(JObject dict)
        {
            List<LedgerEntry> entries = new List<LedgerEntry>();
            foreach (KeyValuePair<string, JToken> kvp in dict)
            {
                JToken value = kvp.Value;

                string ledgerId = kvp.Key;
                string refId = value.Value<string>("refid");
                double time = kvp.Value.Value<long>("double");
                string ltype = kvp.Value.Value<string>("type");
                string assetClass = kvp.Value.Value<string>("aclass");
                string asset = kvp.Value.Value<string>("asset");
                decimal amount = kvp.Value.Value<decimal>("amount");
                decimal fee = kvp.Value.Value<decimal>("fee");
                decimal balance = kvp.Value.Value<decimal>("balance");

                entries.Add(new LedgerEntry(
                    ledgerId,
                    refId,
                    Util.FromUnixTime(time),
                    LedgerTypeExtensions.FromApiString(ltype),
                    assetClass,
                    asset,
                    amount,
                    fee,
                    balance));
            }

            return entries;
        }
        
        public async Task<List<LedgerEntry>> QueryLedgerAsync(IEnumerable<string> ledgerIds)
        {
            if (ledgerIds == null)
                throw new ArgumentNullException(nameof(ledgerIds));

            List<string> list = ledgerIds.ToList();
            if (!list.Any())
                return new List<LedgerEntry>(0);

            Dictionary<string, string> pms = new Dictionary<string, string>
            {
                ["id"] = Util.ToCommaSeparatedChecked(list, nameof(ledgerIds))
            };

            return ParseLedgerEntries(await QueryPrivateAsync<JObject>("QueryLedgers", 2, 0, pms));
        }

        public async Task<AddOrderResult> AddOrderAsync(OrderRequest order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrWhiteSpace(order.Pair))
                throw new ArgumentException("Must specify a pair", nameof(order));

            if (order.Kind == OrderKind.None)
                throw new ArgumentException("Must specify order kind", nameof(order));

            if (order.Type == OrderType.None)
                throw new ArgumentException("Must specify order type", nameof(order));

            if (order.StartTime.HasValue && order.RelativeStartTimeSeconds.HasValue)
            {
                throw new ArgumentException(
                    $"Must specify only one of {nameof(order.StartTime)} " +
                    $"and {nameof(order.RelativeStartTimeSeconds)}", nameof(order));
            }

            if (order.ExpireTime.HasValue && order.RelativeExpireTimeSeconds.HasValue)
            {
                throw new ArgumentException(
                    $"Must specify only one of {nameof(order.ExpireTime)} " +
                    $"and {nameof(order.RelativeExpireTimeSeconds)}", nameof(order));
            }

            Dictionary<string, string> pms = new Dictionary<string, string>
            {
                ["pair"] = order.Pair,
                ["type"] = order.Kind.ToApiString(),
                ["ordertype"] = order.Type.ToApiString(),
                ["volume"] = order.Volume.ToString(CultureInfo.InvariantCulture),
            };

            if (order.Price.HasValue)
                pms["price"] = order.Price.Value.ToApiString();
            if (order.Price2.HasValue)
                pms["price2"] = order.Price2.Value.ToApiString();
            if (order.Leverage != null)
                pms["leverage"] = order.Leverage;

            List<string> flags = new List<string>();
            if (order.VolumeInQuoteCurrency)
                flags.Add("viqc");
            if (order.PreferFeeInBaseCurrency)
                flags.Add("fcib");
            if (order.PreferFeeInQuoteCurrency)
                flags.Add("fciq");
            if (order.NoMarketPriceProtection)
                flags.Add("nompp");
            if (order.PostOnlyOrder)
                flags.Add("post");

            if (flags.Any())
                pms["oflags"] = string.Join(",", flags);

            if (order.StartTime.HasValue)
                pms["starttm"] = Util.ToUnixTime(order.StartTime.Value).ToString();
            if (order.RelativeStartTimeSeconds.HasValue)
                pms["starttm"] = $"+{order.RelativeStartTimeSeconds}";

            if (order.ExpireTime.HasValue)
                pms["expiretm"] = Util.ToUnixTime(order.ExpireTime.Value).ToString();
            if (order.RelativeExpireTimeSeconds.HasValue)
                pms["expiretm"] = $"+{order.RelativeExpireTimeSeconds}";

            if (order.UserReferenceId.HasValue)
                pms["userref"] = order.UserReferenceId.Value.ToString();
            if (order.ValidateOnly)
                pms["validate"] = "1";

            if (order.CloseOrderType.HasValue)
                pms["close[ordertype]"] = order.CloseOrderType.Value.ToApiString();
            if (order.CloseOrderPrice.HasValue)
                pms["close[price]"] = order.CloseOrderPrice.Value.ToApiString();
            if (order.CloseOrderPrice2.HasValue)
                pms["close[price2]"] = order.CloseOrderPrice2.Value.ToApiString();

            JObject result = await QueryPrivateAsync<JObject>("AddOrder", 0, 1, pms);
            JObject desc = result.Value<JObject>("descr");
            string orderDesc = desc.Value<string>("order");
            string closeOrderDesc = desc.Value<string>("close");
            JArray transactions = result.Value<JArray>("txid");
            List<string> txIds;
            if (transactions != null)
                txIds = transactions.Select(jt => jt.Value<string>()).ToList();
            else
                txIds = new List<string>();

            return new AddOrderResult(orderDesc, closeOrderDesc, txIds);
        }

        public async Task<(int count, bool pending)> CancelOrderAsync(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            Dictionary<string, string> pms = new Dictionary<string, string>
            {
                ["txid"] = id,
            };

            JObject result = await QueryPrivateAsync<JObject>("CancelOrder", 0, 1, pms);
            return (result.Value<int>("count"), result.Value<bool>("pending"));
        }

        public async Task<Dictionary<string, OrderInfo>> GetOpenOrdersAsync(bool includeTrades = false, int? userRefId = null)
        {
            Dictionary<string, string> pms = new Dictionary<string, string>();
            if (includeTrades)
                pms["trades"] = "1";
            if (userRefId.HasValue)
                pms["userref"] = userRefId.Value.ToString();

            JObject result = await QueryPrivateAsync<JObject>("OpenOrders", 1, 0, pms);
            JObject orders = result.Value<JObject>("open");
            return ParseOrders(orders);
        }

        public async Task<(int totalCount, Dictionary<string, OrderInfo> orders)> GetClosedOrdersAsync(ClosedOrdersQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (query.StartTime.HasValue && query.StartTransactionId != null)
                throw new ArgumentException($"Only one of {nameof(query.StartTime)} and {nameof(query.StartTransactionId)} can be specified", nameof(query));

            if (query.EndTime.HasValue && query.EndTransactionId != null)
                throw new ArgumentException($"Only one of {nameof(query.EndTime)} and {nameof(query.EndTransactionId)} can be specified", nameof(query));

            Dictionary<string, string> pms = new Dictionary<string, string>();
            if (query.IncludeTrades)
                pms["trades"] = "1";
            if (query.UserReferenceId.HasValue)
                pms["userref"] = query.UserReferenceId.Value.ToString();

            if (query.StartTime.HasValue)
                pms["start"] = Util.ToUnixTime(query.StartTime.Value).ToString();
            if (query.StartTransactionId != null)
                pms["start"] = query.StartTransactionId;

            if (query.EndTime.HasValue)
                pms["end"] = Util.ToUnixTime(query.EndTime.Value).ToString();
            if (query.EndTransactionId != null)
                pms["end"] = query.EndTransactionId;

            if (query.CloseTime.HasValue)
                pms["closetime"] = query.CloseTime.Value.ToApiString();

            pms["ofs"] = query.Offset.ToString();

            JObject result = await QueryPrivateAsync<JObject>("ClosedOrders", 1, 0, pms);
            Dictionary<string, OrderInfo> orders = ParseOrders(result.Value<JObject>("closed"));
            return (result.Value<int>("count"), orders);
        }

        public async Task<Dictionary<string, OrderInfo>> QueryOrdersAsync(
            IEnumerable<string> transactionIds, bool includeTrades = false, int? userRefId = null)
        {
            Dictionary<string, string> pms = new Dictionary<string, string>
            {
                ["txid"] = Util.ToCommaSeparatedChecked(transactionIds, nameof(transactionIds))
            };
            if (includeTrades)
                pms["trades"] = "1";
            if (userRefId.HasValue)
                pms["userref"] = userRefId.Value.ToString();

            JObject result = await QueryPrivateAsync<JObject>("QueryOrders", 1, 0, pms);
            return ParseOrders(result);
        }

        private Dictionary<string, OrderInfo> ParseOrders(JObject obj)
        {
            Dictionary<string, OrderInfo> dict = new Dictionary<string, OrderInfo>();
            foreach (KeyValuePair<string, JToken> kvp in obj)
            {
                JToken value = kvp.Value;
                JToken descr = value.Value<JToken>("descr");
                double startTime = value.Value<double>("starttm");
                double expireTime = value.Value<double>("expiretm");
                string misc = value.Value<string>("misc");
                string oflags = value.Value<string>("oflags");
                JArray trades = value.Value<JArray>("trades");
                double closeTime = value.Value<double>("closetm");

                OrderInfo order = new OrderInfo(
                    transactionId: kvp.Key,
                    referralId: value.Value<string>("refid"),
                    userReferenceId: value.Value<int?>("userref"),
                    status: OrderStatusExtensions.FromApiString(value.Value<string>("status")),
                    openTime: Util.FromUnixTime(value.Value<double>("opentm")),
                    startTime: startTime == 0 ? null : (DateTimeOffset?)Util.FromUnixTime(startTime),
                    expireTime: expireTime == 0 ? null : (DateTimeOffset?)Util.FromUnixTime(expireTime),
                    pair: descr.Value<string>("pair"),
                    kind: OrderKindExtensions.FromApiString(descr.Value<string>("type")),
                    type: OrderTypeExtensions.FromApiString(descr.Value<string>("ordertype")),
                    price: descr.Value<decimal>("price"),
                    price2: descr.Value<decimal>("price2"),
                    leverage: descr.Value<string>("leverage"),
                    description: descr.Value<string>("order"),
                    closeDescription: descr.Value<string>("close"),
                    volume: value.Value<decimal>("vol"),
                    volumeExecuted: value.Value<decimal>("vol_exec"),
                    cost: value.Value<decimal>("cost"),
                    fee: value.Value<decimal>("fee"),
                    averagePrice: value.Value<decimal>("price"),
                    stopPrice: value.Value<decimal>("stopprice"),
                    limitPrice: value.Value<decimal>("limitprice"),
                    stopped: misc.Contains("stopped"),
                    touched: misc.Contains("touched"),
                    liquidated: misc.Contains("liquidated"),
                    partial: misc.Contains("partial"),
                    volumeInQuoteCurrency: oflags.Contains("viqc"),
                    preferFeeInBaseCurrency: oflags.Contains("fcib"),
                    preferFeeInQuoteCurrency: oflags.Contains("fciq"),
                    noMarketPriceProtection: oflags.Contains("nompp"),
                    tradeIds: trades == null ? new List<string>(0) : trades.ToObject<List<string>>(),
                    closeTime: closeTime == 0 ? null : (DateTimeOffset?)Util.FromUnixTime(closeTime),
                    closeReason: value.Value<string>("reason"));

                dict.Add(kvp.Key, order);
            }

            return dict;
        }

        private async Task<T> QueryPublicAsync<T>(string api, Dictionary<string, string> parameters = null)
        {
            string parametersEncoded = "";
            if (parameters != null && parameters.Count > 0)
            {
                IEnumerable<string> paramValues =
                    parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");

                parametersEncoded = "?" + string.Join("&", paramValues);
            }

            HttpResponseMessage resp = await _client.GetAsync($"public/{api}{parametersEncoded}");
            return await ReadResponse<T>(resp);
        }

        private async Task<T> ReadResponse<T>(HttpResponseMessage resp)
        {
            string asString = await resp.Content.ReadAsStringAsync();
            KrakenResponse<T> deserialized = JsonConvert.DeserializeObject<KrakenResponse<T>>(asString);

            if (deserialized.Error.Count > 0)
            {
                List<KrakenDiagnostic> diagnostics = deserialized.Error.Select(ParseDiagnostic).ToList();
                if (!IgnoreWarnings || diagnostics.Any(kd => kd.Severity == DiagnosticSeverity.Error))
                    throw KrakenException.FromDiagnostics(diagnostics);
            }

            return deserialized.Result;
        }

        private async Task<T> QueryPrivateAsync<T>(
            string api, int privateRateCount, int orderRateCount,
            Dictionary<string, string> parameters = null)
        {
            if (PrivateApiRateGate != null)
            {
                for (int i = 0; i < privateRateCount; i++)
                    await PrivateApiRateGate.WaitForLimitAsync();
            }

            if (OrderApiRateGate != null)
            {
                for (int i = 0; i < orderRateCount; i++)
                    await OrderApiRateGate.WaitForLimitAsync();
            }

            Dictionary<string, string> finalParams;
            if (parameters != null)
                finalParams = new Dictionary<string, string>(parameters);
            else
                finalParams = new Dictionary<string, string>();

            long nonce = s_baseTime.Ticks + s_timer.ElapsedTicks;
            finalParams.Add("nonce", nonce.ToString());
            if (_otpGenerator != null)
                finalParams.Add("otp", await _otpGenerator());

            FormUrlEncodedContent content = new FormUrlEncodedContent(finalParams);
            string postData = await content.ReadAsStringAsync();

            string GenerateSignature()
            {
                string localPath = new Uri(_client.BaseAddress, $"private/{api}").LocalPath;
                byte[] hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(nonce + postData));
                }

                byte[] signature;
                using (HMACSHA512 signer = new HMACSHA512(_apiSecret))
                {
                    signature = signer.ComputeHash(Encoding.UTF8.GetBytes(localPath).Concat(hash).ToArray());
                }

                return Convert.ToBase64String(signature);
            }

            content.Headers.Add("API-Key", _apiKey);
            content.Headers.Add("API-Sign", GenerateSignature());
            HttpResponseMessage resp = await _client.PostAsync($"private/{api}", content);
            return await ReadResponse<T>(resp);
        }

        private KrakenDiagnostic ParseDiagnostic(string diagnostic)
        {
            string[] split = diagnostic.Split(':');
            Debug.Assert(split.Length == 2 || split.Length == 3);
            DiagnosticSeverity severity = split[0][0] == 'E' ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
            string category = split[0].Substring(1);
            string type = split[1];
            string extraInfo = split.Length == 3 ? split[2] : "";

            return new KrakenDiagnostic(severity, category, type, extraInfo);
        }

        private class KrakenResponse<T>
        {
            public KrakenResponse(
                List<string> error,
                T result)
            {
                Error = error;
                Result = result;
            }

            public List<string> Error { get; }
            public T Result { get; }
        }
    }
}
