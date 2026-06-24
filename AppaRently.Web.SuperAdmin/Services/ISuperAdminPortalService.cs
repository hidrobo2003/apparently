using AppaRently.Web.SuperAdmin.Models;

namespace AppaRently.Web.SuperAdmin.Services;

public interface ISuperAdminPortalService
{
    Task<SuperAdminUserDashboardViewModel> BuildUserDashboardAsync(
        SuperAdminUserDashboardQueryViewModel query,
        CancellationToken cancellationToken = default);

    Task<SuperAdminApartmentDashboardViewModel> BuildApartmentDashboardAsync(
        SuperAdminApartmentDashboardQueryViewModel query,
        CancellationToken cancellationToken = default);

    Task<SuperAdminUserDetailViewModel?> BuildUserDetailAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<SuperAdminApartmentDetailViewModel?> BuildApartmentDetailAsync(
        Guid apartmentId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    Task<SuperAdminProfileViewModel?> BuildProfileAsync(
        string superAdminId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnotherSuperAdminAsync(
        string currentUserId,
        CancellationToken cancellationToken = default);
}
