using AppaRently.App.DTOs.Apartments;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.SuperAdmin.Models;
using AppaRently.Web.SuperAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.SuperAdmin.Controllers;

[Authorize(Roles = AppaRentlyRoles.SuperAdmin)]
public class ApartmentController : Controller
{
    private readonly IApartmentService _apartmentService;
    private readonly ISuperAdminPortalService _portalService;

    public ApartmentController(
        IApartmentService apartmentService,
        ISuperAdminPortalService portalService)
    {
        _apartmentService = apartmentService;
        _portalService = portalService;
    }

    public async Task<IActionResult> Index([FromQuery] SuperAdminApartmentDashboardQueryViewModel request)
    {
        var model = await _portalService.BuildApartmentDashboardAsync(request);
        return View(model);
    }

    public async Task<IActionResult> Deleted([FromQuery] SuperAdminApartmentDashboardQueryViewModel request)
    {
        request.DeletedOnly = true;
        return await Index(request);
    }

    public async Task<IActionResult> Show(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        return await Detail(id, from, to);
    }

    public async Task<IActionResult> Detail(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var model = await _portalService.BuildApartmentDetailAsync(id, from, to);
        if (model is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View("Detail", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Destroy(Guid id)
    {
        var response = await _apartmentService.DeleteAsSuperAdminAsync(id);
        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }
}
