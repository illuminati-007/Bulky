using System.Linq.Expressions;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using BulkyWeb.Models;

namespace Bulky.DataAccess.Repository;

public class CategoryRepository: Repository<Category>, ICategoryRepository
{
    private ApplicationDbContext _db;

    public CategoryRepository(ApplicationDbContext db):base (db)
    {
        _db = db;
    }
    public void Update(Category c)
    {
        _db.Categories.Update(c);
    }

  
}