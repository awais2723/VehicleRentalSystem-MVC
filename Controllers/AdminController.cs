using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VehicleRentalSystem.Models;

public class AdminController : Controller
{
    VehicleRentalDbEntities db = new VehicleRentalDbEntities();

    public ActionResult Dashboard()
    {
        var totalVehicles = db.Vehicles.Count();
       
        var totalBookings = db.Vehicles.Count(v => v.IsAvailable == false);

        var totalAvailableVehicles = db.Vehicles.Count(v => v.IsAvailable == true);

        ViewBag.TotalVehicles = totalVehicles;
        ViewBag.TotalBookings = totalBookings;
        ViewBag.TotalAvailableVehicles = totalAvailableVehicles;

        return View();
    }

    // Vehicle Management
    public ActionResult Vehicles()
    {
      
        
        
        return View(db.Vehicles.ToList());
    }
    // GET: Create Vehicle
    public ActionResult VehicleForm()
    {
        return View(new Vehicle());
    }

    // POST: Create Vehicle
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult CreateVehicle(Vehicle vehicle, HttpPostedFileBase imageFile)
    {
        if (ModelState.IsValid)
        {
            // Handle image upload
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                string fileName = System.IO.Path.GetFileName(imageFile.FileName);
                string imagePath = "~/Content/Images/" + fileName;
                string serverPath = Server.MapPath(imagePath);
                imageFile.SaveAs(serverPath);

                vehicle.ImagePath = imagePath;
            }

            
            vehicle.IsAvailable = true;

            db.Vehicles.Add(vehicle);
            db.SaveChanges();
            return RedirectToAction("Vehicles");
        }

