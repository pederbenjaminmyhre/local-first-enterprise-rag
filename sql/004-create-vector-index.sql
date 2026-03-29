-- 004-create-vector-index.sql
-- Create DiskANN vector index on DescriptionVector for fast approximate nearest-neighbor search
-- DiskANN maintains ~95% recall while searching datasets larger than available RAM
--
-- NOTE: CREATE VECTOR INDEX may not be available on all SQL Server 2025 editions/platforms.
-- On Linux containers, VECTOR_DISTANCE still works via brute-force scan, which is
-- performant for datasets under ~100K rows (DimProduct has ~600 rows).
-- In Azure SQL MI, DiskANN indexing is fully supported.

BEGIN TRY
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_DimProduct_DescriptionVector'
          AND object_id = OBJECT_ID('dbo.DimProduct')
    )
    BEGIN
        EXEC('CREATE VECTOR INDEX IX_DimProduct_DescriptionVector
              ON dbo.DimProduct(DescriptionVector)
              WITH (METRIC = ''cosine'', TYPE = DISKANN)');
        PRINT 'Created DiskANN vector index IX_DimProduct_DescriptionVector.'
    END
    ELSE
    BEGIN
        PRINT 'Vector index IX_DimProduct_DescriptionVector already exists. Skipping.'
    END
END TRY
BEGIN CATCH
    PRINT 'NOTE: DiskANN vector index creation not supported on this platform.'
    PRINT '      VECTOR_DISTANCE queries will use brute-force scan (fine for small datasets).'
    PRINT '      Error: ' + ERROR_MESSAGE()
END CATCH
GO
