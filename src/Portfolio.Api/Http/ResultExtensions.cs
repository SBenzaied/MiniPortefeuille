using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common;

namespace Portfolio.Api.Http;

public static class ResultExtensions
{
    /// <summary>
    /// Traduit le résultat d'un cas d'usage en réponse HTTP : 404 en ProblemDetails, 400 en
    /// ValidationProblemDetails (erreurs par champ, exploitées telles quelles par le front).
    /// </summary>
    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, ActionResult> onSuccess) =>
        result.Status switch
        {
            ResultStatus.Ok => onSuccess(result.Value!),
            ResultStatus.NotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Ressource introuvable."),
            ResultStatus.Invalid => controller.ValidationProblem(title: ValidationResponses.Title, modelStateDictionary: ToModelState(result.Errors)),
            _ => throw new InvalidOperationException($"Statut inattendu : {result.Status}."),
        };

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> errors)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }
        return modelState;
    }
}
