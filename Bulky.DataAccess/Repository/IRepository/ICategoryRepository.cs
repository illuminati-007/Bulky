using BulkyWeb.Models;

namespace Bulky.DataAccess.Repository.IRepository;

public interface ICategoryRepository: IRepository<Category>
{
    void Update(Category c);
    void Save();
}