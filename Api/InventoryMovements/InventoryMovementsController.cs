using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Api.Auth;
using Inventory.Domain.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.InventoryMovements;

[ApiController]
[Route("api/inventory-movements")]
[Authorize]
public sealed class InventoryMovementsController : ControllerBase
{
    private const string IdempotencyKeyHeaderName = "Idempotency-Key";
    private const int MaxIdempotencyKeyLength = 100;

    private readonly RegisterInventoryMovementHandler _handler;

    public InventoryMovementsController(
        RegisterInventoryMovementHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.InventoryCreate)]
    [ProducesResponseType(typeof(RegisterInventoryMovementResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterInventoryMovementRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request, idempotencyKey);

        if (validationError is not null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: validationError);
        }

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = request.ProductId,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            IdempotencyKey = idempotencyKey
        };

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    private static string? Validate(
        RegisterInventoryMovementRequest request,
        string idempotencyKey)
    {
        if (request.ProductId == Guid.Empty)
        {
            return "ProductId must not be empty.";
        }

        if (request.Quantity <= 0)
        {
            return "Quantity must be greater than zero.";
        }

        if (!Enum.IsDefined(typeof(InventoryMovementType), request.MovementType))
        {
            return "MovementType is not a valid value.";
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return "Idempotency-Key header is required.";
        }

        if (idempotencyKey.Length > MaxIdempotencyKeyLength)
        {
            return $"Idempotency-Key header must not exceed {MaxIdempotencyKeyLength} characters.";
        }

        return null;
    }
}
