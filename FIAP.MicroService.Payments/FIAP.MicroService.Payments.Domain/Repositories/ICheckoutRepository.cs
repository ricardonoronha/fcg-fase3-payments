using FIAP.MicroService.Payments.Domain.Models;

namespace FIAP.MicroService.Payments.Domain.Repositories;

public interface ICheckoutRepository
{
    Task<Checkout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Checkout checkout, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
