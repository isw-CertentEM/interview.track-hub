-- TrackHub Oracle schema (excerpt)
-- Owner: TRACKHUB

CREATE SEQUENCE shipment_seq START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE tracking_event_seq START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE SEQUENCE event_batch_seq START WITH 1 INCREMENT BY 1 NOCACHE;

CREATE TABLE merchants (
    merchant_id NUMBER         PRIMARY KEY,
    name        VARCHAR2(200)  NOT NULL,
    api_key     VARCHAR2(64)   NOT NULL UNIQUE,
    is_active   NUMBER(1)      DEFAULT 1 NOT NULL
);

CREATE TABLE carriers (
    carrier_id NUMBER         PRIMARY KEY,
    code       VARCHAR2(10)   NOT NULL UNIQUE,   -- 'UPS', 'FEDEX', ...
    name       VARCHAR2(200)  NOT NULL
);

CREATE TABLE shipments (
    shipment_id           NUMBER         PRIMARY KEY,
    merchant_id           NUMBER         NOT NULL,
    carrier_id            NUMBER         NOT NULL,
    tracking_number       VARCHAR2(40)   NOT NULL,
    current_status        VARCHAR2(20)   NOT NULL,   -- CREATED|IN_TRANSIT|DELIVERED|EXCEPTION
    current_location_code VARCHAR2(20),
    last_event_at         TIMESTAMP,
    created_at            TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
    CONSTRAINT fk_shipments_merchant FOREIGN KEY (merchant_id) REFERENCES merchants(merchant_id),
    CONSTRAINT fk_shipments_carrier  FOREIGN KEY (carrier_id)  REFERENCES carriers(carrier_id),
    CONSTRAINT uq_shipments_carrier_tracking UNIQUE (carrier_id, tracking_number)
);

CREATE INDEX ix_shipments_merchant_status ON shipments(merchant_id, current_status);

CREATE TABLE tracking_events (
    event_id         NUMBER         PRIMARY KEY,
    shipment_id      NUMBER         NOT NULL,
    carrier_event_id VARCHAR2(80)   NOT NULL,  -- the carrier's own ID for this event
    event_time       TIMESTAMP      NOT NULL,
    status_code      VARCHAR2(20)   NOT NULL,
    location_code    VARCHAR2(20),
    received_at      TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
    CONSTRAINT fk_events_shipment FOREIGN KEY (shipment_id) REFERENCES shipments(shipment_id)
    -- NOTE: no unique constraint on (shipment_id, carrier_event_id)
);

CREATE INDEX ix_events_shipment_time ON tracking_events(shipment_id, event_time);

CREATE TABLE event_batches (
    batch_id         NUMBER         PRIMARY KEY,
    carrier_id       NUMBER         NOT NULL,
    source_file_hash VARCHAR2(64),
    received_at      TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
    processed_at     TIMESTAMP,
    row_count        NUMBER,
    error_message    VARCHAR2(4000)
);

-- Pending downstream notifications for each ingested event. The ingestion
-- path is supposed to write one row here per accepted event; a separate
-- dispatcher picks them up and POSTs to the merchant webhook.
CREATE SEQUENCE event_outbox_seq START WITH 1 INCREMENT BY 1 NOCACHE;

CREATE TABLE event_outbox (
    outbox_id     NUMBER         PRIMARY KEY,
    event_id      NUMBER         NOT NULL,
    shipment_id   NUMBER         NOT NULL,
    destination   VARCHAR2(200)  NOT NULL,
    payload       CLOB,
    status        VARCHAR2(20)   DEFAULT 'PENDING' NOT NULL,
    created_at    TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
    dispatched_at TIMESTAMP,
    CONSTRAINT fk_outbox_event    FOREIGN KEY (event_id)    REFERENCES tracking_events(event_id),
    CONSTRAINT fk_outbox_shipment FOREIGN KEY (shipment_id) REFERENCES shipments(shipment_id)
);

CREATE INDEX ix_outbox_status_created ON event_outbox(status, created_at);
