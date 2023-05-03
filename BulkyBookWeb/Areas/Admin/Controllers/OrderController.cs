using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize]
public class OrderController :Controller
{
    private readonly IUnitOfWork _unit;
    [BindProperty]
    public OrderVM OrderVm { get; set; }

    public OrderController(IUnitOfWork unit)
    {
        _unit = unit;
    }
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Details(int orderId)
    {
         OrderVm = new()
        {
            OrderHeader = _unit.OrderHeader.Get(u=>u.Id==orderId , includeProperties: "ApplicationUser"),
            OrderDetail = _unit.OrderDetail.GetAll(u=>u.OrderHeaderId==orderId,includeProperties:"Product")
        };
        return View(OrderVm);
    }
    [HttpPost]
    [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
    
    public IActionResult UpdateOrderDetail(int orderId)
    {
        var odFromDb = _unit.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
        odFromDb.Name = OrderVm.OrderHeader.Name;
        odFromDb.SurName = OrderVm.OrderHeader.SurName;
        odFromDb.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
        odFromDb.StreetAddress = OrderVm.OrderHeader.StreetAddress;
        odFromDb.City = OrderVm.OrderHeader.City;
        odFromDb.State = OrderVm.OrderHeader.State;
        odFromDb.PostalCode = OrderVm.OrderHeader.PostalCode;

        if (!string.IsNullOrEmpty(OrderVm.OrderHeader.Carrier))
        {
            odFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        }
        if (!string.IsNullOrEmpty(OrderVm.OrderHeader.TrackingNumber))
        {
            odFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        }
        _unit.OrderHeader.Update(odFromDb);
        _unit.Save();

        TempData["Success"] = "Order Details Updated Successfully.";
        
        return RedirectToAction(nameof(Details),new {orderId= odFromDb.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
    public IActionResult StartProcessing()
    {
        _unit.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id , SD.StatusInProcess);
        _unit.Save();
        TempData["Success"] = "Order Details Updated Successfully.";
        return RedirectToAction(nameof(Details),new {orderId= OrderVm.OrderHeader.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
    public IActionResult ShipOrder()
    {
        var orderHeader = _unit.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
        orderHeader.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderVm.OrderHeader.Carrier;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
        }
        _unit.OrderHeader.Update(orderHeader);
        _unit.Save();

        TempData["Success"] = "Order Shipped Successfully.";
        return RedirectToAction(nameof(Details),new {orderId= OrderVm.OrderHeader.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult CancelOrder()
    {
        var odH = _unit.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);

        if (odH.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions()
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = odH.PaymentIntentId
            };
            var service = new RefundService();
            Refund refund = service.Create(options);
            _unit.OrderHeader.UpdateStatus(odH.Id,SD.StatusCancelled,SD.StatusRefunded);
        }
        else
        {
            _unit.OrderHeader.UpdateStatus(odH.Id,SD.StatusCancelled,SD.StatusCancelled);
            
        }
        _unit.Save();
        TempData["Success"] = "Order Cancelled Successfully.";
        return RedirectToAction(nameof(Details),new {orderId= OrderVm.OrderHeader.Id});
    }


    [ActionName("Details")]
    [HttpPost]
    public IActionResult Details_PAY_NOW()
    {
        OrderVm.OrderHeader =
            _unit.OrderHeader.Get(o => o.Id == OrderVm.OrderHeader.Id, includeProperties: "ApplicationUser");
        OrderVm.OrderDetail =
            _unit.OrderDetail.GetAll(o => o.OrderHeaderId == OrderVm.OrderHeader.Id, includeProperties: "Product"); 
        
        
    
       // stripe logic
         var domain = "https://localhost:7169/";
         var options = new SessionCreateOptions()
         {
             SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVm.OrderHeader.Id}",
             CancelUrl = domain + $"admin/order/details?orderId={OrderVm.OrderHeader.Id}",
             LineItems = new List<SessionLineItemOptions>(),
             Mode = "payment",
         };
        
         foreach (var item in  OrderVm.OrderDetail)
         {
             var sessionLineItem = new SessionLineItemOptions
             {
                 PriceData = new SessionLineItemPriceDataOptions
                 {
                     UnitAmount = (long)(item.Price * 100), // 20.50 --> 2050
                     Currency = "usd",
                     ProductData = new SessionLineItemPriceDataProductDataOptions
                     {
                         Name = item.Product.Title
                     }
                 },
                 Quantity = item.Count
             };
             options.LineItems.Add(sessionLineItem);
         }
        
        
         var service = new SessionService();
         Session session =  service.Create(options);
        
         _unit.OrderHeader.UpdateStripePaymentId(OrderVm.OrderHeader.Id,session.Id, session.PaymentIntentId);
         _unit.Save();
         Response.Headers.Add("Location",session.Url);
         return new StatusCodeResult(303);
    }
    
    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = _unit.OrderHeader.Get(o => o.Id == orderHeaderId);
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            //this order by company
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToString() == "paid")
            {
                _unit.OrderHeader.UpdateStripePaymentId(orderHeaderId,session.Id, session.PaymentIntentId);
                _unit.OrderHeader.UpdateStatus(orderHeaderId,orderHeader.OrderStatus,SD.PaymentStatusApproved);
                _unit.Save();
            }
        }

        return View(orderHeaderId);
    }
    
    #region API CALLS

    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> objOrderHeaders ;

        if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
        {
            objOrderHeaders = _unit.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            objOrderHeaders = _unit.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties:"ApplicationUser");
        }
        switch (status)
        {
            
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusPending);
                    break;
                case "delayed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                       
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusInProcess);
                break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusShipped);
                break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusApproved);
                break;
                default:

                    break;
            

        }
        
        return Json(new { data = objOrderHeaders });
    }


    
    #endregion
}