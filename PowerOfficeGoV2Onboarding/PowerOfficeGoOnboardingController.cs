using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PowerOfficeGoV2Onboarding;

[AllowAnonymous]
[ApiController]
[Route(ControllerRoute)]
public class PowerOfficeGoOnboardingController : ControllerBase
{
    internal const string ControllerRoute = "PowerOfficeGoOnboarding";
    internal const string OnboardingCallbackRoute = "authenticate";
    internal const string OnboardingSessionTokenQueryParamName = "onboardingSessionToken";
    
    private readonly IPowerofficeGoOnboardingFinalizingService _onboardingFinalizer;

    public PowerOfficeGoOnboardingController(IPowerofficeGoOnboardingFinalizingService onboardingFinalizer)
    {
        _onboardingFinalizer = onboardingFinalizer;
    }
    
    [HttpGet(OnboardingCallbackRoute)]
    public async Task<IActionResult> Authenticate([FromQuery] string status, [FromQuery] string? token, [FromQuery] Guid onboardingSessionToken)
    {
        var redirectUrl = await _onboardingFinalizer.FinalizeOnboardingAsync(onboardingSessionToken, token, status);
        return Redirect(redirectUrl);
    }
}