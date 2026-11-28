using ShopApp.Infrastructure;
using ShopApp.Models;

namespace ShopApp.Dtos;

/// <summary>Преобразование сущностей EF в DTO (наружу сущности не отдаём).</summary>
public static class Mapping
{
    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name, c.Icon);

    public static ProductDto ToDto(this Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Stock, p.ImageUrl, p.IsActive,
        p.CategoryId, p.Category?.Name ?? string.Empty, p.Category?.Icon ?? "📦");

    public static CartDto ToDto(this Cart? cart)
    {
        if (cart is null) return new CartDto([], 0, 0);

        var items = cart.Items
            .OrderBy(i => i.Id)
            .Select(i => new CartItemDto(
                i.ProductId,
                i.Product.Name,
                i.Product.Price,
                i.Quantity,
                i.Product.Stock,
                IsAvailable: i.Product.IsActive && i.Product.Stock >= i.Quantity,
                i.Product.ImageUrl,
                i.Product.Category?.Icon ?? "📦",
                LineTotal: i.Product.Price * i.Quantity))
            .ToList();

        return new CartDto(items, items.Sum(i => i.Quantity), items.Sum(i => i.LineTotal));
    }

    public static OrderDto ToDto(this Order o) => new(
        o.Id, o.Status, o.CreatedAt,
        o.CustomerName, o.Phone, o.Email, o.Address, o.Comment,
        o.Total,
        o.Items.OrderBy(i => i.Id)
               .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
               .ToList(),
        CanCancel: OrderWorkflow.ClientCanCancel(o.Status),
        NextStatuses: OrderWorkflow.NextStatuses(o.Status));
}
