using System;
using System.Collections.Generic;
using System.Text;

namespace KrakenNet
{
    internal static class Util
    {
        private static readonly DateTimeOffset s_unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static DateTimeOffset FromUnixTime(double timestamp)
        {
            return s_unixEpoch.AddSeconds(timestamp);
        }

        public static long ToUnixTime(DateTimeOffset dt)
        {
            return (long)(dt - s_unixEpoch).TotalSeconds;
        }

        public static string ToCommaSeparatedChecked(IEnumerable<string> vals, string paramName)
        {
            if (vals == null)
                throw new ArgumentNullException(paramName);

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (string val in vals)
            {
                if (val.Contains(","))
                    throw new ArgumentException($"Value '{val}' contains ',' character", paramName);

                if (!first)
                    sb.Append(",");

                sb.Append(val);
                first = false;
            }

            if (sb.Length <= 0)
                throw new ArgumentException("Must specify at least one value", paramName);

            return sb.ToString();
        }
    }
}
