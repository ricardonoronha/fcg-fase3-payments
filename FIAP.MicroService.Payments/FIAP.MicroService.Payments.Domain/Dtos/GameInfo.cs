namespace FIAP.MicroService.Payments.Domain.Dtos;

public class GameInfo
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}

