using System.Diagnostics.CodeAnalysis;

namespace calledudeBot.Models;

public static class Result
{
	public static Result<T> Ok<T>(T value) => new(value, null, true);
	public static Result<T> Fail<T>(string error) => new(default, error, false);
}

public sealed record Result<T>(
	T? Value,
	string? Error,
	[property: MemberNotNullWhen(true, nameof(Result<T>.Value))]
	[property: MemberNotNullWhen(false, nameof(Result<T>.Error))]
	bool Success)
{
	public static implicit operator Result<T>(T value) => Result.Ok(value);
}
