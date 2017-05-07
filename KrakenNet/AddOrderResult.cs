using System.Collections.Generic;

namespace KrakenNet
{
    public class AddOrderResult
    {
        public AddOrderResult(
            string orderDescription,
            string closeOrderDescription,
            IReadOnlyList<string> transactionIds)
        {
            OrderDescription = orderDescription;
            CloseOrderDescription = closeOrderDescription;
            TransactionIds = transactionIds;
        }

        public string OrderDescription { get; }
        public string CloseOrderDescription { get; }
        public IReadOnlyList<string> TransactionIds { get; }
    }
}
