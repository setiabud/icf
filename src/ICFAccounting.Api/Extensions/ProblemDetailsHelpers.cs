namespace ICFAccounting.Api.Extensions;

internal static class ProblemDetailsHelpers
{
    // ArgumentException.Message appends " (Parameter 'name')"; strip it so internal parameter names are not exposed.
    public static string GetClientSafeMessage(ArgumentException exception)
    {
        var parameterSuffix = $" (Parameter '{exception.ParamName}')";
        return exception.ParamName is not null && exception.Message.EndsWith(parameterSuffix, StringComparison.Ordinal)
            ? exception.Message[..^parameterSuffix.Length]
            : exception.Message;
    }
}
