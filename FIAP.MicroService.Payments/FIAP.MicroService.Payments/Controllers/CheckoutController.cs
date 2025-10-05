using Microsoft.AspNetCore.Mvc;

namespace FIAP.MicroService.Payments.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController(
    ILogger<CheckoutController> logger,
    LinkGenerator linkGenerator) : ControllerBase
{
    [HttpPost(Name = nameof(StartCheckout))]
    public ActionResult<StartCheckoutResponse> StartCheckout([FromBody] StartCheckoutRequest request)
    {
        if (!StartCheckoutRequest.IsValid(request, out var message))
            return BadRequest(new { Message = message });

        using var _ = logger.BeginScope(new Dictionary<string, object?>
        {
            ["UserId"] = request.UserId,
            ["GameId"] = request.GameId
        });

        // 🔹 Gera o ID do checkout (normalmente salvo no banco)
        var checkoutId = Guid.NewGuid();

        // 🔹 Gera o link absoluto respeitando X-Forwarded-* e PathBase
        var statusUrl = linkGenerator.GetUriByAction(
            httpContext: HttpContext,
            action: nameof(GetCheckout),
            controller: "Checkout",
            values: new { checkoutId });


        logger.LogInformation("Checkout criado com sucesso");

        // 🔹 Retorna 202 Accepted com Location e corpo de status
        return Accepted(statusUrl, new
        {
            checkoutId,
            message = "Checkout aceito e será processado.",
            statusUrl
        });


    }

    [HttpGet("{checkoutId:guid}", Name = nameof(GetCheckout))]
    public ActionResult<StartCheckoutResponse> GetCheckout(Guid checkoutId)
    {
        if (checkoutId == Guid.Empty)
            return BadRequest();

        return Ok(new StartCheckoutResponse());
    }



    [HttpPost("finish", Name = nameof(FinishCheckout))]
    public ActionResult<FinishCheckoutResponse> FinishCheckout([FromBody] FinishCheckoutRequest request)
    {
        if (!FinishCheckoutRequest.IsValid(request, out var message))
            return BadRequest(new { Message = message });

        using var _ = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CheckoutId"] = request.CheckoutId,
            ["PaymentMethod"] = request.PaymentMethod
        });

        logger.LogInformation("Finalização de checkout iniciada");

        // 🔹 Gera o ID do checkout (normalmente salvo no banco)
        var checkoutId = Guid.NewGuid();

        // 🔹 Gera o link absoluto respeitando X-Forwarded-* e PathBase
        var statusUrl = linkGenerator.GetUriByAction(
            httpContext: HttpContext,
            action: nameof(GetCheckout),
            controller: "Checkout",
            values: new { id = checkoutId });


        logger.LogInformation("Finalização de checkout concluída com sucesso");

        // 🔹 Retorna 202 Accepted com Location e corpo de status
        return Accepted(statusUrl, new
        {
            checkoutId,
            message = "Checkout aceito e será processado.",
            statusUrl
        });


    }
}

public class StartCheckoutRequest
{
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }

    public static bool IsValid(StartCheckoutRequest request, out string message)
    {
        if (request is null || request.UserId == Guid.Empty || request.GameId == Guid.Empty)
        {
            message = $"{nameof(UserId)} e {nameof(GameId)} são obrigatórios.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}
public class StartCheckoutResponse
{
    public Guid CheckoutId { get; set; }
    public UserInfo User { get; set; } = default!;
    public GameInfo Game { get; set; } = default!;
    public decimal Total { get; set; }
    public string[] PaymentMethods { get; set; } = [];
}

public class FinishCheckoutRequest
{
    public Guid CheckoutId { get; set; }
    public string PaymentMethod { get; set; } = default!;

    public static bool IsValid(FinishCheckoutRequest request, out string message)
    {
        if (request is null || request.CheckoutId == Guid.Empty || string.IsNullOrEmpty(request.PaymentMethod))
        {
            message = $"{nameof(CheckoutId)} e {nameof(PaymentMethod)} são obrigatórios.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}

public class FinishCheckoutResponse
{
    public Guid CheckoutId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }

}


public class UserInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class GameInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

