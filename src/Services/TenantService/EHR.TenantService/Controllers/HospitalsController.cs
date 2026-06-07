using EHR.Cqrs;
using EHR.SharedKernel.Authorization;
using EHR.TenantService.Application.Hospitals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.TenantService.Controllers;

[ApiController]
[Route("api/hospitals")]
public sealed class HospitalsController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public HospitalsController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.TenantManage)]
    public async Task<IActionResult> Register(RegisterHospitalCommand command, CancellationToken cancellationToken)
    {
        var hospital = await _cqrs.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = hospital.Id }, hospital);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PlatformPermissions.TenantRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var hospital = await _cqrs.QueryAsync(new GetHospitalByIdQuery(id), cancellationToken);
        return hospital is null ? NotFound() : Ok(hospital);
    }
}
