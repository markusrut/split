using Microsoft.AspNetCore.Mvc;
using MiniValidation;
using Splittat.API.Models.Requests;
using Splittat.API.Services;

namespace Splittat.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithOpenApi()
            .Produces<Models.Responses.AuthResponse>(StatusCodes.Status201Created)
            .Produces<object>(StatusCodes.Status400BadRequest);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithOpenApi()
            .Produces<Models.Responses.AuthResponse>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        [FromServices] AuthService authService)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var (success, error, response) = await authService.RegisterAsync(request);

        if (!success)
        {
            return Results.BadRequest(new { error });
        }

        return Results.Created($"/api/auth/user/{response!.UserId}", response);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] AuthService authService)
    {
        if (!MiniValidator.TryValidate(request, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var (success, error, response) = await authService.LoginAsync(request);

        if (!success)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(response);
    }
}
