namespace Portfolio.Application.Abstractions;

/// <summary>
/// Conseiller à l'origine de la requête. L'implémentation HTTP (Portfolio.Api) le lit dans le
/// claim <c>sub</c> du jeton : c'est la seule source de son identité, jamais l'URL ni le corps.
/// </summary>
public interface ICurrentAdvisor
{
    /// <exception cref="InvalidOperationException">Aucun conseiller authentifié valide.</exception>
    int Id { get; }
}
