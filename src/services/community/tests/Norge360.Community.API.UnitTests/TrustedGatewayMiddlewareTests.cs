using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Norge360.AspNetCore.RequestContext;
using Norge360.AspNetCore.TrustedGateway.Abstractions;
using Norge360.AspNetCore.TrustedGateway.Models;
using Norge360.AspNetCore.TrustedGateway.Options;
using Norge360.Community.API.Middlewares;
using Xunit;

namespace Norge360.Community.API.UnitTests;

public sealed class TrustedGatewayMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesCorrelationHeader_ForValidationCanonicalContract()
    {
        const string correlationId = "corr-test-123";
        string? observedCorrelationId = null;

        var validator = new Mock<ITrustedGatewayRequestValidator>();
        validator
            .Setup(x => x.ValidateAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<HttpContext, string, CancellationToken>((_, cid, _) => observedCorrelationId = cid)
            .ReturnsAsync(TrustedGatewayValidationResult.Success());

        var middleware = new TrustedGatewayMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new TrustedGatewayOptions { RequireTrustedGateway = true }),
            validator.Object,
            NullLogger<TrustedGatewayMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[RequestContextSupport.CorrelationIdHeaderName] = correlationId;

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, observedCorrelationId);
    }
}
