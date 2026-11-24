using LangCenter.Application.Common;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Certificates;

public record GetCertificateQuery(Guid Id);

public class GetCertificateQueryHandler(ICertificateRepository certificates)
{
    public async Task<CertificateDto> HandleAsync(GetCertificateQuery query, CancellationToken ct = default)
    {
        var certificate = await certificates.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy chứng chỉ.");
        return CertificateDto.FromCertificate(certificate);
    }
}

public interface IQrCodeRenderer
{
    byte[] RenderPng(string value);
}
