using calledudeBot.Bots;
using calledudeBot.Chat.Commands;
using calledudeBot.Chat.Info;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Chat
{
    public class CommandHandler<T> : INotificationHandler<T>
        where T : IMessage<T>
    {
        private readonly ILogger _logger;
        private readonly IMessageBot<T> _bot;
        private readonly Lazy<CommandContainer> _commandContainer;

        public CommandHandler(ILogger<CommandHandler<T>> logger, IMessageBot<T> bot, Lazy<CommandContainer> commandContainer)
        {
            _logger = logger;
            _bot = bot;
            _commandContainer = commandContainer;
        }

        public async Task Handle(T notification, CancellationToken cancellationToken)
        {
            var contentSplit = notification.Content.Split();
            if (!CommandUtils.IsCommand(contentSplit[0]))
                return;

            var param = new CommandParameter<T>(contentSplit, notification);
            var cmd = _commandContainer.Value.Commands.GetExistingCommand(param.PrefixedWords[0]);

            var response = await GetResponse(param, cmd);

            if (response == null)
            {
                _logger.LogWarning("Could not get a valid response from command {0}. Something might be configured incorrectly with it.", cmd!.Name);
                return;
            }

            await _bot.SendMessageAsync(notification.CloneWithMessage(response));
        }

        private async Task<string?> GetResponse(CommandParameter<T> param, Command? cmd)
        {
            if (cmd == null)
            {
                return "Not sure what you were trying to do? That is not an available command. Try '!help' or '!help <command>'";
            }
            else if (cmd.RequiresMod && !await param.SenderIsMod())
            {
                return "You're not allowed to use that command";
            }
            else //Get the appropriate response depending on command-type
            {
                _logger.LogInformation("Executing command: {0}", cmd.Name);
                return cmd switch
                {
                    SpecialCommand<CommandParameter> sp => await sp.Handle(param),
                    SpecialCommand s => await s.Handle(),
                    _ => cmd.Response,
                };
            }
        }
    }
}