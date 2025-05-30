using System.Net;
using Crpg.Application.ActivityLogs.Models;
using Crpg.Application.Characters.Commands;
using Crpg.Application.Characters.Models;
using Crpg.Application.Characters.Queries;
using Crpg.Application.Clans.Models;
using Crpg.Application.Clans.Queries;
using Crpg.Application.Common.Results;
using Crpg.Application.Items.Commands;
using Crpg.Application.Items.Models;
using Crpg.Application.Items.Queries;
using Crpg.Application.Limitations.Models;
using Crpg.Application.Limitations.Queries;
using Crpg.Application.Notifications.Commands;
using Crpg.Application.Notifications.Models;
using Crpg.Application.Notifications.Queries;
using Crpg.Application.Restrictions.Models;
using Crpg.Application.Restrictions.Queries;
using Crpg.Application.Users.Commands;
using Crpg.Application.Users.Models;
using Crpg.Application.Users.Queries;
using Crpg.Domain.Entities.Servers;
using Crpg.Domain.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crpg.WebApi.Controllers;

[Authorize(Policy = UserPolicy)]
public class UsersController : BaseController
{
    /// <summary>
    /// Search user. <paramref name="name"/> or the pair (<paramref name="platform"/>, <paramref name="platformUserId"/>)
    /// should be not null.
    /// </summary>
    /// <query name="name">The user name.</query>
    /// <query name="platform">The user platform.</query>
    /// <query name="platformUserId">The user platform Id.</query>
    /// <returns>Found users. A limit of 10 records.</returns>
    /// <response code="200">Ok.</response>
    /// <response code="400">Bad Request.</response>
    [HttpGet("search")]
    [Authorize(Policy = ModeratorPolicy)]
    public async Task<ActionResult<Result<UserPrivateViewModel[]>>> SearchUsers(
        [FromQuery] Platform platform,
        [FromQuery] string? platformUserId,
        [FromQuery] string? name)
    {
        if (name != null)
        {
            return ResultToAction(await Mediator.Send(new GetUsersByNameQuery { Name = name }));
        }

        if (platformUserId != null)
        {
            var res = await Mediator.Send(new GetUserByPlatformIdQuery
            {
                Platform = platform,
                PlatformUserId = platformUserId,
            });
            return ResultToAction(res.Select(u => new[] { u }));
        }

        return ResultToAction(new Result<UserPrivateViewModel[]>(new Error(ErrorType.Validation, ErrorCode.InvalidField)));
    }

