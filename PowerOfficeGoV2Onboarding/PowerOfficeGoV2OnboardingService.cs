using System.Web;
using Microsoft.Extensions.Logging;
using PowerOfficeGoV2;
using PowerOfficeGoV2.Api;
using PowerOfficeGoV2.Model;

namespace PowerOfficeGoV2Onboarding;

/// <summary>
/// Provides functionality for onboarding a new client to your PowerOfficeGo integration.
/// </summary>
public interface IPowerOfficeGoOnboardingService
{
    /// <summary>
    /// Initiates an onboarding session for the provided client org number.
    ///
    /// You need to redirect the user which initiates the client onboarding request to the RedirectUri returned in the result from this method.
    /// </summary>
    /// <remarks>
    /// To use anything other than localhost-based urls you need to get your url whitelisted by the PowerOfficeGo team by sending an email to: go-api@poweroffice.no.
    /// Ask to whitelist the following url: &lt;your_api_base_url&gt;/PowerOfficeGoOnboarding/authenticate
    /// </remarks>
    /// <param name="applicationKey">The integration application key</param>
    /// <param name="subscriptionKey">The integration subscription key</param>
    /// <param name="clientOrgNumber">The org number of the client to onboard</param>
    /// <param name="apiBaseUrl">The base url of your API</param>
    /// <param name="onOnboardingCompleteRedirectUrl">
    /// The user will be redirected to this url when onboarding succeeds or fails.
    /// On error, the errorMessage string query parameter will be provided.
    /// On success, the following query parameters will be provided (all strings): clientKeys, clientNames, clientOrganizationNumbers, userEmail.
    /// Notice that all values except userEmail can be comma separated lists, in case multiple clients were onboarded
    /// For example (but url encoded): &lt;...&gt;?clientKeys=f1ba4158-7bbc-4ecc-a68d-1a8ac42c5480,A34DA14A-9D3E-4736-9867-E3A23EE7EE7E&clientNames=ABC AS,XYZ AS&clientOrganizationNumbers=980386465,12345678&userEmail=jon.doe@company.no
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IOnboardingInitiatePostApiResponse> BeginOnbardingAsync(
        Guid applicationKey,
        string subscriptionKey,
        string clientOrgNumber,
        string apiBaseUrl,
        string onOnboardingCompleteRedirectUrl,
        CancellationToken cancellationToken = default);
}

public interface IPowerofficeGoOnboardingFinalizingService
{
    Task<string> FinalizeOnboardingAsync(Guid onboardingSessionToken, string? onboardingToken, string onboardingStatus, CancellationToken cancellationToken = default);
}

internal class PowerOfficeGoOnboardingService : IPowerOfficeGoOnboardingService, IPowerofficeGoOnboardingFinalizingService
{
    private record OnboardingSession(string SubscriptionKey, string RedirectUrl);
    private readonly Dictionary<Guid, OnboardingSession> _onboardingSessionTokenToSubscriptionKey = new();
    
    private readonly ILogger<PowerOfficeGoOnboardingService> _logger;
    private readonly IPowerOfficeGoApiService _apiService;

