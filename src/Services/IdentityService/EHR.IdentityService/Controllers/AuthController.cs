using EHR.Cqrs;
using EHR.IdentityService.Application.Auth;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public AuthController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshAccessTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("invitations/accept")]
    public async Task<IActionResult> AcceptInvitation(AcceptStaffInvitationCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("mfa/{staffUserId:guid}/enable")]
    [Authorize(Policy = PlatformPermissions.StaffManage)]
    public async Task<IActionResult> EnableMfa(Guid staffUserId, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(new EnableMfaCommand(staffUserId), cancellationToken);
        return result.IsSuccess ? Ok(new { enabled = result.Value }) : NotFound(new { error = result.Error });
    }

    [HttpPost("mfa/{staffUserId:guid}/setup")]
    [Authorize(Policy = PlatformPermissions.StaffManage)]
    public async Task<IActionResult> SetupMfa(Guid staffUserId, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(new SetupMfaCommand(staffUserId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(ResetPasswordRequestCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(new { reset = result.Value }) : BadRequest(new { error = result.Error });
    }
}
