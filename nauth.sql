-- TABLES (com IF NOT EXISTS)
CREATE TABLE IF NOT EXISTS user_addresses (
    address_id bigint NOT NULL,
    user_id bigint NOT NULL,
    zip_code character varying(15),
    address character varying(150),
    complement character varying(150),
    neighborhood character varying(120),
    city character varying(120),
    state character varying(80)
);

CREATE TABLE IF NOT EXISTS user_documents (
    document_id bigint NOT NULL,
    user_id bigint,
    document_type integer DEFAULT 0 NOT NULL,
    base64 text
);

CREATE TABLE IF NOT EXISTS user_phones (
    phone_id bigint NOT NULL,
    user_id bigint NOT NULL,
    phone character varying(30) NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
    user_id bigint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL,
    hash character varying(128),
    slug character varying(140) NOT NULL,
    email character varying(180) NOT NULL,
    name character varying(120) NOT NULL,
    password character varying(255),
    is_admin boolean DEFAULT false NOT NULL,
    recovery_hash character varying(128),
    id_document character varying(30),
    birth_date timestamp without time zone,
    pix_key character varying(180),
    stripe_id character varying(120),
    image character varying(150),
    status integer DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS roles (
    role_id bigint NOT NULL,
    slug character varying(80) NOT NULL,
    name character varying(80) NOT NULL
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id bigint NOT NULL,
    role_id bigint NOT NULL
);

-- SEQUENCES (com verificação IF NOT EXISTS)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'user_id_seq') THEN
        CREATE SEQUENCE user_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'user_addresses_id_seq') THEN
        CREATE SEQUENCE user_addresses_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'user_documents_id_seq') THEN
        CREATE SEQUENCE user_documents_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'user_phones_id_seq') THEN
        CREATE SEQUENCE user_phones_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'role_id_seq') THEN
        CREATE SEQUENCE role_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
    END IF;
END $$;

-- SET SEQUENCES TO COLUMNS (seguro para executar múltiplas vezes)
ALTER SEQUENCE user_id_seq OWNED BY users.user_id;
ALTER SEQUENCE user_addresses_id_seq OWNED BY user_addresses.address_id;
ALTER SEQUENCE user_documents_id_seq OWNED BY user_documents.document_id;
ALTER SEQUENCE user_phones_id_seq OWNED BY user_phones.phone_id;
ALTER SEQUENCE role_id_seq OWNED BY roles.role_id;

-- SET SEQUENCES TO DEFAULT VALUE (seguro para executar múltiplas vezes)
ALTER TABLE ONLY users ALTER COLUMN user_id SET DEFAULT nextval('user_id_seq'::regclass);
ALTER TABLE ONLY user_addresses ALTER COLUMN address_id SET DEFAULT nextval('user_addresses_id_seq'::regclass);
ALTER TABLE ONLY user_documents ALTER COLUMN document_id SET DEFAULT nextval('user_documents_id_seq'::regclass);
ALTER TABLE ONLY user_phones ALTER COLUMN phone_id SET DEFAULT nextval('user_phones_id_seq'::regclass);
ALTER TABLE ONLY roles ALTER COLUMN role_id SET DEFAULT nextval('role_id_seq'::regclass);

