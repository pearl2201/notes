using System.Web;

namespace NotesApi.Models.Settings;

public class AppSettings
{
    public const string SETTING_NAME = nameof(AppSettings);
    public string Secret { get; set; }

    // refresh token time to live (in days), inactive tokens are
    // automatically deleted from the database after this time
    public int RefreshTokenTTL { get; set; }

    public string ValidIssuer { get; set; }

    public string ValidAudience { get; set; }

    public string FrontendUrl { get; set; }

    public string FrontendUrlWelcome(string email, string code, string role) => BuildUrl(FrontendUrl, "/welcome", new Dictionary<string, string>()
        {
            { "email",email },
            { "role",role },
            {"code",code }
        });

    public string HeadDataEmail { get; set; }

    private static string BuildUrl(string baseUrl, string route, Dictionary<string, string> queryParameters)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        uriBuilder.Path = route;
        foreach (var kv in queryParameters)
        {
            query[kv.Key] = kv.Value;
        }
        uriBuilder.Query = query.ToString();
        return uriBuilder.Uri.ToString();
    }
}