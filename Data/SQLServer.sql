DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'portfolio_db') THEN
        CREATE DATABASE portfolio_db;
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS "Users" 
(
    "Id" UUID PRIMARY KEY,
    "Email" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL
);