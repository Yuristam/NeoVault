using System.Globalization;
using Transfers.API.Enums;

namespace Transfers.API.Integration
{
    public class AccountsApiClient
    {
        public static string FormatAmount(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);

        public static async Task<StepResult> PostStepAsync(HttpClient httpClient, string url)
        {
            try
            {
                var response = await httpClient.PostAsync(url, null);

                return response.IsSuccessStatusCode 
                    ? StepResult.Ok 
                    : StepResult.Refused;
            }
            catch
            {
                return StepResult.Unknown;
            }
        }

        public static async Task<bool> IsPerformedAsync(HttpClient client, string key) =>
            (await client.GetAsync($"/operations/{key}")).IsSuccessStatusCode;
    }
}
