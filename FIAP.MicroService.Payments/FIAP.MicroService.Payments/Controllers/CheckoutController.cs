
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
    public async Task< ActionResult<GetCheckoutResponse>> GetCheckout(Guid checkoutId , CancellationToken cancellationToken)
    {
        if (checkoutId == Guid.Empty)
            return BadRequest();

        var result = await checkoutService.GetCheckout(checkoutId, cancellationToken);

        return Ok(result);
    }



    [HttpPost("finish", Name = nameof(FinishCheckout))]
    public async Task<ActionResult<FinishCheckoutResponse>> FinishCheckout([FromBody] FinishCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!FinishCheckoutRequest.IsValid(request, out var message))
            return BadRequest(new { Message = message });

        using var _ = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CheckoutId"] = request.CheckoutId,
            ["PaymentMethod"] = request.PaymentMethod
        });

        logger.LogInformation("Finalização de checkout iniciada");

        var result = await checkoutService.FinishCheckout(request.CheckoutId, cancellationToken);

        // 🔹 Gera o link absoluto respeitando X-Forwarded-* e PathBase
        var statusUrl = linkGenerator.GetUriByAction(
            httpContext: HttpContext,
            action: nameof(GetCheckout),
            controller: "Checkout",
            values: new { id = result.CheckoutId });


        logger.LogInformation("Finalização de checkout concluída com sucesso");

        // 🔹 Retorna 202 Accepted com Location e corpo de status
        return Accepted(statusUrl, new
        {
            result.CheckoutId,
            message = "Checkout aceito e será processado.",
            statusUrl
        });





    }
}

