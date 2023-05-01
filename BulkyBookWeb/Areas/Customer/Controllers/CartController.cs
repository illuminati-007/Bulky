using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Customer.Controllers;
[Area("Customer")]
[Authorize]
public class CartController :Controller
{
    public readonly IUnitOfWork _unit;
    [BindProperty]
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
                u.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new()
        };
        foreach (var cart in ShoppingCartVm.ShopListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVm);
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        ShoppingCartVm = new()
        {
            ShopListCart = _unit.ShoppingCart.GetAll(u =>
                u.ApplicationUserId == userId, includeProperties: "Product"),
            OrderHeader = new()
        };
        ShoppingCartVm.OrderHeader.ApplicationUser = _unit.ApplicationUser.Get(u => u.Id == userId);
        ShoppingCartVm.OrderHeader.Name = ShoppingCartVm.OrderHeader.ApplicationUser.Name;
        ShoppingCartVm.OrderHeader.SurName = ShoppingCartVm.OrderHeader.ApplicationUser.SurName;
        ShoppingCartVm.OrderHeader.PhoneNumber = ShoppingCartVm.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartVm.OrderHeader.StreetAddress = ShoppingCartVm.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVm.OrderHeader.City = ShoppingCartVm.OrderHeader.ApplicationUser.City;
        ShoppingCartVm.OrderHeader.State = ShoppingCartVm.OrderHeader.ApplicationUser.State;
        ShoppingCartVm.OrderHeader.PostalCode = ShoppingCartVm.OrderHeader.ApplicationUser.PostalCode;

      
        foreach (var cart in ShoppingCartVm.ShopListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        return View(ShoppingCartVm);
    }

    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPOST()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        ShoppingCartVm.ShopListCart = _unit.ShoppingCart.GetAll(u =>
            u.ApplicationUserId == userId, includeProperties: "Product");

        ShoppingCartVm.OrderHeader.OrderDate = System.DateTime.Now;
        ShoppingCartVm.OrderHeader.ApplicationUserId = userId;

        ApplicationUser applicationUser = _unit.ApplicationUser.Get(u => u.Id == userId);
        foreach (var cart in ShoppingCartVm.ShopListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            //regular cust acc
            ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusPending;
        }
        else
        {
            //comp user
            ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusApproved;
        }
        _unit.OrderHeader.Add(ShoppingCartVm.OrderHeader);
        _unit.Save();

        foreach (var cart in ShoppingCartVm.ShopListCart)
        {
            OrderDetail od = new()
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartVm.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unit.OrderDetail.Add(od);
            _unit.Save();
            
        }
        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            //regular cust acc we need to capture payment
        //stripe logic
        var domain = "https://localhost:7169/";
        var options = new SessionCreateOptions
        {
            
            SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVm.OrderHeader.Id}",
            CancelUrl = domain + "customer/cart/index",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var item in ShoppingCartVm.ShopListCart)
        {
            var sessionLineItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100), // 20.50 --> 2050
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Title
                    }
                },
                Quantity = item.Count
            };
            options.LineItems.Add(sessionLineItem);
        }
        
        
        var service = new SessionService();
        Session session =  service.Create(options);
        
        _unit.OrderHeader.UpdateStripePaymentId(ShoppingCartVm.OrderHeader.Id,session.Id, session.PaymentIntentId);
        _unit.Save();
        Response.Headers.Add("Location",session.Url);
        return new StatusCodeResult(303);
        }
        return RedirectToAction(nameof(OrderConfirmation), new {id =ShoppingCartVm.OrderHeader.Id});
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = _unit.OrderHeader.Get(o => o.Id == id, includeProperties: "ApplicationUser");
        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
        {
            //this order by cust
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToString() == "paid")
            {
                _unit.OrderHeader.UpdateStripePaymentId(id,session.Id, session.PaymentIntentId);
                _unit.OrderHeader.UpdateStatus(id,SD.StatusApproved,SD.PaymentStatusApproved);
                _unit.Save();
            }
        }

        List<ShoppingCart> shoppingCarts =
            _unit.ShoppingCart.GetAll(u =>
                u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
        _unit.ShoppingCart.RemoveRange(shoppingCarts);
        _unit.Save();
        return View(id);
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