        return View(vehicle);
    }

    // List of Available Vehicles
    public ActionResult AvailableVehicles()
    {
        var availableVehicles = db.Vehicles
            .Where(v => v.IsAvailable == true)
            .ToList();

        return View(availableVehicles);
    }


    // List of Booked Vehicles
    public ActionResult BookedVehicles()
    {
        var bookedVehicles = db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.User)
            .Where(b => b.Vehicle.IsAvailable == false && (b.IsReturned == false || b.IsReturned == null))
            .ToList();

        return View(bookedVehicles); // @model IEnumerable<Booking>
    }



    public ActionResult EditVehicle(int id)
    {
        var vehicle = db.Vehicles.Find(id);
        if (vehicle == null) return HttpNotFound();
        return View("VehicleForm", vehicle); // reuse the same view
    }

    [HttpPost]
    public ActionResult EditVehicle(Vehicle vehicle, HttpPostedFileBase vehicleImage)
    {
        var existing = db.Vehicles.Find(vehicle.Id);
        if (existing == null) return HttpNotFound();

        existing.Name = vehicle.Name;
        existing.Type = vehicle.Type;
        existing.Model = vehicle.Model;
        existing.EngineCapacity = vehicle.EngineCapacity;
        existing.RatePerDay = vehicle.RatePerDay;

        if (vehicleImage != null && vehicleImage.ContentLength > 0)
        {
            string fileName = Path.GetFileName(vehicleImage.FileName);
            string path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
            vehicleImage.SaveAs(path);
            existing.ImagePath = "~/Content/Images/" + fileName;
        }

        db.SaveChanges();
        return RedirectToAction("Vehicles");
    }
    public ActionResult DeleteVehicle(int id)
    {
        var vehicle = db.Vehicles.Find(id);
        if (vehicle == null)
            return HttpNotFound();

        TempData["DeleteConfirm"] = $"Are you sure you want to delete vehicle '{vehicle.Name}' ({vehicle.Model})?";
        TempData["DeleteId"] = vehicle.Id;

        return RedirectToAction("Vehicles");
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult DeleteConfirmed(int id)
    {
        var vehicle = db.Vehicles.Include(v => v.Bookings).FirstOrDefault(v => v.Id == id);
        if (vehicle == null)
            return HttpNotFound();

        if (vehicle.Bookings.Any())
        {
            TempData["DeleteError"] = "Cannot delete this vehicle because it has existing bookings.";
            return RedirectToAction("Vehicles");
        }

        // Optionally delete the image
        if (!string.IsNullOrEmpty(vehicle.ImagePath))
        {
            var fullPath = Server.MapPath(vehicle.ImagePath);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        db.Vehicles.Remove(vehicle);
        db.SaveChanges();

        return RedirectToAction("Vehicles");
    }






    // View pending return requests
    public ActionResult PendingReturns()
    {
        var pendingReturns = db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.User)
            .Where(b => (b.ReturnPending ?? false) && !(b.IsReturned ?? false))
            .ToList();

        // Calculate TotalAmount if needed
        foreach (var booking in pendingReturns)
        {
            if (!booking.TotalAmount.HasValue && booking.StartDate.HasValue && booking.EndDate.HasValue)
            {
                var days = (booking.EndDate.Value - booking.StartDate.Value).Days + 1;
                days = Math.Max(days, 1);
                booking.TotalAmount = days * booking.Vehicle.RatePerDay;
            }
        }

        return View(pendingReturns);
    }










    // Approve return request
    public ActionResult ApproveReturn(int id)
    {
        var booking = db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.User)
            .FirstOrDefault(b => b.Id == id);

        if (booking == null) return HttpNotFound();

        // Make sure required fields exist
        if (!booking.StartDate.HasValue || !booking.EndDate.HasValue)
        {
            TempData["Error"] = "Booking dates are incomplete.";
            return RedirectToAction("PendingReturns");
        }

        // Set vehicle as available again
        booking.Vehicle.IsAvailable = true;
       

        // Save booking to history
        var history = new BookingHistory
        {
            VehicleId = booking.VehicleId,
            UserId = booking.UserId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalAmount = booking.TotalAmount ?? 0,
            VehicleName = booking.Vehicle.Name,
            VehicleModel = booking.Vehicle.Model
        };

        db.BookingHistories.Add(history);

        // Remove from active bookings table
        db.Bookings.Remove(booking);

        db.SaveChanges();

        return RedirectToAction("PendingReturns");
    }


    public ActionResult ReviewReturn(int? id)
    {
        var booking = db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.User)
            .FirstOrDefault(b => b.Id == id);

        if (booking == null) return HttpNotFound();

        if (booking.StartDate.HasValue && booking.EndDate.HasValue)
        {
            var totalDays = (booking.EndDate.Value - booking.StartDate.Value).Days + 1;
            booking.TotalAmount = Math.Max(totalDays, 1) * booking.Vehicle.RatePerDay;
        }

        return View(booking);
    }


    [HttpPost]
    public ActionResult ReviewReturn(int? id, DateTime? newEndDate)
    {
        if (id == null) return RedirectToAction("PendingReturns");

        var booking = db.Bookings.Include(b => b.Vehicle).FirstOrDefault(b => b.Id == id.Value);
        if (booking == null) return HttpNotFound();

        // Use new date if provided, otherwise keep the original end date
        var finalEndDate = newEndDate ?? booking.EndDate;

        if (!booking.StartDate.HasValue || !finalEndDate.HasValue)
        {
            TempData["Error"] = "Booking dates are invalid.";
            return RedirectToAction("PendingReturns");
        }

        booking.EndDate = finalEndDate.Value;

        // Recalculate days and total
        var totalDays = (finalEndDate.Value - booking.StartDate.Value).Days + 1;
        totalDays = Math.Max(totalDays, 1);
        booking.TotalAmount = totalDays * booking.Vehicle.RatePerDay;

        booking.IsReturned = true;
        booking.ReturnPending = false;
        booking.Vehicle.IsAvailable = true;

        var history = new BookingHistory
        {
            VehicleId = booking.VehicleId,
            UserId = booking.UserId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalAmount = booking.TotalAmount ?? 0,
            VehicleName = booking.Vehicle.Name,
            VehicleModel = booking.Vehicle.Model
        };

        db.BookingHistories.Add(history);

        // Remove from active bookings table
        db.Bookings.Remove(booking);

        db.SaveChanges();

        return RedirectToAction("PendingReturns");
    }


}
