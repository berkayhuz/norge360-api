// <copyright file="MessagingUserDevice.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.MessagingService.Domain.Entities;

public sealed class MessagingUserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string PublicIdentityKey { get; set; } = string.Empty;
    public string SignedPreKey { get; set; } = string.Empty;
    public string SignedPreKeySignature { get; set; } = string.Empty;
    public string? OneTimePreKeysJson { get; set; }
    public string SupportedAlgorithms { get; set; } = "x25519-ed25519-xchacha20-poly1305";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}
