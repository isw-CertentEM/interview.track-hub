using TrackHub.Domain;

namespace TrackHub.Application.Connectors;

/// <summary>
/// A connector knows how to read events from one carrier's feed format.
/// Implementations: CSV over SFTP, JSON webhook, polling REST.
/// </summary>
public interface ICarrierConnector
{
    string CarrierCode { get; }

    IEnumerable<ParsedEvent> Parse(Stream source);
}
