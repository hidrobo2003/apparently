using AppaRently.Web.Owner.Models;

namespace AppaRently.Web.Owner.Services;

public interface IOwnerPortalService
{
    Task<OwnerDashboardViewModel> BuildDashboardAsync(
        string ownerId,
        OwnerDashboardQueryViewModel query,
        CancellationToken cancellationToken = default);

    Task<OwnerApartmentDetailViewModel?> BuildApartmentDetailAsync(
        string ownerId,
        Guid apartmentId,
        OwnerReportPeriodViewModel query,
        CancellationToken cancellationToken = default);

    Task<OwnerProfileViewModel?> BuildProfileAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportDashboardAsync(
        string ownerId,
        OwnerDashboardQueryViewModel query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportApartmentAsync(
        string ownerId,
        Guid apartmentId,
        OwnerReportPeriodViewModel query,
        CancellationToken cancellationToken = default);
}
