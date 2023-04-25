using Bulky.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;
[Area("Admin")]
public class CategoryController: Controller
{
    private readonly IUnitOfWork _unit;

    public CategoryController(IUnitOfWork unit)
    {
        _unit = unit;
    }
    
    //Get

    public IActionResult Index()
    {
        List<Category> categories = _unit.Category.GetAll().ToList();
        return View(categories);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category c)
    {
        // if (c.Name = c.DisplayOrder.ToString())
        // {
        //     ModelState.AddModelError("name" ,"The Display Order can not exactly match the Name");
        // }
        if (c.Name !=null && c.Name.ToLower() == "test")
        {
            ModelState.AddModelError("", "Test name is invalid value");
        }

        if (ModelState.IsValid)
        {
            _unit.Category.Add(c);
            _unit.Save();
            TempData["success"] = "Category created successfully!";
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
        Category? categoryFromDb=  _unit.Category.Get(c=>c.Id==id);
       // Category categoryFromDb1 = _db.Categories.FirstOrDefault(c=>c.Id==id);
       // Category categoryFromDb2 = _db.Categories.Where(u => u.Id==id).FirstOrDefault();
        
        if (categoryFromDb == null)
        {
            return NotFound();
        }

        return View(categoryFromDb);
    }

    [HttpPost]
    public IActionResult Edit(Category c)
    {
        if (ModelState.IsValid)
        {
            _unit.Category.Update(c);
            _unit.Save();
            TempData["success"] = "Category edited successfully!";
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
        Category? categoryFromDb=  _unit.Category.Get(c=>c.Id==id);
        if (categoryFromDb == null)
        {
            return NotFound();
        }

        return View(categoryFromDb);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePOST(int? id)
    {
        Category? c = _unit.Category.Get(c=>c.Id==id);
        if (c == null)
        {
            return NotFound();
        }
        _unit.Category.Remove(c);
        _unit.Save();
        TempData["success"] = "Category deleted successfully!";
        return RedirectToAction("Index");
  
    }
}