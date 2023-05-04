using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAcess.Data;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class UserController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public UserController(ApplicationDbContext db,UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }




    public IActionResult RoleManagment(string userId) {

        string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

        RoleManagmentVM RoleVM = new RoleManagmentVM() {
            ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
            RoleList = _db.Roles.Select(i => new SelectListItem {
                Text = i.Name,
                Value = i.Name
            }),
            CompanyList = _db.Companies.Select(i => new SelectListItem {
                Text = i.Name,
                Value = i.Id.ToString()
            }),
        };

        RoleVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;
        return View(RoleVM);
    }
    
    [HttpPost]
    public IActionResult RoleManagment(RoleManagmentVM roleManagmentVM) {

        string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == roleManagmentVM.ApplicationUser.Id).RoleId;
        string oldRole = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;

        if(!(roleManagmentVM.ApplicationUser.Role == oldRole)) {
            //a role was updated
            ApplicationUser applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagmentVM.ApplicationUser.Id);
            if (roleManagmentVM.ApplicationUser.Role == SD.Role_Company) {
                applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
            }
            if (oldRole == SD.Role_Company) {
                applicationUser.CompanyId = null;
            }
            _db.SaveChanges();

            _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(applicationUser, roleManagmentVM.ApplicationUser.Role).GetAwaiter().GetResult();

        }

        return RedirectToAction("Index");
    }


    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        List<ApplicationUser> userlist = _db.ApplicationUsers.Include(u=>u.Company).ToList();

        var userRoles = _db.UserRoles.ToList();
        var roles = _db.Roles.ToList();
        foreach (var user in userlist)
        {
            var roleId = _db.UserRoles.FirstOrDefault(r => r.UserId == user.Id).RoleId;
            user.Role = roles.FirstOrDefault(r => r.Id == roleId).Name;
            if (user.Company == null)
            {
                user.Company = new() { Name = "" }; 
            }
        }
        
        return Json(new { data = userlist });
    }
    
    
   


    #endregion
}