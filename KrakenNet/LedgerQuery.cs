using System;
using System.Collections.Generic;

namespace KrakenNet
{
    public class LedgerQuery
    {
        /// <summary>
        /// The assets to restrict the query to. If empty or <c>null</c> then all assets are queried.
        /// </summary>
        public List<string> Assets { get; set; } = new List<string>();
        /// <summary>
        /// The type of ledger entries to return. If <c>null</c>, returns all types.
        /// </summary>
        public LedgerType? Type { get; set; }
        /// <summary>
        /// The start time to query, or <c>null</c> for no start time.
        /// Only one of <see cref="StartTime"/> and <see cref="StartLedgerId"/> should be set.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }
        /// <summary>
        /// The end time to query, or <c>null</c> for no end time.
        /// Only one of <see cref="EndTime"/> and <see cref="EndLedgerId"/> should be set.
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }
        /// <summary>
        /// The ID of the first ledger entry to include, or <c>null</c> for unrestricted.
        /// Only one of <see cref="StartTime"/> and <see cref="StartLedgerId"/> should be set.
        /// </summary>
        public string StartLedgerId { get; set; }
        /// <summary>
        /// The ID of the last ledger entry to include, or <c>null</c> for unrestricted.
        /// Only one of <see cref="EndTime"/> and <see cref="EndLedgerId"/> should be set.
        /// </summary>
        public string EndLedgerId { get; set; }
        /// <summary>
        /// The offset to start returning results from.
        /// </summary>
        public int Offset { get; set; }
    }
}
