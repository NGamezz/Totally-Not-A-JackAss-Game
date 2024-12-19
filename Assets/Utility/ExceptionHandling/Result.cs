using System;
using System.Runtime.CompilerServices;

public readonly struct Result<T, TError>
{
    private readonly T _value;
    private readonly TError _error;

    private readonly bool _hasError;

    private Result(T value)
    {
        _value = value;
        _error = default;
        _hasError = true;
    }

    private Result(TError error)
    {
        _hasError = false;
        _error = error;
        _value = default;
    }

    public static Result<T, TError> Ok(T value) => new(value);
    public static Result<T, TError> Error(TError error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault() => _hasError ? default : _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) => _hasError ? defaultValue : _value;

    public static explicit operator T(Result<T, TError> result)
    {
        if (result.IsError)
            throw new Exception($"Unhandled error in result: {(object)result._error}");
        return result._value;
    }

    public bool IsOk => !_hasError;
    public bool IsError => _hasError;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwrap()
    {
        if (_hasError)
            throw new Exception($"Unhandled error in result: {_error}");
        return _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TError UnwrapError()
    {
        if (!_hasError)
            throw new Exception("Result is not an error");

        return _error;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure)
    {
        return _hasError ? onFailure(_error) : onSuccess(_value);
    }
}