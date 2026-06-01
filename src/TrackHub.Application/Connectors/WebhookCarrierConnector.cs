using System.Text.Json;
using TrackHub.Domain;

namespace TrackHub.Application.Connectors;

/// <summary>
/// Parses the JSON envelope POSTed by Carrier "FDXP" on their webhook.
/// One envelope contains a single event.
/// </summary>
public class WebhookCarrierConnector : ICarrierConnector
{
    public string CarrierCode => "FDXP";

    public IEnumerable<ParsedEvent> Parse(Stream source)
    {
        using var doc = JsonDocument.Parse(source);
        var root = doc.RootElement;

        yield return new ParsedEvent(
            TrackingNumber: root.GetProperty("tracking").GetString()!,
            CarrierEventId: root.GetProperty("eventId").GetString()!,
            EventTime:      root.GetProperty("ts").GetDateTime(),
            StatusCode:     root.GetProperty("status").GetString()!,
            LocationCode:   root.TryGetProperty("loc", out var l) ? l.GetString() : null);
    }
}
