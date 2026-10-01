namespace Portfolio.Api.Http;

public static class RateLimitPolicies
{
    /// <summary>Limite les tentatives de connexion par adresse IP (freine le bourrage d'identifiants).</summary>
    public const string Login = "login";
}
