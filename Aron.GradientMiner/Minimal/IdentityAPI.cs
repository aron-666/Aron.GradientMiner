using Aron.GradientMiner.Models;
using Aron.GradientMiner.Services;
using Aron.GradientMiner.Services.Identity;
using Aron.GradientMiner.ViewModels;
using Aron.NetCore.Util.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Aron.GradientMiner.Minimal
{
    public static class IdentityAPI
    {
        public static WebApplication AddIdentityAPI(this WebApplication app)
        {

            app.MapPost("/api/Identity/Login", async (HttpContext httpContext, IdentityService identityService) =>
            {
                var loginReq = await httpContext.Request.ReadFromJsonAsync<RequestResult<LoginReq>>(MyJsonContext.Default.RequestResultLoginReq.Options);
                var ret = identityService.Login(loginReq);
                var options = MyJsonContext.Default.ResponseResultLoginResp.Options;
                return Results.Json(ret, options);
            });


            app.MapDelete("/api/Identity/Logout", [Authorize] (IdentityService identityService) =>
            {
                var ret = identityService.Logout();
                var options = MyJsonContext.Default.ResponseResultString.Options;
                return Results.Json(ret, options);
            });

            return app;
        }
    }
}
