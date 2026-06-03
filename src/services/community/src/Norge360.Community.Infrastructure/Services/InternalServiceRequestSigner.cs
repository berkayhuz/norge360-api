namespace Norge360.Community.Infrastructure.Services;

public interface IInternalServiceRequestSigner
{
    ValueTask SignAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

internal sealed class NoOpInternalServiceRequestSigner : IInternalServiceRequestSigner
{
    public ValueTask SignAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
