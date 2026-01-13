using FIAP.MicroService.Payments.Domain.Dtos;

namespace FIAP.MicroService.Payments.Domain.Services;

public interface IUserApiService
{
    Task<UserInfo?> GetById(Guid userId);
}
