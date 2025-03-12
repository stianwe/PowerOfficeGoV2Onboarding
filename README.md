# PowerOfficeGo Onboarding
This project provides functionality for onboarding new clients to your PowerOfficeGo integration using the OAuth2 onboarding flow.

## Using the library in your project
### Before you begin
To use this onboarding flow you need to have your url whitelisted by the PowerOffice team, separately for the live environment and the demo environment.

Send an email to [go-api@poweroffice.no](mailto:go-api@poweroffice.no) indicating whether you need your url whitelisted for the demo environment or the live environment.
The url you whitelist should be: <your_api_base_url>/PowerOfficeGoOnboarding/authenticate

It can be localhost for testing purposes.

### Configuring the api with dependency injection:

In your Program.cs:
```cs
using PowerOfficeGoV2.Extensions;
using PowerOfficeGoV2Onboarding.Extensions;

...

var builder = WebApplication.CreateBuilder(args); 
builder.Services.AddPowerOfficeGoApi(options => 
{
    // Use the PowerOfficeGo demo api (for demo clients). Default is false.
    options.UseDemoApi = true;
});
builder.Services.AddPowerOfficeGoOnboarding();

...
```

### Using the api
```cs
public class YourService
{
    private readonly IPowerOfficeGoV2OnboardingService _powerOfficeGoOnboardingService;
    
    public YourService(IPowerOfficeGoV2OnboardingService powerOfficeGoOnboardingService)
    {
        _powerOfficeGoOnboardingService = powerOfficeGoOnboardingService;
    }
    
    public async Task YourMethodAsync()
    {
        var response = await powerOfficeGoOnboardingService.BeginOnbardingAsync(
            "application_key",
            "subscription_key",
            "client_org_number",
            "https://your-api.com",
            "https://your-frontend.com/client-onboarding");
        
        // Redirect your user to response.RedirectUri
    }
}
```

### Handling completed onboarding sessions
When the onboarding session completes (succeeds or fails), the user will be redirected to the url you provided, https://your-frontend.com/client-onboarding in the above example.

#### Failure
If the onboarding failed, the errorMessage query parameter will be included.

For example: https://your-frontend.com/client-onboarding?errorMessage=IntegrationBlocked

#### Success
If the onboarding succeeds, the following query parameters will be included: clientKeys, clientNames, clientOrganizationNumbers, userEmail.

Notice that all values except userEmail can be comma separated lists, in case multiple clients were onboarded.

For example (but url encoded): [https://your-frontend.com/client-onboarding?clientKeys=f1ba4158-7bbc-4ecc-a68d-1a8ac42c5480,A34DA14A-9D3E-4736-9867-E3A23EE7EE7E&clientNames=ABC AS,XYZ AS&clientOrganizationNumbers=980386465,12345678&userEmail=jon.doe@company.no]()

#### Passing additional information
If you need to pass additional information to the frontend on redirect, you can do this by including query parameters in the redirect url.

For instance:

```cs
var response = await powerOfficeGoOnboardingService.BeginOnbardingAsync(
    "application_key",
    "subscription_key",
    "client_org_number",
    "https://your-api.com",
    "https://your-frontend.com/client-onboarding?myIdentifier=123");
```

The user will then be redirected to the following url when onboarding completes:
[https://your-frontend.com/client-onboarding?myIdentifier=123&clientKeys=...]()