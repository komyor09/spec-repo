using Microsoft.EntityFrameworkCore;
using ShopApp.Data;
using ShopApp.Dtos;
using ShopApp.Infrastructure;
using ShopApp.Models;

namespace ShopApp.Services;

public class OrderService(ShopDbContext db)
{
    /// <summary>
    /// Оформление заказа из корзины. Всё выполняется в одной транзакции:
    /// проверка остатков → списание со склада → создание заказа → очистка корзины.
    /// </summary>
    public async Task<OrderDto> CreateFromCartAsync(Guid clientId, CheckoutDto dto)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        var cart = await db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (cart is null || cart.Items.Count == 0)
            throw ShopException.BadRequest("Корзина пуста");

        // Проверяем все позиции сразу, чтобы показать покупателю полный список проблем
        var problems = new List<string>();
        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
                problems.Add($"«{item.Product.Name}» больше не продаётся");
            else if (item.Quantity > item.Product.Stock)
                problems.Add($"«{item.Product.Name}»: в наличии только {item.Product.Stock} шт.");
        }
        if (problems.Count > 0)
            throw ShopException.BadRequest("Не удалось оформить заказ: " + string.Join("; ", problems));

        var order = new Order
        {
            ClientId = clientId,
            CustomerName = dto.CustomerName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = dto.Email.Trim(),
            Address = dto.Address.Trim(),
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            item.Product.Stock -= item.Quantity; // списываем со склада

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity
            });
        }
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return order.ToDto();
    }

    public async Task<List<OrderDto>> GetClientOrdersAsync(Guid clientId)
    {
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<OrderDto> GetClientOrderAsync(Guid clientId, int orderId)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == clientId);

        return order?.ToDto() ?? throw ShopException.NotFound("Заказ не найден");
    }

    /// <summary>Отмена заказа покупателем (только в статусе «Новый»).</summary>
    public async Task<OrderDto> CancelByClientAsync(Guid clientId, int orderId)
    {
        var order = await LoadForUpdateAsync(o => o.Id == orderId && o.ClientId == clientId);

        if (!OrderWorkflow.ClientCanCancel(order.Status))
            throw ShopException.BadRequest("Заказ уже в обработке — отменить его можно только через поддержку");

        await ChangeStatusInternalAsync(order, OrderStatus.Cancelled);
        return order.ToDto();
    }

    // ===================== Администрирование =====================

    public async Task<List<OrderDto>> GetAllAsync(OrderStatus? status)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var orders = await query.OrderByDescending(o => o.Id).ToListAsync();
        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<OrderDto> ChangeStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await LoadForUpdateAsync(o => o.Id == orderId);

        if (!OrderWorkflow.CanChange(order.Status, newStatus))
            throw ShopException.BadRequest($"Нельзя перевести заказ из статуса {order.Status} в {newStatus}");

        await ChangeStatusInternalAsync(order, newStatus);
        return order.ToDto();
    }

    // ===================== Вспомогательное =====================

    private async Task<Order> LoadForUpdateAsync(System.Linq.Expressions.Expression<Func<Order, bool>> predicate) =>
        await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(predicate)
        ?? throw ShopException.NotFound("Заказ не найден");

    private async Task ChangeStatusInternalAsync(Order order, OrderStatus newStatus)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        // При отмене возвращаем товары на склад
        if (newStatus == OrderStatus.Cancelled)
        {
            var ids = order.Items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).ToList();
            var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            foreach (var item in order.Items)
            {
                if (item.ProductId is int pid && products.TryGetValue(pid, out var product))
                    product.Stock += item.Quantity;
            }
        }

        order.Status = newStatus;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