    /// <summary>
    /// Gets current user information.
    /// </summary>
    [HttpGet("self")]
    public Task<ActionResult<Result<UserViewModel>>> GetUser()
        => ResultToActionAsync(Mediator.Send(new GetUserQuery { UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Get user by id.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>The user.</returns>
    /// <response code="200">Ok.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">User was not found.</response>
    [HttpGet("{userId}")]
    [Authorize(Policy = ModeratorPolicy)]
    public Task<ActionResult<Result<UserPrivateViewModel>>> GetUserById([FromRoute] int userId)
    {
        return ResultToActionAsync(Mediator.Send(new GetUserByIdQuery { UserId = userId }));
    }

    /// <summary>
    /// Update the user note.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="user">The user note update.</param>
    /// <returns>The user's note updated.</returns>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("{userId}/note")]
    public Task<ActionResult<Result<UserPrivateViewModel>>> UpdateUserNote([FromRoute] int userId, [FromBody] UpdateUserNoteCommand user)
    {
        user = user with { UserId = userId };
        return ResultToActionAsync(Mediator.Send(user));
    }

    /// <summary>
    /// Gets all characters by user id.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>Characters list.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("{userId}/characters")]
    [Authorize(Policy = ModeratorPolicy)]
    public Task<ActionResult<Result<IList<CharacterViewModel>>>> GetUserCharactersListByUserId([FromRoute] int userId) =>
        ResultToActionAsync(Mediator.Send(new GetUserCharactersQuery { UserId = userId }));

    /// <summary>
    /// Get user by id.
    /// </summary>
    /// <param name="ids">The user ids.</param>
    /// <returns>The users.</returns>
    /// <response code="200">Ok.</response>
    /// <response code="400">Bad Request.</response>
    [HttpGet]
    [Authorize(Policy = ModeratorPolicy)]
    public Task<ActionResult<Result<IList<UserPrivateViewModel>>>> GetUsersById([FromQuery(Name = "id[]")] int[] ids)
    {
        return ResultToActionAsync(Mediator.Send(new GetUsersByIdQuery { UserIds = ids }));
    }

    /// <summary>
    /// Get all restrictions for a user.
    /// </summary>
    /// <param name="id">The user id.</param>
    /// <returns>The user restrictions.</returns>
    /// <response code="200">Ok.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">User was not found.</response>
    [HttpGet("{id}/restrictions")]
    public Task<ActionResult<Result<IList<RestrictionViewModel>>>> GetUserRestrictions([FromRoute] int id)
    {
        if (CurrentUser.User!.Role == Role.User && id != CurrentUser.User!.Id)
        {
            var res = new Result<IList<RestrictionViewModel>>(new Error(ErrorType.Forbidden, ErrorCode.UserRoleNotMet));
            return Task.FromResult(ResultToAction(res));
        }

        return ResultToActionAsync(Mediator.Send(new GetUserRestrictionsQuery
        {
            UserId = id,
        }));
    }

    /// <summary>
    /// Get active restriction for a user.
    /// </summary>
    /// <returns>The  active user restriction.</returns>
    /// <response code="200">Ok.</response>
    /// <response code="400">Bad Request.</response>
    [HttpGet("self/restriction")]
    public Task<ActionResult<Result<RestrictionPublicViewModel>>> GetUserRestriction()
    {
        return ResultToActionAsync(Mediator.Send(new GetUserRestrictionQuery { UserId = CurrentUser.User!.Id, }));
    }

    /// <summary>
    /// Update the current user.
    /// </summary>
    /// <param name="req">The user with the updated values.</param>
    /// <returns>The updated user.</returns>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self")]
    public Task<ActionResult<Result<UserViewModel>>> UpdateUser([FromBody] UpdateUserCommand req)
    {
        // ReSharper disable once WithExpressionModifiesAllMembers
        req = req with { UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Reward user.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="req">The reward.</param>
    /// <returns>The updated user.</returns>
    /// <response code="200">Done.</response>
    /// <response code="400">Bad Request.</response>
    [Authorize(AdminPolicy)]
    [HttpPut("{id}/rewards")]
    public Task<ActionResult<Result<UserViewModel>>> RewardUser([FromRoute] int id, [FromBody] RewardUserCommand req)
    {
        req = req with { UserId = id, ActorUserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Deletes current user.
    /// </summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("self")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<ActionResult> DeleteUser() =>
        ResultToActionAsync(Mediator.Send(new DeleteUserCommand { UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Gets the specified current user's character.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <response code="200">Ok.</response>
    /// <response code="404">Character not found.</response>
    [HttpGet("self/characters/{id}")]
    public Task<ActionResult<Result<CharacterViewModel>>> GetUserCharacter([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new GetUserCharacterQuery
        { CharacterId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Gets all current user's characters.
    /// </summary>
    /// <response code="200">Ok.</response>
    [HttpGet("self/characters")]
    public Task<ActionResult<Result<IList<CharacterViewModel>>>> GetUserCharactersList() =>
        ResultToActionAsync(Mediator.Send(new GetUserCharactersQuery { UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Updates a character for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <param name="req">The entire character with the updated values.</param>
    /// <returns>The updated character.</returns>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/characters/{id}")]
    public Task<ActionResult<Result<CharacterViewModel>>> UpdateCharacter([FromRoute] int id,
        [FromBody] UpdateCharacterCommand req)
    {
        req = req with { UserId = CurrentUser.User!.Id, CharacterId = id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Get character characteristics for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <returns>The character characteristics.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("self/characters/{id}/characteristics")]
    public Task<ActionResult<Result<CharacterCharacteristicsViewModel>>> GetCharacterCharacteristics([FromRoute] int id)
    {
        return ResultToActionAsync(Mediator.Send(new GetUserCharacterCharacteristicsQuery
        {
            UserId = CurrentUser.User!.Id,
            CharacterId = id,
        }));
    }

    /// <summary>
    /// Updates every character competitive rating.
    /// </summary>>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [Authorize(AdminPolicy)]
    [HttpPut("characters/competitive-ratings")]
    public Task UpdateEveryCharacterCompetitiveRating()
    {
        UpdateEveryCharacterCompetitiveRatingCommand cmd = new();
        return ResultToActionAsync(Mediator.Send(cmd));
    }

    /// <summary>
    /// Respecializes every character.
    /// </summary>>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [Authorize(AdminPolicy)]
    [HttpPut("characters/respecialize")]
    public Task RespecializeAllCharacters()
    {
        RespecializeAllCharactersCommand cmd = new();
        return ResultToActionAsync(Mediator.Send(cmd));
    }

    /// <summary>
    /// Updates character characteristics for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <param name="stats">The character characteristics with the updated values.</param>
    /// <returns>The updated character characteristics.</returns>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/characters/{id}/characteristics")]
    public Task<ActionResult<Result<CharacterCharacteristicsViewModel>>> UpdateCharacterCharacteristics([FromRoute] int id,
        [FromBody] CharacterCharacteristicsViewModel stats)
    {
        UpdateCharacterCharacteristicsCommand cmd = new()
        {
            UserId = CurrentUser.User!.Id,
            CharacterId = id,
            Characteristics = stats,
        };
        return ResultToActionAsync(Mediator.Send(cmd));
    }

    /// <summary>
    /// Convert character characteristics for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <param name="req">The conversion to perform.</param>
    /// <returns>The updated character characteristics.</returns>
    /// <response code="200">Conversion performed.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/characters/{id}/characteristics/convert")]
    public Task<ActionResult<Result<CharacterCharacteristicsViewModel>>> ConvertCharacterCharacteristics([FromRoute] int id,
        [FromBody] ConvertCharacterCharacteristicsCommand req)
    {
        req = req with { CharacterId = id, UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Get character items for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <returns>The character items.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("self/characters/{id}/items")]
    public Task<ActionResult<Result<IList<EquippedItemViewModel>>>> GetCharacterItems([FromRoute] int id)
    {
        return ResultToActionAsync(Mediator.Send(new GetUserCharacterItemsQuery
        {
            UserId = CurrentUser.User!.Id,
            CharacterId = id,
        }));
    }

    /// <summary>
    /// Updates a character's items for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <param name="req">Item slots that changed.</param>
    /// <returns>The updated character items.</returns>
    /// <response code="200">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/characters/{id}/items")]
    public Task<ActionResult<Result<IList<EquippedItemViewModel>>>> UpdateCharacterItems([FromRoute] int id,
        [FromBody] UpdateCharacterItemsCommand req)
    {
        req = req with { CharacterId = id, UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Activate/deactivate character.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <param name="req">Activation value.</param>
    /// <response code="204">Updated.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/characters/{id}/active")]
    public Task<ActionResult> ActivateCharacter([FromRoute] int id,
        [FromBody] ActivateCharacterCommand req)
    {
        req = req with { CharacterId = id, UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Get character statistics for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <returns>The character statistics.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("self/characters/{id}/statistics")]
    public Task<ActionResult<Result<Dictionary<GameMode, CharacterStatisticsViewModel>>>> GetCharacterStatistics([FromRoute] int id)
    {
        return ResultToActionAsync(Mediator.Send(new GetUserCharacterStatisticsQuery
        {
            UserId = CurrentUser.User!.Id,
            CharacterId = id,
        }));
    }

    /// <summary>
    /// Get character exp/gold stats for the current user.
    /// </summary>
    /// <param name="from">Start of the queried time period.</param>
    /// <param name="to">End of the queried time period. This parameter is optional.</param>
    /// <param name="id">Character id.</param>
    /// <returns>The character earning statistics.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("self/characters/{id}/earning-statistics")]
    public async Task<ActionResult<Result<IList<ActivityLogViewModel>>>> GetCharacterEarningStatistics(
        [FromQuery] DateTime from,
        [FromQuery] DateTime? to,
        [FromRoute] int id)
    {
        return ResultToAction(await Mediator.Send(new GetUserCharacterEarningStatisticsQuery
        {
            UserId = CurrentUser.User!.Id,
            CharacterId = id,
            From = from,
            To = to,
        }, CancellationToken.None));
    }

    /// <summary>
    /// Get character limitations for the current user.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <returns>The character limitations.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("self/characters/{id}/limitations")]
    public Task<ActionResult<Result<CharacterLimitationsViewModel>>> GetCharacterLimitations([FromRoute] int id)
    {
        return ResultToActionAsync(Mediator.Send(new GetCharacterLimitationsQuery
        {
            UserId = CurrentUser.User!.Id,
            CharacterId = id,
        }));
    }

    /// <summary>
    /// Resets a character rating.
    /// </summary>
    /// <param name="userId">User id of the character owner.</param>
    /// <param name="id">Character id.</param>
    /// <response code="200">Character rating reset.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Character not found.</response>
    [Authorize(AdminPolicy)]
    [HttpPut("{userId}/characters/{id}/retire")]
    public Task<ActionResult<Result<CharacterViewModel>>> ResetCharacterRating([FromRoute] int userId, [FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new ResetCharacterRatingCommand { CharacterId = id, UserId = userId }));

    /// <summary>
    /// Retires character.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <response code="200">Retired.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Character not found.</response>
    [HttpPut("self/characters/{id}/retire")]
    public Task<ActionResult<Result<CharacterViewModel>>> RetireCharacter([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new RetireCharacterCommand { CharacterId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Respecializes character.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <response code="200">Respecialized.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Character not found.</response>
    [HttpPut("self/characters/{id}/respecialize")]
    public Task<ActionResult<Result<CharacterViewModel>>> RespecializeCharacter([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new RespecializeCharacterCommand { CharacterId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Set the character as tournament character.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <returns>The updated character.</returns>
    /// <response code="200">Done.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/characters/{id}/tournament")]
    public Task<ActionResult<Result<CharacterViewModel>>> SetCharacterForTournament([FromRoute] int id)
    {
        return ResultToActionAsync(Mediator.Send(new SetCharacterForTournamentCommand
        {
            CharacterId = id,
            UserId = CurrentUser.User!.Id,
        }));
    }

    /// <summary>
    /// Reward character.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="characterId">Character id.</param>
    /// <param name="req">The reward.</param>
    /// <returns>The updated character.</returns>
    /// <response code="200">Done.</response>
    /// <response code="400">Bad Request.</response>
    [Authorize(AdminPolicy)]
    [HttpPut("{userId}/characters/{characterId}/rewards")]
    public Task<ActionResult<Result<CharacterViewModel>>> RewardCharacter([FromRoute] int userId,
        [FromRoute] int characterId, [FromBody] RewardCharacterCommand req)
    {
        req = req with { CharacterId = characterId, UserId = userId, ActorUserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Deletes the specified current user's character.
    /// </summary>
    /// <param name="id">Character id.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="404">Character not found.</response>
    [HttpDelete("self/characters/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<ActionResult> DeleteCharacter([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new DeleteCharacterCommand { CharacterId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Gets owned items.
    /// </summary>
    [HttpGet("self/items")]
    public Task<ActionResult<Result<IList<UserItemViewModel>>>> GetUserItems()
    {
        GetUserItemsQuery query = new() { UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(query));
    }

    /// <summary>
    /// Buys item for the current user.
    /// </summary>
    /// <param name="req">The item to buy.</param>
    /// <returns>The bought item.</returns>
    /// <response code="201">Bought.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Item was not found.</response>
    [HttpPost("self/items")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public Task<ActionResult<Result<UserItemViewModel>>> BuyItem([FromBody] BuyItemCommand req)
    {
        req = req with { UserId = CurrentUser.User!.Id };
        return ResultToCreatedAtActionAsync(nameof(GetUserItems), null, ui => new { id = ui.Id },
            Mediator.Send(req));
    }

    /// <summary>
    /// Reforge item.
    /// </summary>
    /// <param name="id">User item id.</param>
    /// <returns>The reforged item.</returns>
    /// <response code="200">Reforged.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/items/{id}/reforge")]
    public Task<ActionResult<Result<UserItemViewModel>>> ReforgeUpgradedUserItem([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new ReforgeUpgradedUserItemCommand { UserItemId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Repair item.
    /// </summary>
    /// <param name="id">User item id.</param>
    /// <returns>The repaired item.</returns>
    /// <response code="200">repaired.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/items/{id}/repair")]
    public Task<ActionResult<Result<UserItemViewModel>>> RepairUserItem([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new RepairUserItemCommand { UserItemId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Upgrade item.
    /// </summary>
    /// <param name="id">User item id.</param>
    /// <returns>The upgraded item.</returns>
    /// <response code="200">Upgraded.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/items/{id}/upgrade")]
    public Task<ActionResult<Result<UserItemViewModel>>> UpgradeUserItem([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new UpgradeUserItemCommand { UserItemId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Sells item for the current user.
    /// </summary>
    /// <param name="id">The id of the user item to sell.</param>
    /// <response code="204">Sold.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Item was not found.</response>
    [HttpDelete("self/items/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<ActionResult> SellUserItem([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new SellUserItemCommand { UserItemId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Gets user clan or null.
    /// </summary>
    [HttpGet("self/clan")]
    public Task<ActionResult<Result<UserClanViewModel>>> GetUserClan()
    {
        GetUserClanQuery req = new() { UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    [Authorize(AdminPolicy)]
    [HttpGet("/users/reward-recent")]
    public Task<ActionResult> RewardRecently()
    {
        return ResultToActionAsync(Mediator.Send(new RewardRecentUserCommand { }));
    }

    /// <summary>
    /// Gets user's notifications.
    /// </summary>
    /// <returns>The user's notifications.</returns>
    /// <response code="200">Ok.</response>
    [HttpGet("self/notifications")]
    public Task<ActionResult<Result<UserNotificationsWithDictViewModel>>> GetUserNotifications()
    {
        GetUserNotificationsQuery req = new() { UserId = CurrentUser.User!.Id };
        return ResultToActionAsync(Mediator.Send(req));
    }

    /// <summary>
    /// Read user's notification.
    /// </summary>
    /// <returns>The updated notification.</returns>
    /// <response code="200">Read.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Notification was not found.</response>
    [HttpPut("self/notifications/{id}")]
    public Task<ActionResult<Result<UserNotificationViewModel>>> UpdateUserNotification([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new ReadUserNotificationCommand { UserNotificationId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Read all user's notifications.
    /// </summary>
    /// <response code="204">Read.</response>
    /// <response code="400">Bad Request.</response>
    [HttpPut("self/notifications/readAll")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<ActionResult> ReadAllUserNotifications() =>
        ResultToActionAsync(Mediator.Send(new ReadAllUserNotificationCommand { UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Delete user's notification.
    /// </summary>
    /// <response code="204">Deleted.</response>
    /// <response code="400">Bad Request.</response>
    /// <response code="404">Notification was not found.</response>
    [HttpDelete("self/notifications/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<ActionResult> DeleteUserNotification([FromRoute] int id) =>
        ResultToActionAsync(Mediator.Send(new DeleteUserNotificationCommand { UserNotificationId = id, UserId = CurrentUser.User!.Id }));

    /// <summary>
    /// Delete all user's notifications.
    /// </summary>
    /// <response code="204">Deleted.</response>
    /// <response code="400">Bad Request.</response>
    [HttpDelete("self/notifications/deleteAll")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<ActionResult> DeleteAllUserNotifications() =>
        ResultToActionAsync(Mediator.Send(new DeleteAllUserNotificationsCommand { UserId = CurrentUser.User!.Id }));
}
