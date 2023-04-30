using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unit;
        public HomeController(ILogger<HomeController> logger,IUnitOfWork unit)
        {
            _unit = unit;
            _logger = logger;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> p = _unit.Product.GetAll(includeProperties:"Category");
            return View(p);
        }

        public IActionResult Details(int id)
        {
            ShoppingCart cart = new(){
            
            Product  = _unit.Product.Get(u=>u.Id==id ,includeProperties:"Category"),
            Count = 1,
            ProductId = id
            
            };
            
            return View(cart);
        }
        
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shopCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shopCart.ApplicationUserId = userId;
            shopCart.Id = 0;
            ShoppingCart cartFromDb = _unit.ShoppingCart.Get(s => s.ApplicationUserId == userId &&
                                                                  s.ProductId == shopCart.ProductId);
          
            if (cartFromDb != null)
            {
                cartFromDb.Count += shopCart.Count;
                _unit.ShoppingCart.Update(cartFromDb);
            }
            else
            {
                _unit.ShoppingCart.Add(shopCart);
            }

            TempData["success"] = "Cart updated successfully";
            
            _unit.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}