using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Customer.Controllers;
[Area("Customer")]
[Authorize]
public class CartController :Controller
{
    public readonly IUnitOfWork _unit;
    public ShoppingCartVM ShoppingCartVm { get; set; }

    public CartController(IUnitOfWork unit)
    {
        _unit = unit;
    }
        
    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        ShoppingCartVm = new()
        {
            ShopListCart = _unit.ShoppingCart.GetAll(u =>
                u.ApplicationUserId == userId, includeProperties: "Product")
        };
        foreach (var cart in ShoppingCartVm.ShopListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVm);
    }

    public IActionResult Summary()
    {
        return View();
    }

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unit.ShoppingCart.Get(c => c.Id == cartId);
        cartFromDb.Count += 1;
        _unit.ShoppingCart.Update(cartFromDb);
        _unit.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        
        var cartFromDb = _unit.ShoppingCart.Get(c => c.Id == cartId);
        if (cartFromDb.Count <= 1)
        {
            _unit.ShoppingCart.Remove(cartFromDb);
        }
        else
        {
            cartFromDb.Count -= 1;
            _unit.ShoppingCart.Update(cartFromDb);
        }

        _unit.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = _unit.ShoppingCart.Get(c => c.Id == cartId);
        _unit.ShoppingCart.Remove(cartFromDb);
        _unit.Save();
        return RedirectToAction(nameof(Index));
    }
    
    private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
    {
        if (shoppingCart.Count <= 50)
        {
            return shoppingCart.Product.Price;
        }
        else
        {
            if ( shoppingCart.Count < 100)
            {
                return shoppingCart.Product.Price50;
            }  else
            {
                return shoppingCart.Product.Price100;
            }
        }
    }
}