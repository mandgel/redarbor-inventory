using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public static class InventoryMovementFingerprint
{
    public static string Create(
        RegisterInventoryMovementCommand command)
    {
        var payload = string.Join(
            "|",
            command.ProductId.ToString("D"),
            (int)command.MovementType,
            command.Quantity.ToString(
                "G29",
                CultureInfo.InvariantCulture));

        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}