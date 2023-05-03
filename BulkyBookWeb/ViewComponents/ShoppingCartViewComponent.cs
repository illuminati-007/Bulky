using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.ViewComponents;

public class ShoppingCartViewComponent:ViewComponent
{
    private readonly IUnitOfWork _unit;

    public ShoppingCartViewComponent(IUnitOfWork unit)
    {
        _unit = unit;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        if (claim != null)
        {
            if (HttpContext.Session.GetInt32(SD.SessionCart) == null)
            {
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unit.ShoppingCart.GetAll(u=>u.ApplicationUserId == claim.Value).Count());
            }
            return View((int)HttpContext.Session.GetInt32(SD.SessionCart));
        }
        else
        {
            HttpContext.Session.Clear();
            return View(0);
        }
    }
    
}