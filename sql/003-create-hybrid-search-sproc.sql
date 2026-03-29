-- 003-create-hybrid-search-sproc.sql
-- Hybrid search stored procedure: combines VECTOR_DISTANCE (cosine similarity)
-- with optional metadata filters on Category and Color

CREATE OR ALTER PROCEDURE dbo.usp_HybridProductSearch
    @QueryVector  NVARCHAR(MAX),   -- Vector literal format: [0.123,0.456,...]
    @TopK         INT = 5,
    @Category     NVARCHAR(50) = NULL,
    @Color        NVARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopK)
        p.ProductKey,
        p.EnglishProductName,
        p.EnglishDescription,
        sc.EnglishProductSubcategoryName AS ProductSubcategoryName,
        p.Color,
        1.0 - VECTOR_DISTANCE('cosine', p.DescriptionVector, CAST(@QueryVector AS VECTOR(1024))) AS SimilarityScore
    FROM dbo.DimProduct p
    LEFT JOIN dbo.DimProductSubcategory sc
        ON p.ProductSubcategoryKey = sc.ProductSubcategoryKey
    WHERE p.DescriptionVector IS NOT NULL
      AND (@Category IS NULL OR sc.EnglishProductSubcategoryName LIKE '%' + @Category + '%')
      AND (@Color IS NULL OR p.Color = @Color)
    ORDER BY
        VECTOR_DISTANCE('cosine', p.DescriptionVector, CAST(@QueryVector AS VECTOR(1024))) ASC;
END
GO

PRINT 'Created/updated usp_HybridProductSearch stored procedure.'
GO
