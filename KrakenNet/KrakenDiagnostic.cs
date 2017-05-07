using System.Text;

namespace KrakenNet
{
    public class KrakenDiagnostic
    {
        public KrakenDiagnostic(
            DiagnosticSeverity severity,
            string category,
            string type,
            string extraInfo)
        {
            Severity = severity;
            Category = category;
            Type = type;
            ExtraInfo = extraInfo;
        }

        public DiagnosticSeverity Severity { get; }
        public string Category { get; }
        public string Type { get; }
        public string ExtraInfo { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}] {1}: {2}", Category, Severity, Type);

            if (!string.IsNullOrWhiteSpace(ExtraInfo))
                sb.AppendFormat(" ({0})", ExtraInfo);

            return sb.ToString();
        }
    }

    public enum DiagnosticSeverity
    {
        Warning,
        Error,
    }
}
