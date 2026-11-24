using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace LangCenter.Tests.Support;

public static partial class PortalForms
{
    public static async Task<string> ReadHtmlAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    public static async Task<string> GetPageAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadHtmlAsync(response);
    }

    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string formHtml,
        Dictionary<string, string>? fields = null)
    {
        fields ??= [];
        var token = AntiforgeryInput().Match(formHtml);
        Assert.True(token.Success, "Expected an antiforgery token in the rendered Razor form.");
        fields["__RequestVerificationToken"] = token.Groups[1].Value;
        return client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    public static async Task LoginAsync(HttpClient client, string email, string? returnUrl = null)
    {
        var login = await GetPageAsync(client, "/login");
        using var response = await PostAsync(client, "/login", login, new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = AuthenticatedApiFactory.Password,
            ["ReturnUrl"] = returnUrl ?? string.Empty
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/workspace", response.Headers.Location?.OriginalString);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex AntiforgeryInput();
}
