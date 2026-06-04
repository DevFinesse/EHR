using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.SharedKernel.Authorization;

namespace EHR.AppointmentService.Application.Appointments;

public sealed class GetAppointmentByIdHandler : IQueryHandler<GetAppointmentByIdQuery, Appointment?>
{
    private readonly IAppointmentRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public GetAppointmentByIdHandler(IAppointmentRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Appointment?> HandleAsync(GetAppointmentByIdQuery query, CancellationToken cancellationToken)
    {
        var appointment = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (appointment is not null)
        {
            _tenantAuthorization.EnsureCanAccessTenant(appointment.TenantId);
        }

        return appointment;
    }
}
