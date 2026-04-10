namespace Solodoc.Api.Helpers;

public static class ApiResponses
{
    public static IResult ValidationError(string field, string message)
        => Results.BadRequest(new { error = message, field });

    public static IResult NotFoundError(string entityType)
        => Results.NotFound(new { error = $"{entityType} ble ikke funnet." });

    public static IResult ForbiddenError()
        => Results.Json(new { error = "Du har ikke tilgang til denne handlingen." }, statusCode: 403);

    public static IResult ConflictError(string message)
        => Results.Conflict(new { error = message });
}
