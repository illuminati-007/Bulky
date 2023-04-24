using Bulky.DataAccess.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers;

public class CategoryController: Controller
{
    private readonly ApplicationDbContext _db;

    public CategoryController(ApplicationDbContext db)
    {
        _db = db;
    }
    
    //Get

    public IActionResult Index()
    {
        List<Category> categories = _db.Categories.ToList();
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
            _db.Categories.Add(c);
            _db.SaveChanges();
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
        Category? categoryFromDb=  _db.Categories.Find(id);
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
            _db.Categories.Update(c);
            _db.SaveChanges();
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
        Category? categoryFromDb=  _db.Categories.Find(id);
        if (categoryFromDb == null)
        {
            return NotFound();
        }

        return View(categoryFromDb);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePOST(int? id)
    {
        Category? c = _db.Categories.Find(id);
        if (c == null)
        {
            return NotFound();
        }
        _db.Categories.Remove(c);
        _db.SaveChanges();
        TempData["success"] = "Category deleted successfully!";
        return RedirectToAction("Index");
  
    }
}