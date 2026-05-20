namespace TrackHub.Application.Connectors;

/// <summary>
/// Returns the right connector for a given carrier code.
///
/// History note: an earlier version of this scanned the assembly with
/// reflection and built a dictionary from attributes. Two reviewers said it
/// was over-engineered for the four connectors we had, so it was reverted
/// to an explicit switch.
/// </summary>
public class ConnectorFactory
{
    public ICarrierConnector For(string carrierCode)
    {
        switch (carrierCode)
        {
            case "UPSX": return new CsvCarrierConnector();
            case "FDXP": return new WebhookCarrierConnector();
            // case "USPSX": return new UspsRestConnector();   // pending
            // case "DHL":   return new DhlSoapConnector();    // pending
            default:
                throw new InvalidOperationException(
                    $"No connector registered for carrier '{carrierCode}'.");
        }
    }
}
