using EHR.AuditService.Application.AuditRecords;
using EHR.Cqrs;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.AuditService.Controllers;

[ApiController]
[Route("api/audit-events")]
public sealed class AuditEventsController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public AuditEventsController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.AuditWrite)]
    public async Task<IActionResult> Record(RecordAuditCommand command, CancellationToken cancellationToken)
    {
        var auditRecord = await _cqrs.SendAsync(command, cancellationToken);
        return Created($"/api/audit-events/{auditRecord.Id}", auditRecord);
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.AuditRead)]
    public async Task<IActionResult> List([FromQuery] string? tenantId, CancellationToken cancellationToken)
    {
        var records = await _cqrs.QueryAsync(new ListAuditRecordsQuery(tenantId), cancellationToken);
        return Ok(records);
    }
}
