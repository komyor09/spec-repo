using Microsoft.AspNetCore.Mvc;
using ShopApp.Dtos;
using ShopApp.Services;

namespace ShopApp.Controllers;

[ApiController]
[Route("api")]
public class CatalogController(ProductService products) : ControllerBase
{
    /// <summary>GET /api/categories — список категорий.</summary>
    [HttpGet("categories")]
    public Task<List<CategoryDto>> GetCategories() => products.GetCategoriesAsync();

    /// <summary>
    /// GET /api/products?categoryId=1&amp;search=код&amp;sort=price_asc&amp;page=1&amp;pageSize=12
    /// sort: new | price_asc | price_desc | name
    /// </summary>
    [HttpGet("products")]
    public Task<PagedResult<ProductDto>> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
        => products.SearchAsync(categoryId, search, sort, page, pageSize, includeInactive: false);

    /// <summary>GET /api/products/5 — карточка товара.</summary>
    [HttpGet("products/{id:int}")]
    public Task<ProductDto> GetProduct(int id) => products.GetAsync(id);
}
