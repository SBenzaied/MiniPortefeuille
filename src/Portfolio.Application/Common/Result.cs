namespace Portfolio.Application.Common;

public enum ResultStatus
{
    Ok,
    NotFound,
    Invalid,
}

/// <summary>
/// Issue d'un cas d'usage. Les cas attendus (ressource absente ou hors périmètre, entrée invalide)
/// sont des valeurs de retour et non des exceptions ; la couche API les traduit en codes HTTP.
/// </summary>
public sealed class Result<T>
{
    private Result(ResultStatus status, T? value, IReadOnlyDictionary<string, string[]>? errors)
    {
        Status = status;
        Value = value;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public ResultStatus Status { get; }
    public T? Value { get; }

    /// <summary>Erreurs de validation par champ (noms en camelCase, comme dans le JSON).</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static Result<T> Ok(T value) => new(ResultStatus.Ok, value, null);
    public static Result<T> NotFound() => new(ResultStatus.NotFound, default, null);
    public static Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors) => new(ResultStatus.Invalid, default, errors);
}

/// <summary>Résultat sans valeur, pour les opérations comme la suppression.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
