using Microsoft.EntityFrameworkCore;
using TrackHub.Infrastructure;

namespace TrackHub.Application.Services;

public record ShipmentSummaryDto(
    decimal ShipmentId,
    string  TrackingNumber,
    string  CarrierCode,
    string  CurrentStatus,
    int     EventCount,
    DateTime? LastEventAt);

public class ShipmentReportService
{
    private readonly TrackHubDbContext _db;

    public ShipmentReportService(TrackHubDbContext db) { _db = db; }

    /// <summary>
    /// Returns one row per shipment for the given merchant, with a count of
    /// tracking events recorded for each shipment. Used by the merchant
    /// dashboard.
    /// </summary>
    public List<ShipmentSummaryDto> GetSummaryForMerchant(decimal merchantId)
    {
        var shipments = _db.Shipments
            .Where(s => s.MerchantId == merchantId)
            .ToList();

        var result = new List<ShipmentSummaryDto>();
        foreach (var s in shipments)
        {
            var carrier = _db.Carriers.First(c => c.CarrierId == s.CarrierId);
            var eventCount = _db.TrackingEvents.Count(e => e.ShipmentId == s.ShipmentId);

            result.Add(new ShipmentSummaryDto(
                s.ShipmentId, s.TrackingNumber, carrier.Code,
                s.CurrentStatus, eventCount, s.LastEventAt));
        }
        return result;
    }
}
