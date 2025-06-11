using System;
using System.Linq;
using System.Web.Mvc;
using VehicleRentalSystem.Models;
using System.Data.Entity;

public class CustomerController : Controller
{
    VehicleRentalDbEntities db = new VehicleRentalDbEntities();

    private bool IsCustomer()
    {
        return Session["Role"]?.ToString() == "Customer" && Session["UserId"] != null;
    }

    public ActionResult Dashboard()
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account");

        var vehicles = db.Vehicles
            .Where(v => v.IsAvailable == true)
            .ToList();

        return View(vehicles);
    }

    // GET: Book vehicle
    public ActionResult Book(int id)
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account", new { role = "Customer" });

        var vehicle = db.Vehicles.Find(id);
        if (vehicle == null || vehicle.IsAvailable != true)
            return HttpNotFound("Vehicle not available.");

        ViewBag.VehicleName = vehicle.Name;
        ViewBag.VehicleType = vehicle.Type;
        ViewBag.RatePerDay = vehicle.RatePerDay;

        return View(new Booking { VehicleId = id });
    }

    // POST: Book vehicle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Book(Booking booking)
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account", new { role = "Customer" });

        if (ModelState.IsValid)
        {
            int userId = (int)Session["UserId"];

            booking.UserId = userId;
            booking.StartDate = DateTime.Now;
            booking.IsReturned = false;
            booking.ReturnPending = true;

            var vehicle = db.Vehicles.Find(booking.VehicleId);
            

            vehicle.IsAvailable = false;
            db.Bookings.Add(booking);
            db.Entry(vehicle).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = "Booking successful!";
            return RedirectToAction("MyBookings");
        }

        var v = db.Vehicles.Find(booking.VehicleId);
        ViewBag.VehicleName = v?.Name;
        ViewBag.VehicleType = v?.Type;
        ViewBag.RatePerDay = v?.RatePerDay;

        return View(booking);
    }

    // GET: View current user's bookings
    public ActionResult MyBookings()
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account");

        int userId = (int)Session["UserId"];

        var bookings = db.Bookings
            .Include(b => b.Vehicle)
            .Where(b => b.UserId == userId)  //&& (!b.IsReturned ?? true)
            .ToList();

        return View(bookings);
    }

    // GET: Past bookings for the current user
    public ActionResult BookingHistory()
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account");

        int userId = (int)Session["UserId"];

        var history = db.BookingHistories
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.EndDate)
            .ToList();

        return View(history);
    }

    // POST: Send return request
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult RequestReturn(int id, string confirmedEndDate)
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account");

        var booking = db.Bookings.Include(b => b.Vehicle).FirstOrDefault(b => b.Id == id);
        if (booking == null || booking.IsReturned == true)
            return HttpNotFound();

        int userId = (int)Session["UserId"];
        if (booking.UserId != userId)
        {
            return new HttpUnauthorizedResult("You are not allowed to return this booking.");
        }

        if (!DateTime.TryParse(confirmedEndDate, out DateTime parsedEndDate) || parsedEndDate < booking.StartDate)
        {
            TempData["Message"] = "Invalid end date submitted.";
            return RedirectToAction("MyBookings");
        }

        booking.EndDate = parsedEndDate;
        booking.ReturnPending = true;
        booking.IsReturned = false;

        db.Entry(booking).State = EntityState.Modified;
        db.SaveChanges();

        TempData["Message"] = "Return request sent to admin.";
        return RedirectToAction("MyBookings");
    }

    // GET: All available vehicles
    public ActionResult AvailableVehicles()
    {
        if (!IsCustomer())
            return RedirectToAction("Login", "Account");

        var availableVehicles = db.Vehicles
            .Where(v => v.IsAvailable == true)
            .ToList();

        return View(availableVehicles);
    }
}
