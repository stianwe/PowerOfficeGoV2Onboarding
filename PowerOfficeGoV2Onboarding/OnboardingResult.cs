using PowerOfficeGoV2.Model;

namespace PowerOfficeGoV2Onboarding;

/// <summary>
/// The result of an onboarding session.
/// </summary>
/// <param name="OnboardingResponse">The onboarding response, if successful</param>
/// <param name="ErrorMessage">The error message, in case of failure</param>
public record OnboardingResult(FinalizeOnboardingResponseDto? OnboardingResponse, string? ErrorMessage);