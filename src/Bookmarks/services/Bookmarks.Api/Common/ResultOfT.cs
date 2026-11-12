namespace Bookmarks.Api.Common;

/// <summary>Outcome of a service call that carries a value on success.</summary>
/// <typeparam name="T">Type carried on success.</typeparam>
/// <remarks>
/// Instances are created through the factory methods on <see cref="Result"/>, so there is
/// exactly one place in the codebase that decides what a success or a failure looks like.
/// </remarks>
public sealed class Result<T> : Result
{
    /// <summary>The value, present only when <see cref="Result.IsSuccess"/> is true.</summary>
    public T? Value { get; }

    internal Result(bool isSuccess, T? value, string? error, ResultErrorKind errorKind)
        : base(isSuccess, error, errorKind) => Value = value;
}
