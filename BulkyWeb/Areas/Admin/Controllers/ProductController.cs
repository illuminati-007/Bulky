using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;
[Area("Admin")]
public class ProductController: Controller
{
    private readonly IUnitOfWork _unit;

    public ProductController(IUnitOfWork unit)
    {
        _unit = unit;
    }
    
    //Get

    public IActionResult Index()
    {
        List<Product> products = _unit.Product.GetAll().ToList();
      
        return View(products);
    }

    public IActionResult Create()
    {
        IEnumerable<SelectListItem> CategoryList = _unit.Category.GetAll().Select(c =>
            new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });
       // ViewBag.CategoryList = CategoryList;
       ViewData["CategoryList"] = CategoryList;
        return View();
    }

    [HttpPost]
    public IActionResult Create(Product p)
    {
        // if (c.Name = c.DisplayOrder.ToString())
        // {
        //     ModelState.AddModelError("name" ,"The Display Order can not exactly match the Name");
        // }
       

        if (ModelState.IsValid)
        {
            _unit.Product.Add(p);
            _unit.Save();
            TempData["success"] = "Product created successfully!";
            return RedirectToAction("Index");
        }
        

        return View();
    }

    public IActionResult Edit(int? id)
    {
        if (id == null && id == 0)
        {
            return NotFound();
        }
        Product? p=  _unit.Product.Get(c=>c.Id==id);
       // Category categoryFromDb1 = _db.Categories.FirstOrDefault(c=>c.Id==id);
       // Category categoryFromDb2 = _db.Categories.Where(u => u.Id==id).FirstOrDefault();
        
        if (p == null)
        {
            return NotFound();
        }

        return View(p);
    }

    [HttpPost]
    public IActionResult Edit(Product p)
    {
        if (ModelState.IsValid)
        {
            _unit.Product.Update(p);
            _unit.Save();
            TempData["success"] = "Product edited successfully!";
            return RedirectToAction("Index");
        }

        return View();
    }

    public IActionResult Delete(int? id)
    {
        if (id == null && id == 0)
        {
            return NotFound();
        }
        Product? p=  _unit.Product.Get(c=>c.Id==id);
        if (p == null)
        {
            return NotFound();
        }

        return View(p);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePOST(int? id)
    {
        Product? p = _unit.Product.Get(c=>c.Id==id);
        if (p == null)
        {
            return NotFound();
        }
        _unit.Product.Remove(p);
        _unit.Save();
        TempData["success"] = "Category deleted successfully!";
        return RedirectToAction("Index");
  
    }
}