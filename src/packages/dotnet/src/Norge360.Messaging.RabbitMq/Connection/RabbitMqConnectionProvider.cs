// <copyright file="RabbitMqConnectionProvider.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Norge360.Messaging.RabbitMq.Options;
using RabbitMQ.Client;

namespace Norge360.Messaging.RabbitMq.Connection;

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IAsyncDisposable, IDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private IConnection? connection;
    private bool disposed;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (connection?.IsOpen == true)
        {
            return connection;
        }

        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (connection?.IsOpen == true)
            {
                return connection;
            }

            var rabbitOptions = options.Value;
            var uri = new Uri(rabbitOptions.Uri);
            var factory = new ConnectionFactory
            {
                Uri = uri,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(rabbitOptions.NetworkRecoveryIntervalSeconds),
                ConsumerDispatchConcurrency = rabbitOptions.ConsumerDispatchConcurrency
            };

            if (uri.Scheme == "amqps" && !string.IsNullOrWhiteSpace(rabbitOptions.CaCertificatePath))
            {
                factory.Ssl.Enabled = true;
                factory.Ssl.ServerName = uri.Host;
                factory.Ssl.CertificateValidationCallback =
                    CreateCaCertificateValidationCallback(rabbitOptions.CaCertificatePath);
            }

            connection?.Dispose();
            connection = await factory.CreateConnectionAsync("Norge360", cancellationToken);
            return connection;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private static RemoteCertificateValidationCallback CreateCaCertificateValidationCallback(string caCertificatePath)
    {
        var caCertificate = X509Certificate2.CreateFromPemFile(caCertificatePath);
        return (_, certificate, _, sslPolicyErrors) =>
        {
            if (certificate is null ||
                sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable) ||
                sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                return false;
            }

            using var serverCertificate = new X509Certificate2(certificate);
            using var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    TrustMode = X509ChainTrustMode.CustomRootTrust
                }
            };

            chain.ChainPolicy.CustomTrustStore.Add(caCertificate);
            return chain.Build(serverCertificate);
        };
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        connection?.Dispose();
        connectionLock.Dispose();
        disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        connectionLock.Dispose();
        disposed = true;
    }
}
