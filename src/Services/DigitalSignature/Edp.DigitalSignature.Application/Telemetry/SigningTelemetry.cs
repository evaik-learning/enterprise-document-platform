namespace Edp.DigitalSignature.Application.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public static class SigningTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Edp.DigitalSignature");
    public static readonly Meter Meter = new("Edp.DigitalSignature", "1.0.0");
    public static readonly Counter<long> RequestsCreated = Meter.CreateCounter<long>("signing_requests_created_total");
    public static readonly Counter<long> RequestsCompleted = Meter.CreateCounter<long>("signing_requests_completed_total");
    public static readonly Counter<long> RequestsExpired = Meter.CreateCounter<long>("signing_requests_expired_total");
    public static readonly Histogram<double> ProviderLatency = Meter.CreateHistogram<double>("signing_provider_latency_ms", "ms");
}