using Microsoft.AspNetCore.Mvc;
using ShopApp.Dtos;
using ShopApp.Services;

namespace ShopApp.Controllers;

[Route("api/cart")]
public class CartController(CartService cart) : ClientApiController
{
    /// <summary>GET /api/cart — содержимое корзины.</summary>
    [HttpGet]
    public Task<CartDto> Get() => cart.GetAsync(ClientId);

    /// <summary>POST /api/cart/items — добавить товар (или увеличить количество).</summary>
    [HttpPost("items")]
    public Task<CartDto> AddItem([FromBody] AddToCartDto dto) =>
        cart.AddItemAsync(ClientId, dto.ProductId, dto.Quantity);

    /// <summary>PUT /api/cart/items/5 — задать количество (0 — удалить).</summary>
    [HttpPut("items/{productId:int}")]
    public Task<CartDto> SetQuantity(int productId, [FromBody] UpdateQuantityDto dto) =>
        cart.SetQuantityAsync(ClientId, productId, dto.Quantity);

    /// <summary>DELETE /api/cart/items/5 — удалить позицию.</summary>
    [HttpDelete("items/{productId:int}")]
    public Task<CartDto> RemoveItem(int productId) => cart.RemoveItemAsync(ClientId, productId);

    /// <summary>DELETE /api/cart — очистить корзину.</summary>
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await cart.ClearAsync(ClientId);
        return NoContent();
    }
}
