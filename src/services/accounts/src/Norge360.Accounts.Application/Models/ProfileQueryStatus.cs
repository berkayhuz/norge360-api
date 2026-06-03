// <copyright file="ProfileQueryStatus.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

namespace Norge360.Accounts.Application.Models;

public enum ProfileQueryStatus
{
    Success = 0,
    NotFound = 1,
    ProvisioningPending = 2,
    Unauthorized = 3
}
