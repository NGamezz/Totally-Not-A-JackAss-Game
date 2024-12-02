using System;
using System.Runtime.CompilerServices;

public readonly struct Result<T, TError>
{
    private readonly T _value;
    private readonly TError _error;

    private Result(T value)
    {
        IsError = false;
        _value = value;
        _error = default;
    }

    private Result(TError error)
    {
        IsError = true;
        _value = default;
        _error = error;
    }

    public static Result<T, TError> Ok(T value) => new(value);
    public static Result<T, TError> Error(TError error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault() => !IsError ? _value : default;

    public static explicit operator T(Result<T, TError> result)
    {
        if (result.IsError)
            throw new Exception($"Unhandled error in result: {(object)result._error}");
        return result._value;
    }

    public bool IsOk => !IsError;
    public bool IsError { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Unwrap()
    {
        if (this.IsError)
            throw new Exception($"Unhandled error in result: {this._error}");
        return this._value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TError UnwrapError()
    {
        if (!IsError)
            throw new Exception("Result is not an error");

        return _error;
    }
}

//Records are Reference Types but have Value-type-like behaviour.
// public abstract record Result<T, TError> : IDisposable
// {
//     public sealed record Ok(T Value) : Result<T, TError>;
//
//     public sealed record Error(TError Err) : Result<T, TError>;
//
//     public T GetValueOrDefault()
//     {
//         return this switch
//         {
//             Ok({ } value) => value,
//             _ => default(T)!
//         };
//     }
//
//     public void Dispose()
//     {
//         switch (this)
//         {
//             case Ok(not null and IDisposable v):
//                 v.Dispose();
//                 break;
//         }
//     }
//
//     public static explicit operator T(Result<T, TError> result)
//     {
//         return result switch
//         {
//             Ok({ } value) => value,
//             Error({ } err) => throw new Exception($"Unhandled error in result: {err}"),
//             _ => throw new Exception("Unexpected exception")
//         };
//     }
// }