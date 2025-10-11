
using FIAP.MicroService.Payments.Domain.Dtos;
using FIAP.MicroService.Payments.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace FIAP.MicroService.Payments.API.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController(
    ILogger<CheckoutController> logger,
    LinkGenerator linkGenerator,
    ICheckoutService checkoutService) : ControllerBase
{
    [HttpPost(Name = nameof(StartCheckout))]
    public async Task<ActionResult<StartCheckoutResponse>> StartCheckout([FromBody] StartCheckoutRequest request, CancellationToken ct)
    {
        if (!StartCheckoutRequest.IsValid(request, out var message))
            return BadRequest(new { Message = message });

        using var _ = logger.BeginScope(new Dictionary<string, object?>
        {
            ["UserId"] = request.UserId,
            ["GameId"] = request.GameId
        });

        var response = await checkoutService.StartCheckout(request, ct);

        // 🔹 Gera o link absoluto respeitando X-Forwarded-* e PathBase
        var statusUrl = linkGenerator.GetUriByAction(
            httpContext: HttpContext,
            action: nameof(GetCheckout),
            controller: "Checkout",
            values: new { response.CheckoutId });

        logger.LogInformation("Checkout criado com sucesso");

        return Accepted(statusUrl, response);
    }

    [HttpGet("{checkoutId:guid}", Name = nameof(GetCheckout))]
    public ActionResult<GetCheckoutResponse> GetCheckout(Guid checkoutId)
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

