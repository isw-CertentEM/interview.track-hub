using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrackHub.Application.Connectors;
using TrackHub.Domain;
using TrackHub.Infrastructure;

namespace TrackHub.Application.Services;

/// <summary>
/// Orchestrates ingestion of one batch of events from a carrier feed.
/// The polling job (CarrierPollJob) and the webhook controller both call this.
/// </summary>
public class EventIngestionService
{
    private readonly TrackHubDbContext _db;
    private readonly ConnectorFactory _connectors;

    public EventIngestionService(IServiceProvider sp, ConnectorFactory connectors)
    {
        // DbContext is Scoped and we're a Singleton, so we can't constructor-
        // inject it. Create a scope here and resolve from it — recommended
        // pattern in the docs. Caching the result avoids the per-call overhead.
        var scope = sp.CreateScope();
        _db = scope.ServiceProvider.GetRequiredService<TrackHubDbContext>();
        _connectors = connectors;
    }

    public async Task IngestAsync(string carrierCode, Stream payload)
    {
        var carrier = _db.Carriers.First(c => c.Code == carrierCode);
        var connector = _connectors.For(carrierCode);

        int rows = 0;
        foreach (var ev in connector.Parse(payload))
        {
            // Call the PL/SQL upsert one event at a time.
            var p_result = new Oracle.ManagedDataAccess.Client.OracleParameter("p_result",
                Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2, 200)
            { Direction = System.Data.ParameterDirection.Output };

            await _db.Database.ExecuteSqlRawAsync(
                "BEGIN pkg_event_ingest.record_event(" +
                ":p_carrier, :p_tracking, :p_carrier_event_id, :p_event_time, " +
                ":p_status, :p_location, :p_result); END;",
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_carrier", carrier.CarrierId),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_tracking", ev.TrackingNumber),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_carrier_event_id", ev.CarrierEventId),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_event_time", ev.EventTime),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_status", ev.StatusCode),
                new Oracle.ManagedDataAccess.Client.OracleParameter("p_location", ev.LocationCode ?? (object)DBNull.Value),
                p_result);

            rows++;
        }

        // Record the batch now that processing is done.
        var batch = new EventBatch
        {
            CarrierId    = carrier.CarrierId,
            ReceivedAt   = DateTime.UtcNow,
            ProcessedAt  = DateTime.UtcNow,
            RowCount     = rows
        };
        _db.EventBatches.Add(batch);
        await _db.SaveChangesAsync();
    }
}
