using AppaRently.App.DTOs.Favorites;
using AppaRently.App.Interfaces;
using AppaRently.Web.Client.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Client.Controllers;

public class FavoriteController : ClientControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoriteController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _favoriteService.GetAllAsync(new FavoriteSearchRequest
        {
            UserId = userId
        });

        var model = new FavoritePageViewModel
        {
            Favorites = response.Data?.ToList() ?? new List<FavoriteResponse>()
        };

        return View(model);
    }

    public async Task<IActionResult> Show(Guid id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _favoriteService.GetByIdAsync(id);
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

    public IActionResult Create(Guid apartmentId)
    {
        if (string.IsNullOrWhiteSpace(GetCurrentUserId()))
        {
            return RedirectToLogin();
        }

        return View(new CreateFavoriteRequest
        {
            ApartmentId = apartmentId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Storage(CreateFavoriteRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        var response = await _favoriteService.CreateAsync(userId, request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View("Create", request);
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction("Detail", "Apartment", new { id = request.ApartmentId });
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

        var response = await _favoriteService.DeleteAsync(id, userId);
        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }

}
