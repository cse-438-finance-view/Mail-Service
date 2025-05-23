using MailService.Services;

namespace MailService.Middleware
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFirebaseAuthService _firebaseAuthService;

        public FirebaseAuthMiddleware(RequestDelegate next, IFirebaseAuthService firebaseAuthService)
        {
            _next = next;
            _firebaseAuthService = firebaseAuthService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip auth for health check endpoints and swagger
            var path = context.Request.Path.Value?.ToLower();
            if (path == "/" || path.StartsWith("/swagger") || path.StartsWith("/openapi") || path == "/send-test-mail")
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing or invalid Authorization header");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var firebaseToken = await _firebaseAuthService.VerifyTokenAsync(token);
                
                // Add user information to context
                context.Items["UserId"] = firebaseToken.Uid;
                context.Items["UserEmail"] = firebaseToken.Claims.GetValueOrDefault("email");
                
                await _next(context);
            }
            catch (UnauthorizedAccessException)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or expired token");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Authentication error: {ex.Message}");
            }
        }
    }
} 