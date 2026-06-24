using AppaRently.App.DTOs.Apartments;
using AppaRently.App.DTOs.Favorites;
using AppaRently.App.DTOs.Reservations;
using AppaRently.App.Interfaces;
using AppaRently.Web.Client.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Client.Controllers;

public class ApartmentController : ClientControllerBase
{
    private const int CalendarWindowDays = 30;

    private readonly IApartmentService _apartmentService;
    private readonly IFavoriteService _favoriteService;
    private readonly IReservationService _reservationService;

    public ApartmentController(
        IApartmentService apartmentService,
        IFavoriteService favoriteService,
        IReservationService reservationService)
    {
        _apartmentService = apartmentService;
        _favoriteService = favoriteService;
        _reservationService = reservationService;
    }

    public async Task<IActionResult> Index([FromQuery] ApartmentDashboardViewModel? request)
    {
        var filters = request?.Filters ?? new ApartmentSearchRequest();
        var response = await _apartmentService.GetAllAsync(filters);
        var model = new ApartmentDashboardViewModel
        {
            Filters = filters,
            Apartments = response.Data?.ToList() ?? new List<ApartmentResponse>()
        };

        return View(model);
    }

    public Task<IActionResult> Show(Guid id) => Detail(id);

    public async Task<IActionResult> Detail(Guid id)
    {
        var apartmentResponse = await _apartmentService.GetByIdAsync(id);
        if (!apartmentResponse.Success || apartmentResponse.Data is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var apartment = apartmentResponse.Data;
        var reservationsResponse = await _reservationService.GetAllAsync(new ReservationSearchRequest
        {
            ApartmentId = id
        });

        var favoritesResponse = await _favoriteService.GetAllAsync(new FavoriteSearchRequest
        {
            ApartmentId = id
        });

        var currentUserId = GetCurrentUserId();
        var isFavorited = false;

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            var currentFavoriteResponse = await _favoriteService.GetAllAsync(new FavoriteSearchRequest
            {
                UserId = currentUserId,
                ApartmentId = id
            });

            isFavorited = currentFavoriteResponse.Data?.Any() == true;
        }

        var model = new ApartmentDetailViewModel
        {
            Apartment = apartment,
            FavoriteCount = favoritesResponse.Data?.Count() ?? 0,
            IsFavorited = isFavorited,
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            CalendarDays = BuildCalendar(reservationsResponse.Data ?? new List<ReservationResponse>())
        };

        return View(model);
    }

    private static IReadOnlyList<ApartmentCalendarDayViewModel> BuildCalendar(IEnumerable<ReservationResponse> reservations)
    {
        var bookedRanges = reservations
            .Where(x => x.DeletedAt is null)
            .Select(x => (Start: x.CheckIn.Date, End: x.CheckOut.Date))
            .ToList();

        var startDate = DateTime.Today;
        var calendar = new List<ApartmentCalendarDayViewModel>(CalendarWindowDays);

        for (var index = 0; index < CalendarWindowDays; index++)
        {
            var date = startDate.AddDays(index);
            var isBooked = bookedRanges.Any(range => date >= range.Start && date < range.End);

            calendar.Add(new ApartmentCalendarDayViewModel
            {
                Date = date,
                IsBooked = isBooked,
                IsToday = date.Date == DateTime.Today
            });
        }

        return calendar;
    }
}
