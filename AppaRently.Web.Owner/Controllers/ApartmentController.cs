using System.Security.Claims;
using AppaRently.App.DTOs.Apartments;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.Owner.Models;
using AppaRently.Web.Owner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Owner.Controllers;

[Authorize(Roles = AppaRentlyRoles.Owner)]
public class ApartmentController : Controller
{
    private readonly IApartmentService _apartmentService;
    private readonly IOwnerPortalService _ownerPortalService;

    public ApartmentController(
        IApartmentService apartmentService,
        IOwnerPortalService ownerPortalService)
    {
        _apartmentService = apartmentService;
        _ownerPortalService = ownerPortalService;
    }

    public async Task<IActionResult> Index([FromQuery] OwnerDashboardQueryViewModel request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var model = await _ownerPortalService.BuildDashboardAsync(ownerId, request);
        return View(model);
    }

    public async Task<IActionResult> Deleted([FromQuery] OwnerDashboardQueryViewModel request)
    {
        request.DeletedOnly = true;
        return await Index(request);
    }

    public async Task<IActionResult> Show(Guid id, [FromQuery] OwnerReportPeriodViewModel request)
    {
        return await Detail(id, request);
    }

    public async Task<IActionResult> Detail(Guid id, [FromQuery] OwnerReportPeriodViewModel request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var model = await _ownerPortalService.BuildApartmentDetailAsync(ownerId, id, request);
        if (model is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View("Detail", model);
    }

    public IActionResult Create()
    {
        return View(new CreateApartmentRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Storage(CreateApartmentRequest request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var response = await _apartmentService.CreateAsync(ownerId, request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View("Create", request);
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var response = await _apartmentService.GetByIdByOwnerIdAsync(id, ownerId);
        if (!response.Success || response.Data is null)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["ApartmentId"] = id;
        return View(new UpdateApartmentRequest
        {
            Title = response.Data.Title,
            Description = response.Data.Description,
            ImageUrl = response.Data.ImageUrl,
            Price = response.Data.Price,
            Address = response.Data.Address,
            City = response.Data.City,
            Department = response.Data.Department
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(Guid id, UpdateApartmentRequest request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var response = await _apartmentService.UpdateAsync(id, ownerId, request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            ViewData["ApartmentId"] = id;
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
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var response = await _apartmentService.DeleteAsync(id, ownerId);
        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export([FromQuery] OwnerDashboardQueryViewModel request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var fileBytes = await _ownerPortalService.ExportDashboardAsync(ownerId, request);
        var fileName = $"owner-report-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Export(Guid id, [FromQuery] OwnerReportPeriodViewModel request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var fileBytes = await _ownerPortalService.ExportApartmentAsync(ownerId, id, request);
        var fileName = $"apartment-report-{id:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
