namespace DGPM_SPM.Core.Application.Models.Auth;

public class PermissionBatchRequest
{
    public List<string> FunctionIds { get; set; } = new();
}
