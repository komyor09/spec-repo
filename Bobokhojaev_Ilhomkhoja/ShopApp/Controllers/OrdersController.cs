using Microsoft.AspNetCore.Mvc;
using ShopApp.Dtos;
using ShopApp.Services;

namespace ShopApp.Controllers;

[Route("api/orders")]
public class OrdersController(OrderService orders) : ClientApiController
{
    /// <summary>POST /api/orders — оформить заказ из текущей корзины.</summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CheckoutDto dto)
    {
        var order = await orders.CreateFromCartAsync(ClientId, dto);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>GET /api/orders — заказы текущего покупателя.</summary>
    [HttpGet]
    public Task<List<OrderDto>> List() => orders.GetClientOrdersAsync(ClientId);

    /// <summary>GET /api/orders/5 — один заказ.</summary>
    [HttpGet("{id:int}")]
    public Task<OrderDto> Get(int id) => orders.GetClientOrderAsync(ClientId, id);

    /// <summary>POST /api/orders/5/cancel — отменить свой заказ (если он ещё «Новый»).</summary>
    [HttpPost("{id:int}/cancel")]
    public Task<OrderDto> Cancel(int id) => orders.CancelByClientAsync(ClientId, id);
}
