using EHR.Cqrs;
using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Application.Staff;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.IdentityService.Controllers;

[ApiController]
[Route("api/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public StaffController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost("invitations")]
    [Authorize(Policy = PlatformPermissions.StaffInvite)]
    public async Task<IActionResult> Invite(InviteStaffCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Created($"/api/staff/invitations/{result.Value!.InvitationToken}", result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.StaffManage)]
    public async Task<IActionResult> Create(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await _cqrs.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = staffUser.Id }, staffUser);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PlatformPermissions.StaffRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var staffUser = await _cqrs.QueryAsync(new GetStaffUserByIdQuery(id), cancellationToken);
        return staffUser is null ? NotFound() : Ok(staffUser);
    }
}
