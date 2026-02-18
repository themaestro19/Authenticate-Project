using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeVault.Api.Data;
using SafeVault.Api.Models;
using SafeVault.Api.Security;

namespace SafeVault.Api.Controllers;

[ApiController]
[Route("api/records")]
public sealed class FinancialRecordsController : ControllerBase
{
    private readonly SafeVaultDbContext _db;

    public FinancialRecordsController(SafeVaultDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "X-UserId")] Guid userId,
        CreateFinancialRecordRequest req,
        CancellationToken ct)
    {
        try
        {
            var name = InputSanitizer.SanitizeRecordName(req.Name);
            if (name.WasModified) throw new ValidationException("Record name contained forbidden characters.");

            var currency = InputSanitizer.SanitizeCurrency(req.Currency);
            if (currency.WasModified) throw new ValidationException("Currency contained forbidden characters.");
            InputSanitizer.ValidateCurrencyOrThrow(currency.Value);

            var notes = InputSanitizer.SanitizeNotes(req.Notes);
            if (notes.WasModified) throw new ValidationException("Notes contained forbidden characters.");

            var record = new FinancialRecord
            {
                UserId = userId,
                Name = name.Value,
                Amount = req.Amount,
                Currency = currency.Value,
                Notes = string.IsNullOrWhiteSpace(notes.Value) ? null : notes.Value
            };

            _db.FinancialRecords.Add(record);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = record.Id }, new { record.Id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var record = await _db.FinancialRecords.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return NotFound();
        return Ok(record);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromHeader(Name = "X-UserId")] Guid userId,
        [FromQuery] string q,
        CancellationToken ct)
    {
        // Validate/normalize q (don’t allow “script/query-ish” chars)
        var term = InputSanitizer.SanitizeRecordName(q);
        if (term.WasModified) return BadRequest(new { error = "Search term contained forbidden characters." });
        if (term.Value.Length is < 1 or > 80) return BadRequest(new { error = "Invalid search term length." });

        // Parameterized by EF Core through LINQ translation [page:2]
        var results = await _db.FinancialRecords.AsNoTracking()
            .Where(r => r.UserId == userId && r.Name.Contains(term.Value))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.Name, r.Amount, r.Currency, r.CreatedAt })
            .Take(50)
            .ToListAsync(ct);

        return Ok(results);
    }
}
