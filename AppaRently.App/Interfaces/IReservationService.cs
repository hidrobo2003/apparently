using AppaRently.App.DTOs.Reservations;
using AppaRently.App.ServiceResponse;

namespace AppaRently.App.Interfaces;

public interface IReservationService
{
    Task<ServiceResponse<IEnumerable<ReservationResponse>>> GetAllAsync(ReservationSearchRequest? request = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ReservationResponse>> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ReservationResponse>> CreateAsync(string userId, CreateReservationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ReservationResponse>> UpdateAsync(Guid reservationId, string userId, UpdateReservationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteAsync(Guid reservationId, string userId, CancellationToken cancellationToken = default);
}
