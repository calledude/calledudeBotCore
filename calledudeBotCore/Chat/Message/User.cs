using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace calledudeBot.Chat;

public sealed class User
{
	public string Name { get; }

	private readonly AsyncLazy<bool> _isModerator;

	public User(string userName, bool isMod = false)
	{
		Name = userName;
		_isModerator = new AsyncLazy<bool>(() => Task.FromResult(isMod));
	}

	public User(string userName, Func<Task<bool>> isModFunc)
	{
		Name = userName;
		_isModerator = new AsyncLazy<bool>(isModFunc);
	}

	public async Task<bool> IsModerator()
		=> await _isModerator;
}

public static class UserExtensions
{
	public static string CapitalizeUsername(this User user)
	=> char.ToUpper(user.Name[0]) + user.Name[1..];
}