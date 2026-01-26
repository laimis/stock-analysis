CREATE DATABASE stockanalysis;
create user ___ with encrypted password ___;
grant all privileges on database stockanalysis to stockanalysis;

CREATE TABLE analysis (
	ticker TEXT,
	created TIMESTAMP,
	lastprice DECIMAL,
	lastbookvalue DECIMAL,
	lastpevalue DECIMAL,
	industry TEXT,
	PRIMARY KEY(ticker)
);
ALTER TABLE analysis OWNER TO stocks;

CREATE TABLE metrics (
	ticker TEXT,
	created TIMESTAMP,
	json TEXT,
	PRIMARY KEY(ticker)
);
ALTER TABLE metrics OWNER TO stocks;

CREATE TABLE companies (
	ticker TEXT,
	created TIMESTAMP,
	name TEXT,
	industry TEXT,
	sector TEXT,
	json TEXT,
	PRIMARY KEY(ticker)
);
ALTER TABLE companies OWNER TO stocks;

CREATE TABLE prices (
	ticker TEXT,
	created TIMESTAMP,
	lastclose DECIMAL,
	json TEXT,
	PRIMARY KEY(ticker)
);
ALTER TABLE prices OWNER TO stocks;


CREATE TABLE events (
	key TEXT,
    entity TEXT,
	userid uuid,
	created TIMESTAMP,
	version INT,
	eventjson TEXT,
	PRIMARY KEY(entity, key, userid, version)
);
ALTER TABLE events OWNER TO stocks;

CREATE TABLE loginlog (
    username TEXT,
    date TIMESTAMP
);
ALTER TABLE loginlog OWNER TO stocks;

CREATE TABLE users (
    id TEXT,
    email TEXT,
    PRIMARY KEY (id) 
);
ALTER TABLE users OWNER TO stocks;

CREATE UNIQUE INDEX users_id ON users USING BTREE(id);
CREATE UNIQUE INDEX users_email ON users USING BTREE(email);


CREATE TABLE processidtouserassociations (
    id uuid,
    userid uuid,
    timestamp text,
    PRIMARY KEY (id) 
);
ALTER TABLE processidtouserassociations OWNER TO stocks;

CREATE UNIQUE INDEX processidtouserassociations_id ON processidtouserassociations USING BTREE(id);

CREATE TABLE blobs (
	key text,
	blob text,
	inserted timestamp,
	PRIMARY KEY (key) 
);

ALTER TABLE events ADD COLUMN AggregateId TEXT;

UPDATE events SET aggregateid = eventjson::json->>'AggregateId'
WHERE events.AggregateId IS NULL;

ALTER TABLE events ALTER COLUMN AggregateId SET NOT NULL;

CREATE UNIQUE INDEX events_aggregateid_unique 
    ON events USING BTREE(aggregateid,userid,version);

CREATE TABLE accountbalancessnapshots (
    userid uuid,
    cash decimal,
    equity decimal,
    longvalue decimal,
    shortvalue decimal,
    date text,
    PRIMARY KEY (userid,date) 
);
ALTER TABLE accountbalancessnapshots OWNER TO stocks;

CREATE TABLE accountbrokerageorders (
    orderid text NOT NULL,
    price decimal NOT NULL,
    quantity decimal NOT NULL,
    status text NOT NULL,
    ticker text NOT NULL,
    ordertype text NOT NULL,
    instruction text NOT NULL,
    assettype text NOT NULL,
    executiontime text NULL,
    enteredtime text NOT NULL,
    expirationtime text NULL,
    canbecancelled boolean,
    userid uuid NOT NULL,
    modified timestamptz NOT NULL,
    PRIMARY KEY (userid,orderid) 
);
ALTER TABLE accountbrokerageorders OWNER TO stocks;

CREATE TABLE accountbrokeragetransactions (
    transactionid text NOT NULL,
    description text NOT NULL,
    brokeragetype text NOT NULL,
    tradedate text NOT NULL,
    settlementdate text NOT NULL,
    netamount decimal NOT NULL,
    inferredticker text NULL,
    inferredtype text NULL,
    userid uuid NOT NULL,
    inserted text NOT NULL,
    applied text NULL,
    PRIMARY KEY (userid, transactionid)
);
ALTER TABLE accountbrokeragetransactions OWNER TO stocks;

CREATE TABLE optionpricings (
    id SERIAL PRIMARY KEY,
    userid uuid NOT NULL,
    optionpositionid uuid NOT NULL,
    underlyingticker TEXT NOT NULL,
    symbol TEXT NOT NULL,
    expiration TEXT NOT NULL,
    strikeprice DECIMAL NOT NULL,
    optiontype TEXT NOT NULL,
    volume BIGINT NOT NULL,
    openinterest BIGINT NOT NULL,
    bid DECIMAL NOT NULL,
    ask DECIMAL NOT NULL,
    last DECIMAL NOT NULL,
    mark DECIMAL NOT NULL,
    volatility DECIMAL NOT NULL,
    delta DECIMAL NOT NULL,
    gamma DECIMAL NOT NULL,
    theta DECIMAL NOT NULL,
    vega DECIMAL NOT NULL,
    rho DECIMAL NOT NULL,
    underlyingprice DECIMAL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);
CREATE INDEX optionpricings_userid ON optionpricings(userid);
CREATE INDEX optionpricings_optionpositionid ON optionpricings(optionpositionid);
CREATE INDEX optionpricings_symbol ON optionpricings(symbol);
ALTER TABLE optionpricings OWNER TO stocks;

CREATE TABLE stockpricealerts (
    alertid uuid PRIMARY KEY,
    userid uuid NOT NULL,
    ticker TEXT NOT NULL,
    pricelevel DECIMAL NOT NULL,
    alerttype TEXT NOT NULL,
    note TEXT NOT NULL,
    state TEXT NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL,
    triggeredat TIMESTAMP WITH TIME ZONE,
    lastresetat TIMESTAMP WITH TIME ZONE
);
CREATE INDEX stockpricealerts_userid ON stockpricealerts(userid);
ALTER TABLE stockpricealerts OWNER TO stockanalysis;

CREATE TABLE reminders (
    reminderid uuid PRIMARY KEY,
    userid uuid NOT NULL,
    date TIMESTAMP WITH TIME ZONE NOT NULL,
    message TEXT NOT NULL,
    ticker TEXT,
    state TEXT NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL,
    sentat TIMESTAMP WITH TIME ZONE
);
CREATE INDEX reminders_userid ON reminders(userid);
CREATE INDEX reminders_date ON reminders(date);
CREATE INDEX reminders_state ON reminders(state);
ALTER TABLE reminders OWNER TO stockanalysis;

CREATE TABLE tickercik (
    ticker TEXT PRIMARY KEY,
    cik TEXT NOT NULL,
    title TEXT NOT NULL,
    lastupdated TIMESTAMP WITH TIME ZONE NOT NULL
);
CREATE INDEX tickercik_cik ON tickercik(cik);
ALTER TABLE tickercik OWNER TO stockanalysis;
