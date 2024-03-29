﻿using calledudeBot.Chat.Commands;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Chat.Info;

public abstract class CommandParameter
{
	public List<string> PrefixedWords { get; }
	public IEnumerable<string> EnclosedWords { get; }
	public IEnumerable<string> Words { get; }

	protected CommandParameter(IEnumerable<string> param)
	{
		PrefixedWords = param
			.Where(x => x.IsCommand())
			.ToList();

		EnclosedWords = param
			.SkipWhile(x => !x.StartsWith('<'));

		Words = param
			.SkipWhile(x => x.IsCommand())
			.TakeWhile(x => !x.StartsWith('<'));
	}

	public abstract Task<bool> SenderIsMod();
	public abstract IMessage Message { get; }
}

public class CommandParameter<T> : CommandParameter, IRequest<T> where T : IMessage
{
	public override IMessage Message { get; }

	public CommandParameter(IEnumerable<string> param, T message) : base(param)
	{
		Message = message;
	}

	public override async Task<bool> SenderIsMod()
	{
		if (Message.Sender is null)
			return false;

		return await Message.Sender.IsModerator();
	}
}