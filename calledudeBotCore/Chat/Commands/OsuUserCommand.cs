using calledudeBot.Chat.Info;
using calledudeBot.Models;
using calledudeBot.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Commands;

public class OsuUserCommand : SpecialCommand<CommandParameter>
{
    private readonly IOsuUserService _osuUserService;
    private readonly ILogger<OsuUserCommand> _logger;

    public OsuUserCommand(IOsuUserService osuUserService, ILogger<OsuUserCommand> logger)
    {
        Name = "!osuLookup";
        Description = "Gives information about an osu! player";
        RequiresMod = false;

        _osuUserService = osuUserService;
        _logger = logger;
    }

    protected override async Task<string> HandleCommand(CommandParameter param)
    {
        var user = param.Words.FirstOrDefault();

        if (user is null)
            return "Username required.";

        OsuUser? osuUser;

        try
        {
            osuUser = await _osuUserService.GetOsuUser(user);
        }
        catch (Exception ex)
        {
            const string message = "An error occured while getting osu! user data.";
            _logger.LogError(ex, message);
            return $"{message} Try again later.";
        }

        if (osuUser is null)
            return $"Could not find user {user}";

        var accuracyFormatted = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", osuUser.Accuracy);
        var ppFormatted = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", osuUser.PP);

        return $"osu! player {osuUser.Username} - Lv. {osuUser.Level} has {ppFormatted}PP (Global: #{osuUser.Rank} - Country: #{osuUser.CountryRank}) with {accuracyFormatted}% accuracy";
    }
}
