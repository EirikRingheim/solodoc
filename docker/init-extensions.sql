-- Enable required PostgreSQL extensions for Solodoc
-- This runs automatically on first database creation

-- Full-text search with Norwegian language support (built-in, just verify)
-- tsvector columns use 'norwegian' configuration

-- Fuzzy/typo-tolerant search
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
