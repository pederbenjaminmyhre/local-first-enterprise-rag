namespace EnterpriseRag.Core.Models;

public class Product
{
    public int ProductKey { get; set; }
    public string EnglishProductName { get; set; } = string.Empty;
    public string? EnglishDescription { get; set; }
    public string? ProductSubcategoryName { get; set; }
    public string? Color { get; set; }
    public float[]? DescriptionVector { get; set; }
}
