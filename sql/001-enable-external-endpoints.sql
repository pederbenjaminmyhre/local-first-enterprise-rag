-- 001-enable-external-endpoints.sql
-- Enable external REST endpoint support in SQL Server 2025
-- Required for SQL Server to communicate with external services via HTTPS

IF NOT EXISTS (SELECT 1 FROM sys.configurations WHERE name = 'external rest endpoint enabled')
BEGIN
    PRINT 'External REST endpoint configuration not found (may not be needed in this SQL Server edition).'
END
ELSE
BEGIN
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;

    EXEC sp_configure 'external rest endpoint enabled', 1;
    RECONFIGURE;

    PRINT 'External REST endpoints enabled successfully.'
END
GO
