using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers;

public class CategoryController: Controller
{
    private readonly ICategoryRepository catRepo;

    public CategoryController(ICategoryRepository db)
    {
        catRepo = db;
    }
    
    //Get

    public IActionResult Index()
    {
        List<Category> categories = catRepo.GetAll().ToList();
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
            catRepo.Add(c);
            catRepo.Save();
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
        Category? categoryFromDb=  catRepo.Get(c=>c.Id==id);
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
            catRepo.Update(c);
            catRepo.Save();
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
        Category? categoryFromDb=  catRepo.Get(c=>c.Id==id);
        if (categoryFromDb == null)
        {
            return NotFound();
        }

        return View(categoryFromDb);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePOST(int? id)
    {
        Category? c = catRepo.Get(c=>c.Id==id);
        if (c == null)
        {
            return NotFound();
        }
        catRepo.Remove(c);
        catRepo.Save();
        TempData["success"] = "Category deleted successfully!";
        return RedirectToAction("Index");
  
    }
}