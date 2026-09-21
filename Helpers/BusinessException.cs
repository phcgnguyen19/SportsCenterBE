namespace SportsCenterAPI.Helpers;

/// <summary>An expected business-rule failure with an HTTP response status.</summary>
public sealed class BusinessException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
