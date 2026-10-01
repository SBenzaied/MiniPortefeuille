using System.Globalization;
using System.Text;

namespace Portfolio.Mcp.Tools;

/// <summary>Libellés français des valeurs de l'API, identiques à ceux de l'interface web.</summary>
public static class Labels
{
    private static readonly Dictionary<string, string> RiskProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Prudent"] = "Prudent",
        ["Balanced"] = "Équilibré",
        ["Dynamic"] = "Dynamique",
    };

    private static readonly Dictionary<string, string> AssetClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Equities"] = "Actions",
        ["Bonds"] = "Obligations",
        ["Cash"] = "Liquidités",
        ["Other"] = "Autres",
    };

    public static string RiskProfile(string apiValue) => RiskProfiles.GetValueOrDefault(apiValue, apiValue);

    public static string AssetClass(string apiValue) => AssetClasses.GetValueOrDefault(apiValue, apiValue);

    /// <summary>
    /// Convertit un profil saisi par l'utilisateur (« équilibré », « Equilibre », « Balanced »…)
    /// en valeur de l'API. Retourne <c>null</c> si le profil est inconnu.
    /// </summary>
    public static string? ParseRiskProfile(string input)
    {
        var normalized = RemoveAccents(input.Trim());
        foreach (var (apiValue, label) in RiskProfiles)
        {
            if (string.Equals(normalized, apiValue, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, RemoveAccents(label), StringComparison.OrdinalIgnoreCase))
            {
                return apiValue;
            }
        }
        return null;
    }

    private static string RemoveAccents(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var kept = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(kept.ToArray()).Normalize(NormalizationForm.FormC);
    }
}
