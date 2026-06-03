using Microsoft.Extensions.Options;
using Norge360.Accounts.API.Options;

namespace Norge360.Accounts.API.Security;

public sealed class InternalServiceSigningOptionsValidation : IValidateOptions<InternalServiceSigningOptions>
{
    public ValidateOptionsResult Validate(string? name, InternalServiceSigningOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            failures.Add("InternalServices:Signing:Secret is required when signing is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            failures.Add("InternalServices:Signing:ServiceName is required when signing is enabled.");
        }

        if (options.ClockSkewSeconds is < 30 or > 600)
        {
            failures.Add("InternalServices:Signing:ClockSkewSeconds must be between 30 and 600.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
