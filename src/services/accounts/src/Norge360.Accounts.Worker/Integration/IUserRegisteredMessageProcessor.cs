// <copyright file="IUserRegisteredMessageProcessor.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Worker.Integration;

public interface IUserRegisteredMessageProcessor
{
    Task<UserRegisteredProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> body,
        UserRegisteredMessageMetadata metadata,
        CancellationToken cancellationToken);
}
