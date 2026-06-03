// <copyright file="CommunityPostInterest.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using Norge360.Community.Domain.Enums;
using Norge360.Entities;
namespace Norge360.Community.Domain.Entities; public sealed class CommunityPostInterest : AuditableEntity { public Guid PostId { get; set; } public Guid UserId { get; set; } public CommunityPostInterestType InterestType { get; set; } }
