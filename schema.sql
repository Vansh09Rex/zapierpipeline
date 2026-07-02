-- PostgreSQL DDL Schema for Shopify-Zapier Integration Pipeline

-- Orders Table definition
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id VARCHAR(100) NOT NULL UNIQUE,
    customer_email VARCHAR(255) NOT NULL,
    total_amount NUMERIC(12, 2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Synced, Failed
    zoho_record_id VARCHAR(100),
    source VARCHAR(50) NOT NULL -- Shopify, Zapier
);

-- Indexing for optimized lookup
CREATE INDEX IF NOT EXISTS idx_orders_order_id ON orders(order_id);
CREATE INDEX IF NOT EXISTS idx_orders_customer_email ON orders(customer_email);
