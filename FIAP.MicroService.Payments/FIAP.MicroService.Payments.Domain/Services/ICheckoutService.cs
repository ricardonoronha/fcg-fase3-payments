using FIAP.MicroService.Payments.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIAP.MicroService.Payments.Domain.Services;

public interface ICheckoutService
{
    Task<StartCheckoutResponse> StartCheckout(StartCheckoutRequest request, CancellationToken ct = default);
    Task<GetCheckoutResponse> GetCheckout(Guid checkoutId, CancellationToken ct = default);
    Task<FinishCheckoutResponse> FinishCheckout(Guid checkoutId, CancellationToken ct = default);
}
