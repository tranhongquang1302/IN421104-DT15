namespace LangCenter.API.Certificates;

public class CertificateLinks
{
    private readonly Uri _baseUrl;

    public CertificateLinks(string? publicBaseUrl)
    {
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var url) ||
            url.Scheme is not ("http" or "https") || url.Host.Length == 0 ||
            url.UserInfo.Length != 0 || url.Query.Length != 0 || url.Fragment.Length != 0)
        {
            throw new InvalidOperationException("Configure CertificateLinks:PublicBaseUrl as an HTTP(S) base URL.");
        }

        _baseUrl = new Uri(url.AbsoluteUri.TrimEnd('/') + '/');
    }

    public Uri GetVerificationUrl(string lookupCode) => new(_baseUrl, $"api/certificates/verify/{lookupCode}");

    public Uri GetPublicVerificationUrl(string lookupCode) => new(_baseUrl, $"certificates/verify/{lookupCode}");
}
