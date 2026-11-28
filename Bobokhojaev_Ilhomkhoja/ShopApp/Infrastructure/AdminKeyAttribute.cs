using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ShopApp.Infrastructure;

/// <summary>
/// Упрощённая защита админских методов: запрос должен содержать заголовок
/// X-Admin-Key, совпадающий с Admin:Key из appsettings.json.
/// Для учебного проекта этого достаточно; в реальном — ASP.NET Core Identity / JWT.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = config["Admin:Key"];
        var actual = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expected) || actual != expected)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Неверный ключ администратора" });
        }
    }
}
