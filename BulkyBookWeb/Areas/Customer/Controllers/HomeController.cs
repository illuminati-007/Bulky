using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BulkyBook.DataAccess.Repository.IRepository;

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
            Product p = _unit.Product.Get(u=>u.Id==id ,includeProperties:"Category");
            return View(p);
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