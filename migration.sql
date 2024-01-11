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

-- cash,equity,longValue,shortValue,date,userId
CREATE TABLE accountbalancessnapshots (
    userid uuid,
    cash decimal,
    equity decimal,
    longvalue decimal,
    shortvalue decimal,
    date timestamp,
    PRIMARY KEY (userid,date) 
);
ALTER TABLE accountbalancessnapshots OWNER TO stocks;