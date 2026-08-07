namespace PGM.Core.Application.Models.Auth;

public class PermissionResponse
{
    public string FunctionId { get; set; } = string.Empty;
    public bool Allowed { get; set; }
}
