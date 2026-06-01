using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrackHub.Application.Services;

namespace TrackHub.Api.Controllers;

[ApiController]
[Route("api/merchants/{merchantId:decimal}/shipments")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ShipmentReportService _reports;
    private readonly EventIngestionService _ingest;

    public TrackingController(ShipmentReportService reports, EventIngestionService ingest)
    {
        _reports = reports;
        _ingest = ingest;
    }

    /// <summary>Merchant dashboard: one row per shipment with event counts.</summary>
    [HttpGet]
    public IActionResult GetSummary(decimal merchantId)
    {
        var rows = _reports.GetSummaryForMerchant(merchantId);
        return Ok(rows);
    }

    /// <summary>
    /// Webhook endpoint a carrier POSTs to. The {carrierCode} segment tells us
    /// which connector to use for the payload.
    /// </summary>
    // Carriers don't present JWTs to us; they're external systems POSTing on
    // their own schedules. The controller's class-level [Authorize] doesn't
    // apply to this entry point.
    [HttpPost("/api/carriers/{carrierCode}/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(string carrierCode)
    {
        await _ingest.IngestAsync(carrierCode, Request.Body);
        return Accepted();
    }
}
