-- TrackHub: reporting query used by ShipmentReportService.GetDailyExceptionsAsync.
-- Returns, for the given merchant, every shipment that produced an EXCEPTION
-- event yesterday, with the latest event details.

SELECT s.shipment_id,
       s.tracking_number,
       s.current_status,
       e.event_time   AS exception_at,
       e.location_code,
       UPPER(c.code)  AS carrier_code
  FROM shipments       s
  JOIN tracking_events e ON e.shipment_id = s.shipment_id
  JOIN carriers        c ON c.carrier_id  = s.carrier_id
 WHERE s.merchant_id = :p_merchant_id
   AND e.status_code = 'EXCEPTION'
   AND TRUNC(e.received_at) = TRUNC(SYSDATE - 1)
 ORDER BY e.event_time DESC;
