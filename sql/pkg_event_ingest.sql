-- TrackHub: PL/SQL package for ingesting tracking events.
-- Called from C# (EventIngestionService) once per parsed event.

CREATE OR REPLACE PACKAGE pkg_event_ingest AS

    -- Called by the C# ingestion service once per parsed tracking event.
    -- Records the event if new, then rolls the shipment's current status
    -- forward to reflect it.
    PROCEDURE record_event(
        p_carrier_id       IN  carriers.carrier_id%TYPE,
        p_tracking_number  IN  shipments.tracking_number%TYPE,
        p_carrier_event_id IN  tracking_events.carrier_event_id%TYPE,
        p_event_time       IN  TIMESTAMP,
        p_status_code      IN  VARCHAR2,
        p_location_code    IN  VARCHAR2,
        p_result           OUT VARCHAR2
    );

    -- Called by the C# ingestion service when a batch fails partway through.
    -- Durably records the failure on the batch row so operations can see it.
    PROCEDURE mark_batch_failed(
        p_batch_id      IN event_batches.batch_id%TYPE,
        p_error_message IN VARCHAR2
    );

END pkg_event_ingest;
/

CREATE OR REPLACE PACKAGE BODY pkg_event_ingest AS

    -- Called by the C# ingestion service once per parsed tracking event.
    -- Records the event if new, then rolls the shipment's current status
    -- forward to reflect it.
    PROCEDURE record_event(
        p_carrier_id       IN  carriers.carrier_id%TYPE,
        p_tracking_number  IN  shipments.tracking_number%TYPE,
        p_carrier_event_id IN  tracking_events.carrier_event_id%TYPE,
        p_event_time       IN  TIMESTAMP,
        p_status_code      IN  VARCHAR2,
        p_location_code    IN  VARCHAR2,
        p_result           OUT VARCHAR2
    ) AS
        v_shipment_id   shipments.shipment_id%TYPE;
        v_existing_evts NUMBER;
    BEGIN
        -- Look up the shipment for this (carrier, tracking_number) pair
        SELECT shipment_id
          INTO v_shipment_id
          FROM shipments
         WHERE carrier_id      = p_carrier_id
           AND tracking_number = p_tracking_number;

        -- Has this carrier event been recorded before?
        SELECT COUNT(*)
          INTO v_existing_evts
          FROM tracking_events
         WHERE shipment_id      = v_shipment_id
           AND carrier_event_id = p_carrier_event_id;

        IF v_existing_evts = 0 THEN
            INSERT INTO tracking_events
                (event_id, shipment_id, carrier_event_id,
                 event_time, status_code, location_code)
            VALUES
                (tracking_event_seq.NEXTVAL, v_shipment_id, p_carrier_event_id,
                 p_event_time, p_status_code, p_location_code);
        END IF;

        -- Roll the latest status forward onto the shipment row
        UPDATE shipments
           SET current_status        = p_status_code,
               current_location_code = p_location_code,
               last_event_at         = p_event_time
         WHERE shipment_id = v_shipment_id;

        COMMIT;
        p_result := 'OK';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_result := 'ERROR: ' || SQLERRM;
    END record_event;


    -- Called by the C# ingestion service when a batch fails partway through.
    -- Durably records the failure on the batch row so operations can see it.
    PROCEDURE mark_batch_failed(
        p_batch_id      IN event_batches.batch_id%TYPE,
        p_error_message IN VARCHAR2
    ) AS
        v_processed_at event_batches.processed_at%TYPE;
    BEGIN
        -- Lock the batch row so a concurrent finaliser can't race us.
        SELECT processed_at
          INTO v_processed_at
          FROM event_batches
         WHERE batch_id = p_batch_id
           FOR UPDATE;

        IF v_processed_at IS NOT NULL THEN
            RAISE_APPLICATION_ERROR(
                -20201,
                'Batch ' || p_batch_id || ' is already finalised; cannot mark as failed.');
        END IF;

        UPDATE event_batches
           SET processed_at  = SYSTIMESTAMP,
               error_message = SUBSTR(p_error_message, 1, 4000)
         WHERE batch_id = p_batch_id;

        -- Transaction control belongs to the caller.
    END mark_batch_failed;

END pkg_event_ingest;
/
