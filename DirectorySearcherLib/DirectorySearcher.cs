using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DirectorySearcherLib
{
    public static class DirectorySearcher
    {
        public static string clientId = "0464982a-83bf-4361-b4e6-b13acf04d265";
        public static string commonAuthority = "https://login.microsoftonline.com/common/";
        public static Uri returnUri = new Uri("https://MyDirectorySearcherApp");        
        const string graphResourceUri = "https://graph.microsoft.com";
        public static string graphApiVersion = "2013-11-08";

        public static async Task<List<User>> SearchByAlias(string alias, IPlatformParameters parent) // add this param
        {
            AuthenticationResult authResult = null;
            JObject jResult = null;
            List<User> results = new List<User>();
            AuthenticationContext authContext = new AuthenticationContext(commonAuthority);

            try
            {
                // To avoid the user consent page, input the values for your registered application above,
                // comment out the if statement immediately below, and replace the commonAuthority parameter
                // with https://login.microsoftonline.com/common/<your.tenant.domain.com>
                if (authContext.TokenCache.ReadItems().Count() > 0)
                    authContext = new AuthenticationContext(authContext.TokenCache.ReadItems().First().Authority);
                authResult = await authContext.AcquireTokenSilentAsync(graphResourceUri, clientId);
            }
            catch(AdalSilentTokenAcquisitionException)
            {
                authResult = await authContext.AcquireTokenAsync(graphResourceUri, clientId, returnUri, parent);
            }
            catch (Exception ee)
            {
                results.Add(new User { error = ee.Message });
                return results;
            }

            try
            {
                string graphRequest = String.Format(CultureInfo.InvariantCulture, "{0}/v1.0/users?$filter=mailNickname eq '{3}'", graphResourceUri, authResult.TenantId, graphApiVersion, alias);
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, graphRequest);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                HttpResponseMessage response = await client.SendAsync(request);

                string content = await response.Content.ReadAsStringAsync();
                jResult = JObject.Parse(content);
            }
            catch (Exception ee)
            {
                results.Add(new User { error = ee.Message });
                return results;
            }

            if (jResult["odata.error"] != null)
            {
                results.Add(new User { error = (string)jResult["odata.error"]["message"]["value"] });
                return results;
            }
            if (jResult["value"] == null)
            {
                results.Add(new User { error = "Unknown Error." });
                return results;
            }
            foreach (JObject result in jResult["value"])
            {
                results.Add(new User
                {
                    displayName = (string)result["displayName"],
                    givenName = (string)result["givenName"],
                    surname = (string)result["surname"],
                    userPrincipalName = (string)result["userPrincipalName"],
                    telephoneNumber = (string)result["telephoneNumber"] == null ? "Not Listed." : (string)result["telephoneNumber"]
                });
            }

            return results;
        }
    }
}
