using BulkyBook.DataAcess.Data;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    private readonly ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db):base(db)
    {
        _db = db;
    }

    public void Update(Company comp)
    {
        _db.Companies.Update(comp);
    }
}