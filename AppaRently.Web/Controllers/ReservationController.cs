using AppaRently.App.DTOs.Reservations;
using AppaRently.App.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Client.Controllers;

public class ReservationController : ClientControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _reservationService.GetAllAsync(new ReservationSearchRequest
        {
            UserId = userId
        });

        var reservations = response.Data?
            .OrderByDescending(x => x.CreatedAt)
            .ToList() ?? new List<ReservationResponse>();

        return View(reservations);
    }

    public async Task<IActionResult> Show(Guid id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _reservationService.GetByIdAsync(id);
        if (!response.Success || response.Data is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(response.Data.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return View(response.Data);
    }

    public IActionResult Create(Guid apartmentId, DateTime? checkIn = null, DateTime? checkOut = null)
    {
        if (string.IsNullOrWhiteSpace(GetCurrentUserId()))
        {
            return RedirectToLogin();
        }

        return View(new CreateReservationRequest
        {
            ApartmentId = apartmentId,
            CheckIn = checkIn ?? DateTime.Today.AddDays(1),
            CheckOut = checkOut ?? DateTime.Today.AddDays(2)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Storage(CreateReservationRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _reservationService.CreateAsync(userId, request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View("Create", request);
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction("Detail", "Apartment", new { id = request.ApartmentId });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _reservationService.GetByIdAsync(id);
        if (!response.Success || response.Data is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(response.Data.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return View(new UpdateReservationRequest
        {
            CheckIn = response.Data.CheckIn,
            CheckOut = response.Data.CheckOut
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(Guid id, UpdateReservationRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _reservationService.UpdateAsync(id, userId, request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View("Edit", request);
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Destroy(Guid id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _reservationService.DeleteAsync(id, userId);
        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }

}
