using RoomHub.Api.Domain.Common;

namespace RoomHub.Api.Common.Http;

// Maps a domain Error to a ProblemDetails response (application/problem+json)
// with the HTTP status required by the API contract.
public static class ErrorResults
{
    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: error.Code);
    }
}