    public PowerOfficeGoOnboardingService(ILogger<PowerOfficeGoOnboardingService> logger, IPowerOfficeGoApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    public async Task<IOnboardingInitiatePostApiResponse> BeginOnbardingAsync(
        Guid applicationKey,
        string subscriptionKey,
        string clientOrgNumber,
        string apiBaseUrl,
        string onOnboardingCompleteRedirectUrl,
        CancellationToken cancellationToken = default)
    {
        var api = await GetApiOrThrowAsync(subscriptionKey, cancellationToken);
        var onboardingSessionToken = GetOnboardingSessionToken(subscriptionKey, onOnboardingCompleteRedirectUrl);
        return await api.OnboardingInitiatePostAsync(new InitiateOnboardingPostDto(
            applicationKey,
            clientOrgNumber,
            $"{apiBaseUrl}/{PowerOfficeGoOnboardingController.ControllerRoute}/{PowerOfficeGoOnboardingController.OnboardingCallbackRoute}?{PowerOfficeGoOnboardingController.OnboardingSessionTokenQueryParamName}={onboardingSessionToken}"),
            cancellationToken);
    }

    public async Task<string> FinalizeOnboardingAsync(Guid onboardingSessionToken, string? onboardingToken, string onboardingStatus, CancellationToken cancellationToken = default)
    {
        var onboardingSession = CompleteOnboardingSessionToken(onboardingSessionToken);
        
        if (!Enum.TryParse<OnboardingStatus>(onboardingStatus, out var status))
        {
            return ErrorRedirectUrl(onboardingSession.RedirectUrl, $"Unexpected onboarding status received from PowerOfficeGo: {onboardingStatus}");
        }
        
        if (status is not OnboardingStatus.Success)
        {
            return ErrorRedirectUrl(onboardingSession.RedirectUrl, $"Failure status received from PowerOfficeGo: {status}");
        }
        
        var api = await GetApiOrThrowAsync(onboardingSession.SubscriptionKey, cancellationToken);
        var result = await api.OnboardingFinalizePostAsync(new FinalizeOnboardingPostDto(onboardingToken), cancellationToken);
        if (result.TryOk(out var onboardingResult))
        {
            return SuccessRedirectUrl(onboardingSession.RedirectUrl, onboardingResult);
        }
        else
        {
            return ErrorRedirectUrl(onboardingSession.RedirectUrl, $"Failure while finalizing onboarding: {result.StatusCode}: {result.RawContent}");
        }
    }

    private static string UrlWithQuestionMark(string url)
        => url.Contains('?') ? $"{url}&" : $"{url}?";
    
    private static string ErrorRedirectUrl(string baseUrl, string errorMessage)
        => $"{UrlWithQuestionMark(baseUrl)}errorMessage={HttpUtility.UrlEncode(errorMessage)}";

    private static string SuccessRedirectUrl(string baseUrl, FinalizeOnboardingResponseDto response)
        => $"{UrlWithQuestionMark(baseUrl)}" +
           $"clientKeys={string.Join(",", response.OnboardedClientsInformation!.Select(x => x.ClientKey))}&" +
           $"clientNames={HttpUtility.UrlEncode(string.Join(",", response.OnboardedClientsInformation!.Select(x => x.ClientName)))}&" +
           $"clientOrganizationNumbers={string.Join(",", response.OnboardedClientsInformation!.Select(x => x.ClientOrganizationNumber))}&" +
           $"userEmail={response.UserEmail})";

    private async Task<OnboardingApi> GetApiOrThrowAsync(string subscriptionKey, CancellationToken cancellationToken)
    {
        var api = await _apiService.GetApiAsync<OnboardingApi>(null, subscriptionKey, cancellationToken);
        if (api is null)
        {
            throw new InvalidOperationException($"Could not resolve onboarding api ({nameof(OnboardingApi)}). Please make sure that you have set up everything correctly according to the README");
        }
        
        return api;
    }

    private Guid GetOnboardingSessionToken(string subscriptionKey, string onboardingCompleteRedirectUrl)
    {
        var token = Guid.NewGuid();
        lock (_onboardingSessionTokenToSubscriptionKey)
        {
            _onboardingSessionTokenToSubscriptionKey[token] = new OnboardingSession(subscriptionKey, onboardingCompleteRedirectUrl);
        }

        return token;
    }

    private OnboardingSession CompleteOnboardingSessionToken(Guid onboardingSessionToken)
    {
        lock (_onboardingSessionTokenToSubscriptionKey)
        {
            if (!_onboardingSessionTokenToSubscriptionKey.TryGetValue(onboardingSessionToken, out var onboardingSession))
            {
                throw new Exception($"Attempted to complete onboarding with unknown token: {onboardingSessionToken}");
            }

            return onboardingSession;
        }
    }
}