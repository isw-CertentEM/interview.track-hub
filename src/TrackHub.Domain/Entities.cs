namespace TrackHub.Domain;

public class Merchant
{
    public decimal MerchantId { get; set; }
    public string Name { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class Carrier
{
    public decimal CarrierId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class Shipment
{
    public decimal ShipmentId { get; set; }
    public decimal MerchantId { get; set; }
    public decimal CarrierId { get; set; }
    public string TrackingNumber { get; set; } = default!;
    public string CurrentStatus { get; set; } = default!;
    public string? CurrentLocationCode { get; set; }
    public DateTime? LastEventAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Carrier Carrier { get; set; } = default!;
    public ICollection<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
}

public class TrackingEvent
{
    public decimal EventId { get; set; }
    public decimal ShipmentId { get; set; }
    public string CarrierEventId { get; set; } = default!;
    public DateTime EventTime { get; set; }
    public string StatusCode { get; set; } = default!;
    public string? LocationCode { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public class EventBatch
{
    public decimal BatchId { get; set; }
    public decimal CarrierId { get; set; }
    public string? SourceFileHash { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int? RowCount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>One parsed tracking event from a carrier feed, before persistence.</summary>
public record ParsedEvent(
    string TrackingNumber,
    string CarrierEventId,
    DateTime EventTime,
    string StatusCode,
    string? LocationCode);
