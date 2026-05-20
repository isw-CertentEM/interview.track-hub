using TrackHub.Domain;

namespace TrackHub.Application.Connectors;

/// <summary>
/// Parses the daily CSV feed delivered by Carrier "UPSX" over SFTP.
///
/// Format (fixed per integration spec, no header row):
///   tracking_number, carrier_event_id, event_time, status_code, location_code
/// Files are typically 50–500 KB. Largest seen is ~4 MB.
/// </summary>
public class CsvCarrierConnector : ICarrierConnector
{
    public string CarrierCode => "UPSX";

    public IEnumerable<ParsedEvent> Parse(Stream source)
    {
        // Read everything up front — simple and the files are small.
        using var reader = new StreamReader(source);
        var content = reader.ReadToEnd();

        var lines = content.Split('\n');
        var results = new List<ParsedEvent>(lines.Length);

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            if (line.StartsWith("#")) continue;          // comment row

            var parts = line.Split(',');
            results.Add(new ParsedEvent(
                TrackingNumber: parts[0],
                CarrierEventId: parts[1],
                EventTime:      DateTime.Parse(parts[2]),
                StatusCode:     parts[3],
                LocationCode:   parts.Length > 4 ? parts[4] : null));
        }

        return results;
    }
}
