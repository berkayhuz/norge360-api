// <copyright file="TrustedDeviceRepository.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Norge360.Auth.Application.Abstractions;
using Norge360.Auth.Domain.Entities;
using Norge360.Auth.Infrastructure.Persistence;

namespace Norge360.Auth.Infrastructure.Services;

public sealed class TrustedDeviceRepository(AuthDbContext dbContext) : ITrustedDeviceRepository
{
    public async Task<IReadOnlyCollection<TrustedDevice>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Set<TrustedDevice>()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.LastSeenAtUtc ?? x.TrustedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<TrustedDevice?> FindActiveByFingerprintAsync(Guid userId, string deviceFingerprintHash, CancellationToken cancellationToken) =>
        dbContext.Set<TrustedDevice>().SingleOrDefaultAsync(
            x => x.UserId == userId && !x.IsDeleted && x.RevokedAtUtc == null && x.DeviceFingerprintHash == deviceFingerprintHash,
            cancellationToken);

    public Task AddAsync(TrustedDevice device, CancellationToken cancellationToken) =>
        dbContext.Set<TrustedDevice>().AddAsync(device, cancellationToken).AsTask();

    public async Task<bool> RevokeAsync(Guid userId, Guid deviceId, DateTime utcNow, string reason, CancellationToken cancellationToken)
    {
        var device = await dbContext.Set<TrustedDevice>().SingleOrDefaultAsync(
            x => x.UserId == userId && x.Id == deviceId && !x.IsDeleted,
            cancellationToken);

        if (device is null)
        {
            return false;
        }

        if (!device.IsRevoked)
        {
            device.Revoke(utcNow, reason);
        }

        return true;
    }
}
