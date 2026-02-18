using System.ComponentModel.DataAnnotations;

namespace SafeVault.Api.Models;

public sealed class RegisterRequest
{
    [Required, MinLength(3), MaxLength(32)]
    public string UserName { get; set; } = "";

    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; set; } = "";
}

public sealed class LoginRequest
{
    [Required, MinLength(3), MaxLength(32)]
    public string UserName { get; set; } = "";

    [Required, MinLength(1), MaxLength(128)]
    public string Password { get; set; } = "";
}

public sealed class CreateFinancialRecordRequest
{
    [Required, MinLength(1), MaxLength(80)]
    public string Name { get; set; } = "";

    [Range(0.01, 1_000_000_000)]
    public decimal Amount { get; set; }

    [Required, MinLength(3), MaxLength(3)]
    public string Currency { get; set; } = "THB";

    [MaxLength(500)]
    public string? Notes { get; set; }
}
