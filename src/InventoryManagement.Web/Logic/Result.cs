namespace InventoryManagement.Web.Logic;

public abstract record Result<T>
{
    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fail(string Error) : Result<T>;

    public bool IsOk => this is Ok;
    public string Message => this is Fail f ? f.Error : "";
    public T Unwrap() => this is Ok o ? o.Value : throw new InvalidOperationException(Message);

    public Result<U> Map<U>(Func<T, U> f) => this switch
    {
        Ok o => new Result<U>.Ok(f(o.Value)),
        Fail x => new Result<U>.Fail(x.Error),
        _ => throw new InvalidOperationException()
    };

    public Result<U> Bind<U>(Func<T, Result<U>> f) => this switch
    {
        Ok o => f(o.Value),
        Fail x => new Result<U>.Fail(x.Error),
        _ => throw new InvalidOperationException()
    };
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => new Result<T>.Ok(value);
    public static Result<T> Fail<T>(string error) => new Result<T>.Fail(error);
}