-- PRIMARY KEYS (com verificação IF NOT EXISTS)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'users_pkey') THEN
        ALTER TABLE ONLY users ADD CONSTRAINT users_pkey PRIMARY KEY (user_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_user_addresses') THEN
        ALTER TABLE ONLY user_addresses ADD CONSTRAINT pk_user_addresses PRIMARY KEY (address_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_documents_pkey') THEN
        ALTER TABLE ONLY user_documents ADD CONSTRAINT user_documents_pkey PRIMARY KEY (document_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_phones_pkey') THEN
        ALTER TABLE ONLY user_phones ADD CONSTRAINT user_phones_pkey PRIMARY KEY (phone_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'roles_pkey') THEN
        ALTER TABLE ONLY roles ADD CONSTRAINT roles_pkey PRIMARY KEY (role_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_roles_pkey') THEN
        ALTER TABLE ONLY user_roles ADD CONSTRAINT user_roles_pkey PRIMARY KEY (user_id, role_id);
    END IF;
END $$;

-- FOREIGN KEYS (com verificação IF NOT EXISTS)
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_address') THEN
        ALTER TABLE ONLY user_addresses ADD CONSTRAINT fk_user_address FOREIGN KEY (user_id) REFERENCES users(user_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_document') THEN
        ALTER TABLE ONLY user_documents ADD CONSTRAINT fk_user_document FOREIGN KEY (user_id) REFERENCES users(user_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_phone') THEN
        ALTER TABLE ONLY user_phones ADD CONSTRAINT fk_user_phone FOREIGN KEY (user_id) REFERENCES users(user_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_role_user') THEN
        ALTER TABLE ONLY user_roles ADD CONSTRAINT fk_user_role_user FOREIGN KEY (user_id) REFERENCES users(user_id);
    END IF;
END $$;

DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_user_role_role') THEN
        ALTER TABLE ONLY user_roles ADD CONSTRAINT fk_user_role_role FOREIGN KEY (role_id) REFERENCES roles(role_id);
    END IF;
END $$;

-- CREATE ADMIN USER (com ON CONFLICT para evitar duplicatas)
INSERT INTO users (
	user_id, 
	created_at, 
	updated_at, 
	slug, 
	hash, 
	email, 
	name, 
	password, 
	is_admin, 
	recovery_hash, 
	id_document, 
	birth_date, 
	pix_key, 
	stripe_id, 
	image
) VALUES (
	1, 
	CURRENT_DATE, 
	CURRENT_DATE, 
	'rodrigo-landim',
	'pU8MILkTP95axC7-02EyMt2cA6fTNsSv-IK5z6KrbG7iS6n76c8VDVsq39pRVy319DorstlJ5Gy06WByFpqRTSEfmMmSWylIWXV2', 
	'rodrigo@emagine.com.br', 
	'Rodrigo Landim Carneiro', 
	NULL, 
	true, 
	'first-access', 
	NULL, 
	NULL, 
	NULL,
	NULL, 
	NULL
)
ON CONFLICT (user_id) DO NOTHING;

INSERT INTO user_addresses (
	address_id, 
	user_id, 
	zip_code, 
	address, 
	complement, 
	neighborhood, 
	city, 
	state
) VALUES (
	1, 
	1, 
	'73252900', 
	'Condomínio RK', 
	'Conj. Centauros Qd N Casa 42', 
	'Sobradinho', 
	'Brasília', 
	'DF'
)
ON CONFLICT (address_id) DO NOTHING;

INSERT INTO user_phones (
	phone_id, 
	user_id, 
	phone
) VALUES (
	1, 
	1, 
	'61998752588'
)
ON CONFLICT (phone_id) DO NOTHING;

INSERT INTO roles (
	role_id,
	slug,
	name
) VALUES (
	1,
	'administrator',
	'Administrator'
)
ON CONFLICT (role_id) DO NOTHING;

INSERT INTO user_roles (
	user_id,
	role_id
) VALUES (
	1,
	1
)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- AJUSTAR SEQUENCES (apenas se necessário)
DO $$
BEGIN
    PERFORM setval('user_id_seq', GREATEST(2, (SELECT COALESCE(MAX(user_id), 0) + 1 FROM users)), false);
    PERFORM setval('user_addresses_id_seq', GREATEST(2, (SELECT COALESCE(MAX(address_id), 0) + 1 FROM user_addresses)), false);
    PERFORM setval('user_phones_id_seq', GREATEST(2, (SELECT COALESCE(MAX(phone_id), 0) + 1 FROM user_phones)), false);
    PERFORM setval('role_id_seq', GREATEST(2, (SELECT COALESCE(MAX(role_id), 0) + 1 FROM roles)), false);
END $$;
