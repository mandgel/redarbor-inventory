using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Domain.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.InventoryMovements;

[ApiController]
[Route("api/inventory-movements")]
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
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterInventoryMovementRequest request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeaderName]
            .ToString();

        var validationError = Validate(request, idempotencyKey);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = request.ProductId,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            IdempotencyKey = idempotencyKey
        };

        return await ExecuteAsync(command, cancellationToken);
    }

    private async Task<IActionResult> ExecuteAsync(
        RegisterInventoryMovementCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _handler.HandleAsync(
                command,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InsufficientStockException)
        {
            return Conflict("Insufficient stock to complete the inventory movement.");
        }
        catch (IdempotencyConflictException)
        {
            return Conflict("The idempotency key has already been used for a different request.");
        }
        catch (ProductNotFoundException)
        {
            return NotFound("The specified product does not exist.");
        }
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
