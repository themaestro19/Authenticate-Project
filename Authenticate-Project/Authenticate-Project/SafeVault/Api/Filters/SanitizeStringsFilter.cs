using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SafeVault.Api.Security;
using System.Reflection;

namespace SafeVault.Api.Filters;

public sealed class SanitizeStringsFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            SanitizeObjectGraph(arg);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static void SanitizeObjectGraph(object obj)
    {
        var type = obj.GetType();

        // Only sanitize DTO-like objects (avoid touching framework objects)
        if (type.IsPrimitive || obj is string) return;

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (prop.PropertyType != typeof(string)) continue;

            var current = (string?)prop.GetValue(obj);
            if (current is null) continue;

            // Generic sanitization: strip control chars + angle brackets
            var sanitized = InputSanitizer.SanitizeNotes(current);

            // If the input had to be modified, reject rather than silently “fixing” sensitive input
            if (sanitized.WasModified)
                throw new ValidationException($"Input contained forbidden characters in field '{prop.Name}'.");

            prop.SetValue(obj, sanitized.Value);
        }
    }
}
