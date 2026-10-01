using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Portfolio.Api.Http;

public static class ValidationResponses
{
    public const string Title = "Les données envoyées ne sont pas valides.";

    /// <summary>
    /// Réponse quand le JSON lui-même ne peut pas être lu (ex. <c>"quantity": "abc"</c>).
    /// Le message par défaut d'ASP.NET Core expose le type .NET et la position dans le flux ;
    /// on le remplace par un message neutre rattaché au nom du champ (<c>$.quantity</c> → <c>quantity</c>).
    /// </summary>
    public static IActionResult FromInvalidModelState(ActionContext context)
    {
        var invalid = context.ModelState
            .Where(e => e.Value?.ValidationState == ModelValidationState.Invalid)
            .ToList();
        // Une erreur de lecture JSON ($...) entraîne aussi une erreur « corps requis » sur le
        // paramètre : on ne garde alors que les erreurs JSON, plus parlantes.
        var jsonErrors = invalid.Where(e => e.Key.StartsWith('$')).ToList();

        var errors = new Dictionary<string, string[]>();
        if (jsonErrors.Count > 0)
        {
            foreach (var (key, _) in jsonErrors)
            {
                if (key.StartsWith("$.", StringComparison.Ordinal))
                {
                    errors[key[2..]] = ["Valeur au format invalide."];
                }
                else
                {
                    errors[""] = ["Le corps de la requête n'est pas un JSON valide."];
                }
            }
        }
        else
        {
            foreach (var (key, entry) in invalid)
            {
                errors[key] = entry!.Errors.Select(e => e.ErrorMessage).ToArray();
            }
        }

        var problem = new ValidationProblemDetails(errors)
        {
            Title = Title,
            Status = StatusCodes.Status400BadRequest,
        };
        return new BadRequestObjectResult(problem);
    }
}
