using Microsoft.AspNetCore.Mvc;
using ShopApp.Dtos;
using ShopApp.Infrastructure;
using ShopApp.Models;
using ShopApp.Services;

namespace ShopApp.Controllers;

/// <summary>Методы администратора. Все требуют заголовок X-Admin-Key.</summary>
[ApiController]
[Route("api/admin")]
[AdminKey]
public class AdminController(OrderService orders, ProductService products) : ControllerBase
{
    /// <summary>GET /api/admin/check — проверка ключа (для входа в админку).</summary>
    [HttpGet("check")]
    public IActionResult Check() => NoContent();

    // ---------- Заказы ----------

    /// <summary>GET /api/admin/orders?status=New</summary>
    [HttpGet("orders")]
    public Task<List<OrderDto>> GetOrders([FromQuery] OrderStatus? status) => orders.GetAllAsync(status);

    /// <summary>PUT /api/admin/orders/5/status — { "status": "Processing" }</summary>
    [HttpPut("orders/{id:int}/status")]
    public Task<OrderDto> ChangeStatus(int id, [FromBody] ChangeStatusDto dto) =>
        orders.ChangeStatusAsync(id, dto.Status!.Value);

    // ---------- Товары ----------

    /// <summary>GET /api/admin/products — все товары, включая скрытые.</summary>
    [HttpGet("products")]
    public Task<PagedResult<ProductDto>> GetProducts([FromQuery] string? search) =>
        products.SearchAsync(null, search, "name", 1, 100, includeInactive: true);

    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] ProductEditDto dto)
    {
        var created = await products.CreateAsync(dto);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("products/{id:int}")]
    public Task<ProductDto> UpdateProduct(int id, [FromBody] ProductEditDto dto) => products.UpdateAsync(id, dto);
}
