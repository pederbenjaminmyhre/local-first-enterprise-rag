-- 002-alter-dimproduct-vector.sql
-- Add VECTOR(1024) column to DimProduct for storing mxbai-embed-large embeddings

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
      AND TABLE_NAME = 'DimProduct'
      AND COLUMN_NAME = 'DescriptionVector'
)
BEGIN
    ALTER TABLE dbo.DimProduct
    ADD DescriptionVector VECTOR(1024) NULL;

    PRINT 'Added DescriptionVector VECTOR(1024) column to DimProduct.'
END
ELSE
BEGIN
    PRINT 'DescriptionVector column already exists. Skipping.'
END
GO
