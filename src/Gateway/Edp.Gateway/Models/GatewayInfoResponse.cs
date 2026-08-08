namespace Edp.Gateway.Models;

public sealed record GatewayInfoResponse(
    string ServiceName,
    string Version,
    string Environment,
    string CorrelationId);
