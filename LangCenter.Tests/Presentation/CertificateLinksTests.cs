using LangCenter.API.Certificates;
using Xunit;

namespace LangCenter.Tests.Presentation;

public class CertificateLinksTests
{
    [Theory]
    [InlineData("https://example.test")]
    [InlineData("https://example.test/demo/")]
    public void GetVerificationUrl_WithConfiguredBase_PreservesBasePath(string baseUrl)
    {
        var links = new CertificateLinks(baseUrl);

        Assert.Equal($"{baseUrl.TrimEnd('/')}/api/certificates/verify/CODE", links.GetVerificationUrl("CODE").AbsoluteUri);
    }

    [Theory]
    [InlineData("relative")]
    [InlineData("ftp://example.test")]
    [InlineData("https://example.test?ignored=yes")]
    [InlineData("https://example.test#fragment")]
    [InlineData("https://example.test@other.example.test")]
    public void Create_WithInvalidBaseUrl_RejectsConfiguration(string baseUrl) =>
        Assert.Throws<InvalidOperationException>(() => new CertificateLinks(baseUrl));
}
