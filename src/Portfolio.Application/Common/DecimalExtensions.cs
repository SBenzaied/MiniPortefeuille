namespace Portfolio.Application.Common;

public static class DecimalExtensions
{
    /// <summary>
    /// Retire les zéros non significatifs (20.000000 → 20). PostgreSQL renvoie un numeric(18,6)
    /// avec toutes ses décimales et SQLite avec au moins une : on expose la même valeur quel que soit le fournisseur.
    /// </summary>
    public static decimal Normalize(this decimal value) => value / 1.000000000000000000000000000000000m;
}
