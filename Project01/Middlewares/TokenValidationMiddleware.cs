using Project01.Clients.AuthClients;
using Project01.Context;
using System.IdentityModel.Tokens.Jwt;

namespace Project01.Middlewares
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthClientService _authClient;
        

        public TokenValidationMiddleware(RequestDelegate next, AuthClientService authService)
        {
            _next = next;
            _authClient = authService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
            {
                var validate = await _authClient.AuthenticateToken(token);
                if (validate != null && validate.ResCode == 1) 
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(validate.ResMsg);
                    return;
                }
            }

            await _next(context);
        }
    }
}
