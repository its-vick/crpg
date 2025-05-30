﻿using Crpg.Application.Clans.Commands;
using Crpg.Application.Common.Results;
using Crpg.Application.Common.Services;
using Crpg.Domain.Entities.Clans;
using Crpg.Domain.Entities.Users;
using Moq;
using NUnit.Framework;

namespace Crpg.Application.UTest.Clans;

public class KickClanMemberCommandTest : TestBase
{
    private static readonly Mock<IActivityLogService> ActivityLogService = new() { DefaultValue = DefaultValue.Mock };
    private static readonly Mock<IUserNotificationService> UserNotificationsService = new() { DefaultValue = DefaultValue.Mock };

    private static readonly IClanService ClanService = new ClanService(ActivityLogService.Object, UserNotificationsService.Object);

    [Test]
    public async Task ShouldLeaveClanIfUserKickedHimself()
    {
        Clan clan = new();
        User user = new() { ClanMembership = new ClanMember { Clan = clan, Role = ClanMemberRole.Member } };
        ArrangeDb.Users.Add(user);
        await ArrangeDb.SaveChangesAsync();

        var result = await new KickClanMemberCommand.Handler(ActDb, ClanService, ActivityLogService.Object, UserNotificationsService.Object).Handle(new KickClanMemberCommand
        {
            UserId = user.Id,
            ClanId = clan.Id,
            KickedUserId = user.Id,
        }, CancellationToken.None);

        Assert.That(result.Errors, Is.Null);
        Assert.That(AssertDb.ClanMembers, Has.Exactly(0)
            .Matches<ClanMember>(cm => cm.ClanId == clan.Id && cm.UserId == user.Id));
    }

    [TestCase(ClanMemberRole.Member, ClanMemberRole.Officer)]
    [TestCase(ClanMemberRole.Officer, ClanMemberRole.Leader)]
    public async Task ShouldNotKickUserIfHisRoleIsHigher(ClanMemberRole userRole, ClanMemberRole kickedUserRole)
    {
        Clan clan = new();
        User user = new() { ClanMembership = new ClanMember { Clan = clan, Role = userRole } };
        User kickedUser = new() { ClanMembership = new ClanMember { Clan = clan, Role = kickedUserRole } };
        ArrangeDb.Users.AddRange(user, kickedUser);
        await ArrangeDb.SaveChangesAsync();

        var result = await new KickClanMemberCommand.Handler(ActDb, ClanService, ActivityLogService.Object, UserNotificationsService.Object).Handle(new KickClanMemberCommand
        {
            UserId = user.Id,
            ClanId = clan.Id,
            KickedUserId = kickedUser.Id,
        }, CancellationToken.None);

        Assert.That(result.Errors, Is.Not.Null);
        Assert.That(result.Errors![0].Code, Is.EqualTo(ErrorCode.ClanMemberRoleNotMet));
    }

    [TestCase(ClanMemberRole.Officer, ClanMemberRole.Member)]
    [TestCase(ClanMemberRole.Leader, ClanMemberRole.Officer)]
    public async Task ShouldKickUser(ClanMemberRole userRole, ClanMemberRole kickedUserRole)
    {
        Clan clan = new();
        User user = new() { ClanMembership = new ClanMember { Clan = clan, Role = userRole } };
        User kickedUser = new() { ClanMembership = new ClanMember { Clan = clan, Role = kickedUserRole } };
        ArrangeDb.Users.AddRange(user, kickedUser);
        await ArrangeDb.SaveChangesAsync();

        var result = await new KickClanMemberCommand.Handler(ActDb, ClanService, ActivityLogService.Object, UserNotificationsService.Object).Handle(new KickClanMemberCommand
        {
            UserId = user.Id,
            ClanId = clan.Id,
            KickedUserId = kickedUser.Id,
        }, CancellationToken.None);

        Assert.That(result.Errors, Is.Null);
        Assert.That(AssertDb.ClanMembers, Has.Exactly(1)
            .Matches<ClanMember>(cm => cm.ClanId == clan.Id && cm.UserId == user.Id));
        Assert.That(AssertDb.ClanMembers, Has.Exactly(0)
            .Matches<ClanMember>(cm => cm.ClanId == clan.Id && cm.UserId == kickedUser.Id));
    }
}
