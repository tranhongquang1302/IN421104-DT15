using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Tuition;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/enrollments")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class TuitionController(RecordPaymentCommandHandler recordPayment, GetTuitionBalanceQueryHandler getBalance,
    IEnrollmentLookupQuery enrollmentLookup) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.ManageTuition)]
    [ProducesResponseType<IReadOnlyList<EnrollmentLookupDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentLookupDto>>> FindEnrollments(
        [FromQuery, Required, EmailAddress, StringLength(Student.MaxEmailLength)] string studentEmail, CancellationToken ct) =>
        Ok(await enrollmentLookup.FindByStudentEmailAsync(studentEmail, ct));

    [HttpPost("{id:guid}/payments")]
    [Authorize(Policy = Policies.ManageTuition)]
    [ProducesResponseType<PaymentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentDto>> RecordPayment(Guid id, RecordPaymentRequest request, CancellationToken ct)
    {
        var payment = await recordPayment.HandleAsync(new RecordPaymentCommand(id, request.Amount!.Value, request.RequestId), ct);
        return StatusCode(StatusCodes.Status201Created, payment);
    }

    [HttpGet("{id:guid}/balance")]
    [Authorize(Policy = Policies.ReadTuitionBalance)]
    [ProducesResponseType<TuitionBalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TuitionBalanceDto>> GetBalance(Guid id, CancellationToken ct) =>
        Ok(await getBalance.HandleAsync(new GetTuitionBalanceQuery(id), ct));
}

public record RecordPaymentRequest([Required] decimal? Amount, Guid? RequestId = null);
