using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace KrakenNet
{
    public class KrakenException : Exception
    {
        private KrakenDiagnostic krakenDiagnostic;

        public KrakenException()
        {
        }

        public KrakenException(string message) : base(message)
        {
        }

        public KrakenException(KrakenDiagnostic krakenDiagnostic)
        {
            this.krakenDiagnostic = krakenDiagnostic;
        }

        public KrakenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public KrakenException(string message, Exception innerException, IReadOnlyList<KrakenDiagnostic> diagnostics) : base(message, innerException)
        {
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<KrakenDiagnostic> Diagnostics { get; } = Array.Empty<KrakenDiagnostic>();

        internal static KrakenException FromDiagnostics(IReadOnlyList<KrakenDiagnostic> diagnostics)
        {
            if (diagnostics.Count == 1)
                return new KrakenException(diagnostics[0].ToString(), null, diagnostics);

            int numErrors = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
            int numWarnings = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
            Debug.Assert(numErrors + numWarnings > 0);

            StringBuilder message = new StringBuilder();
            if (numErrors > 0 && numWarnings > 0)
                message.AppendFormat("{0} errors and {1} warnings");
            else if (numErrors > 0)
                message.AppendFormat("{0} errors", numErrors);
            else
                message.AppendFormat("{0} warnings", numWarnings);

            foreach (KrakenDiagnostic diagnostic in diagnostics)
            {
                message.AppendLine();
                message.AppendFormat("  {0}", diagnostic);
            }

            return new KrakenException(message.ToString(), null, diagnostics);
        }
    }
}
