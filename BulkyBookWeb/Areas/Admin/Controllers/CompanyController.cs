using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers;

[Area("Admin")]
 [Authorize(Roles = SD.Role_Admin)]
public class CompanyController:Controller
{
    private readonly IUnitOfWork _unit;

    public CompanyController(IUnitOfWork unit)
    {
        _unit = unit;
    }

    public IActionResult Index()
    {
        List<Company> compList = _unit.Company.GetAll().ToList();
        return View(compList);
    }


    public IActionResult Upsert(int? id)
    {
        if (id == null || id == 0)
        {
            return View(new Company());
        }
        else
        {
            Company copm = _unit.Company.Get(c => c.Id == id);
            return View(copm);
        }
    }

    [HttpPost]
    public IActionResult Upsert(Company comp)
    {
        if (ModelState.IsValid)
        {
            if (comp.Id == 0)
            {
                _unit.Company.Add(comp);
            }
            else
            {
                _unit.Company.Update(comp);
            }
            _unit.Save();
            TempData["success"] = "Сompany created successfully";
            return RedirectToAction("Index");
        }
        else
        {
            return View(comp);
        }
    }

    #region  API CALLS

    [HttpGet] 
    public IActionResult GetAll()
    {
        List<Company> complist = _unit.Company.GetAll().ToList();
        return Json(new {data = complist});
    }

    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var companyToDelete = _unit.Company.Get(c => c.Id == id);
        if (companyToDelete == null)
        {
            return Json(new { success = false, message = "Error while deleting!" });
            
        }
        _unit.Company.Remove(companyToDelete);
        _unit.Save();

        return Json(new { success = true, message = "Delete Successful" });
    } 
    
    #endregion
}