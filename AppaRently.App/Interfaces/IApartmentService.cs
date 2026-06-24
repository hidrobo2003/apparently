using AppaRently.App.DTOs.Apartments;
using AppaRently.App.ServiceResponse;

namespace AppaRently.App.Interfaces;

public interface IApartmentService
{
    Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetAllAsync(ApartmentSearchRequest? request = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetByOwnerIdAsync(string ownerId, ApartmentSearchRequest? request = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetDeletedAsync(ApartmentSearchRequest? request = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<IEnumerable<ApartmentResponse>>> GetDeletedByOwnerIdAsync(string ownerId, ApartmentSearchRequest? request = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ApartmentResponse>> GetByIdAsync(Guid apartmentId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ApartmentResponse>> GetByIdByOwnerIdAsync(Guid apartmentId, string ownerId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ApartmentResponse>> CreateAsync(string ownerId, CreateApartmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ApartmentResponse>> UpdateAsync(Guid apartmentId, string ownerId, UpdateApartmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteAsync(Guid apartmentId, string ownerId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteAsSuperAdminAsync(Guid apartmentId, CancellationToken cancellationToken = default);
}
