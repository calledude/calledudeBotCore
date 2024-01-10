using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IMessageDispatcher
{
	Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}

public class MessageDispatcher : IMessageDispatcher
{
	private readonly ILogger<MessageDispatcher> _logger;
	private readonly IMediator _mediator;

	public MessageDispatcher(ILogger<MessageDispatcher> logger, IMediator mediator)
	{
		_logger = logger;
		_mediator = mediator;
	}

	public async Task PublishAsync(INotification notification, CancellationToken cancellationToken = default)
	{
		var notificationType = notification.GetType().GetFriendlyName();
		_logger.LogInformation("Beginning to publish a {notificationType} message", notificationType);

		try
		{
			await _mediator.Publish(notification, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An exception was thrown in the MediatR adapter");
		}

		_logger.LogInformation("Finished invoking {notificationType} handlers", notificationType);
	}
}

public static class TypeExtensions
{
	private static readonly Dictionary<Type, string> _primitiveTypeNames = new()
	{
		{ typeof(string), "string" },
		{ typeof(object), "object" },
		{ typeof(bool), "bool" },
		{ typeof(byte), "byte" },
		{ typeof(char), "char" },
		{ typeof(decimal), "decimal" },
		{ typeof(double), "double" },
		{ typeof(short), "short" },
		{ typeof(int), "int" },
		{ typeof(long), "long" },
		{ typeof(sbyte), "sbyte" },
		{ typeof(float), "float" },
		{ typeof(ushort), "ushort" },
		{ typeof(uint), "uint" },
		{ typeof(ulong), "ulong" },
		{ typeof(void), "void" }
	};

	public static string GetFriendlyName(this Type type)
	{
		if (type.IsPointer)
			return type.GetElementType()!.GetFriendlyName() + "*";

		if (type.IsArray)
			return type.GetElementType()!.GetFriendlyName() + "[]";

		if (!type.IsGenericType)
		{
			if (_primitiveTypeNames.TryGetValue(type, out var primitiveName))
				return primitiveName;

			return type.Name;
		}

		var genericArguments = type.GetGenericArguments();

		var tick = type.Name.IndexOf('`');
		var name = type.Name.Remove(tick);

		var arguments = "<" + string.Join(", ", genericArguments.Select(x => x.GetFriendlyName())) + ">";
		return name.Insert(tick, arguments);
	}
}