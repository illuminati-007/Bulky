namespace BulkyBook.Models.ViewModels;

public class ShoppingCartVM
{
   public IEnumerable<ShoppingCart> ShopListCart { get; set; }
   public double OrderTotal { get; set; }
}