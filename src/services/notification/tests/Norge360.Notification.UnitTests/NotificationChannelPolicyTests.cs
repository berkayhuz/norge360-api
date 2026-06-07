// <copyright file="NotificationChannelPolicyTests.cs" company="Norge360">
// Copyright (c) 2026 Norge360. All rights reserved.
// Norge360 is proprietary software. See the LICENSE file in the repository root.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Norge360.Notification.Application.Abstractions;
using Norge360.Notification.Application.Services;
using Norge360.Notification.Contracts.Notifications;
using Norge360.Notification.Contracts.Notifications.Enums;
using Norge360.Notification.Contracts.Notifications.Models;
using Norge360.Notification.Contracts.Notifications.Requests;

namespace Norge360.Notification.UnitTests;

public sealed class NotificationChannelPolicyTests
{
    [Fact]
    public async Task ResolveChannelsAsync_ShouldPassNotificationTypeToPreferenceReader()
    {
        var userId = Guid.NewGuid();
        var reader = new Mock<IUserNotificationPreferenceReader>();
        reader.Setup(x => x.IsChannelEnabledAsync(
                userId,
                NotificationCategory.Community,
                NotificationTypes.PostLike,
                NotificationChannel.InApp,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var policy = new DefaultNotificationChannelPolicy(
            reader.Object,
            Mock.Of<ILogger<DefaultNotificationChannelPolicy>>());
        var request = new SendNotificationRequest(
            new NotificationRecipient(userId, null, null, null, null),
            [NotificationChannel.InApp],
            NotificationCategory.Community,
            NotificationPriority.Normal,
            "subject",
            "body",
            null,
            null,
            new Dictionary<string, string>
            {
                ["notificationType"] = NotificationTypes.PostLike
            },
            null,
            null);

        var channels = await policy.ResolveChannelsAsync(request, CancellationToken.None);

        channels.Should().ContainSingle().Which.Should().Be(NotificationChannel.InApp);
        reader.Verify(x => x.IsChannelEnabledAsync(
            userId,
            NotificationCategory.Community,
            NotificationTypes.PostLike,
            NotificationChannel.InApp,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
