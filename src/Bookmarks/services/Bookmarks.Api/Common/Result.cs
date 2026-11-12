namespace Bookmarks.Api.Common;

/// <summary>
/// Outcome of a service call that carries no value. Forces the caller to handle failure
/// instead of guessing what a null or a thrown exception meant.
/// </summary>
public class Result
{
    /// <summary>Whether the call succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Why the call failed, present only when <see cref="IsSuccess"/> is false.</summary>
    public string? Error { get; }

    /// <summary>What kind of failure occurred, so the caller can choose a status code.</summary>
    public ResultErrorKind ErrorKind { get; }

    private protected Result(bool isSuccess, string? error, ResultErrorKind errorKind)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorKind = errorKind;
    }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, null, ResultErrorKind.None);

    /// <summary>Creates a failed result.</summary>
    /// <param name="errorKind">What kind of failure occurred.</param>
    /// <param name="error">Why the call failed.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(ResultErrorKind errorKind, string error) =>
        new(false, error, errorKind);

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    /// <typeparam name="T">Type carried on success.</typeparam>
    /// <param name="value">The value to carry.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success<T>(T value) =>
        new(true, value, null, ResultErrorKind.None);

    /// <summary>Creates a failed result for an operation that would have carried a value.</summary>
    /// <typeparam name="T">Type that would have been carried on success.</typeparam>
    /// <param name="errorKind">What kind of failure occurred.</param>
    /// <param name="error">Why the call failed.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure<T>(ResultErrorKind errorKind, string error) =>
        new(false, default, error, errorKind);
}
