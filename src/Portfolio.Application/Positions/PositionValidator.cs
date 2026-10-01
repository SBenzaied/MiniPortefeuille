using Portfolio.Domain.Entities;

namespace Portfolio.Application.Positions;

/// <summary>Données d'une position une fois validées et normalisées.</summary>
public sealed record ValidPosition(string Instrument, AssetClass AssetClass, decimal Quantity, decimal UnitPrice, string Currency);

public static class PositionValidator
{
    public const int InstrumentMaxLength = 100;
    public const int MaxDecimals = 6;

    /// <summary>Plafond de cohérence, aligné sur la précision numeric(18,6) en base.</summary>
    public const decimal MaxAmount = 999_999_999_999m;

    public static (ValidPosition? Value, IReadOnlyDictionary<string, string[]> Errors) Validate(
        PositionRequest? request, IReadOnlyCollection<string> knownCurrencies)
    {
        var errors = new Dictionary<string, List<string>>();
        void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                errors[field] = list = [];
            }
            list.Add(message);
        }

        if (request is null)
        {
            return (null, new Dictionary<string, string[]> { [""] = ["Le corps de la requête est vide."] });
        }

        var instrument = request.Instrument?.Trim();
        if (string.IsNullOrEmpty(instrument))
        {
            Add("instrument", "L'instrument est obligatoire.");
        }
        else if (instrument.Length > InstrumentMaxLength)
        {
            Add("instrument", $"L'instrument ne doit pas dépasser {InstrumentMaxLength} caractères.");
        }

        AssetClass assetClass = default;
        var assetClassName = request.AssetClass?.Trim();
        if (string.IsNullOrEmpty(assetClassName))
        {
            Add("assetClass", "La classe d'actifs est obligatoire.");
        }
        else if (!TryParseAssetClass(assetClassName, out assetClass))
        {
            Add("assetClass", $"Classe d'actifs inconnue. Valeurs acceptées : {string.Join(", ", Enum.GetNames<AssetClass>())}.");
        }

        ValidateAmount(request.Quantity, "quantity", "La quantité", feminine: true, Add);
        ValidateAmount(request.UnitPrice, "unitPrice", "Le prix unitaire", feminine: false, Add);

        var currency = request.Currency?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(currency))
        {
            Add("currency", "La devise est obligatoire.");
        }
        else if (!knownCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase))
        {
            Add("currency", $"Devise inconnue. Devises acceptées : {string.Join(", ", knownCurrencies.Order())}.");
        }

        if (errors.Count > 0)
        {
            return (null, errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        return (new ValidPosition(instrument!, assetClass, request.Quantity!.Value, request.UnitPrice!.Value, currency!),
            new Dictionary<string, string[]>());
    }

    private static void ValidateAmount(decimal? value, string field, string label, bool feminine, Action<string, string> add)
    {
        if (value is not { } v)
        {
            add(field, $"{label} est obligatoire.");
        }
        else if (v <= 0)
        {
            add(field, $"{label} doit être strictement positi{(feminine ? "ve" : "f")}.");
        }
        else if (v > MaxAmount)
        {
            add(field, $"{label} dépasse la valeur maximale autorisée.");
        }
        else if (decimal.Round(v, MaxDecimals) != v)
        {
            add(field, $"{label} accepte au plus {MaxDecimals} décimales.");
        }
    }

    // Enum.TryParse accepte aussi "1" ou "Equities, Bonds" : on n'accepte que les noms exacts.
    private static bool TryParseAssetClass(string value, out AssetClass assetClass)
    {
        var name = Enum.GetNames<AssetClass>().FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        assetClass = name is null ? default : Enum.Parse<AssetClass>(name);
        return name is not null;
    }
}
