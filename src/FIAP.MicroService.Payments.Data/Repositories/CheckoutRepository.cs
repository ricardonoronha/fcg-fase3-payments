using FIAP.MicroService.Payments.Domain.Models;
using FIAP.MicroService.Payments.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FIAP.MicroService.Payments.Data.Repositories;

public class CheckoutRepository : ICheckoutRepository
{
    private readonly PaymentsDbContext _db;

    public CheckoutRepository(PaymentsDbContext db) => _db = db;

    public async Task<Checkout?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Checkouts
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task AddAsync(Checkout checkout, CancellationToken ct = default)
    {
        await _db.Checkouts.AddAsync(checkout, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
