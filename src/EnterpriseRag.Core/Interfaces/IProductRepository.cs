namespace EnterpriseRag.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctColorsAsync(CancellationToken ct = default);
}
