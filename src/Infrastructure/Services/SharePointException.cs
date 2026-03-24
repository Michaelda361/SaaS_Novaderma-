namespace TalentManagement.Infrastructure.Services;

public class SharePointException(string message, int? statusCode = null)
    : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
