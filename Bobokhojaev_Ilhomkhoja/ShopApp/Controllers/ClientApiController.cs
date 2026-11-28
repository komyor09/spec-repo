using Microsoft.AspNetCore.Mvc;
using ShopApp.Infrastructure;

namespace ShopApp.Controllers;

/// <summary>
/// Базовый контроллер для методов покупателя.
/// Покупатель определяется заголовком X-Client-Id (GUID из localStorage браузера).
/// </summary>
[ApiController]
public abstract class ClientApiController : ControllerBase
{
    public const string ClientIdHeader = "X-Client-Id";

    protected Guid ClientId
    {
        get
        {
            var raw = Request.Headers[ClientIdHeader].ToString();
            if (Guid.TryParse(raw, out var id) && id != Guid.Empty) return id;
            throw ShopException.BadRequest($"Не передан или некорректен заголовок {ClientIdHeader}");
        }
    }
}
