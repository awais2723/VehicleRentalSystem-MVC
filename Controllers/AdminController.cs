using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VehicleRentalSystem.Models;
using VehicleRentalSystem.ViewModels;


public class AdminController : Controller
{
    VehicleRentalDbEntities db = new VehicleRentalDbEntities();

    public ActionResult Dashboard()
    {
        var totalVehicles = db.Vehicles.Count();
        var totalBookings = db.Bookings.Count(b => !(b.IsReturned ?? false)); // Count active (not yet returned) bookings
        var totalAvailableVehicles = db.Vehicles.Count(v => v.IsAvailable == true);
        var pendingReturnRequests = db.Bookings.Count(b => (b.ReturnPending ?? false) && !(b.IsReturned ?? false));

        ViewBag.TotalVehicles = totalVehicles;
        ViewBag.TotalBookings = totalBookings;
        ViewBag.TotalAvailableVehicles = totalAvailableVehicles;
        ViewBag.PendingReturnRequests = pendingReturnRequests; // Pass this count to the view

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
    //public ActionResult ApproveReturn(int id)
    //{
    //    var booking = db.Bookings
    //        .Include(b => b.Vehicle)
    //        .Include(b => b.User)
    //        .FirstOrDefault(b => b.Id == id);

    //    if (booking == null) return HttpNotFound();

    //    // Make sure required fields exist
    //    if (!booking.StartDate.HasValue || !booking.EndDate.HasValue)
    //    {
    //        TempData["Error"] = "Booking dates are incomplete.";
    //        return RedirectToAction("PendingReturns");
    //    }

    //    // Set vehicle as available again
    //    booking.Vehicle.IsAvailable = true;


    //    // Save booking to history
    //    var history = new BookingHistory
    //    {
    //        VehicleId = booking.VehicleId,
    //        UserId = booking.UserId,
    //        StartDate = booking.StartDate,
    //        EndDate = booking.EndDate,
    //        TotalAmount = booking.TotalAmount ?? 0,
    //        VehicleName = booking.Vehicle.Name,
    //        VehicleModel = booking.Vehicle.Model
    //    };

    //    db.BookingHistories.Add(history);

    //    // Remove from active bookings table
    //    db.Bookings.Remove(booking);

    //    db.SaveChanges();

    //    return RedirectToAction("PendingReturns");
    //}


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


    // ... (existing AdminController code) ...

    // View Customers
    public ActionResult ViewCustomers()
    {
        var customers = db.Users.Where(u => u.Role == "Customer").ToList();
        return View(customers);
    }

    // GET: Edit Customer
    public ActionResult EditCustomer(int? id)
    {
        if (id == null)
        {
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
        }
        var user = db.Users.Find(id);
        if (user == null)
        {
            return HttpNotFound();
        }
        return View(user); // Reuse a generic UserForm or create specific EditCustomer view
    }






    // POST: Edit Customer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult EditCustomer([Bind(Include = "Id,Username,Phone")] User user) // Only allow binding for editable fields
    {
        if (ModelState.IsValid)
        {
            var existingUser = db.Users.Find(user.Id);
            if (existingUser == null)
            {
                return HttpNotFound();
            }

            // Update only allowed fields
            existingUser.Username = user.Username;
            existingUser.Phone = user.Phone;
            // Password and Role are typically not edited here for security
            // if (existingUser.Password != user.Password) { // Handle password change only if necessary and securely }
            // existingUser.Role = user.Role; // Role change usually has its own dedicated logic/permissions

            db.Entry(existingUser).State = EntityState.Modified;
            db.SaveChanges();
            TempData["SuccessMessage"] = "Customer details updated successfully!";
            return RedirectToAction("ViewCustomers");
        }
        return View(user);
    }

    // GET: Delete Customer (Confirmation)
    public ActionResult DeleteCustomer(int? id)
    {
        if (id == null)
        {
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
        }
        var user = db.Users.Find(id);
        if (user == null)
        {
            return HttpNotFound();
        }

        // Check for associated bookings before confirming deletion
        var hasBookings = db.Bookings.Any(b => b.UserId == id);
        if (hasBookings)
        {
            TempData["DeleteError"] = $"Cannot delete customer '{user.Username}' because they have existing bookings.";
            return RedirectToAction("ViewCustomers");
        }

        TempData["DeleteConfirm"] = $"Are you sure you want to delete customer '{user.Username}'?";
        TempData["DeleteId"] = user.Id;
        return RedirectToAction("ViewCustomers"); // Redirect back to list to show confirmation message
    }

    // POST: Delete Customer Confirmed
    [HttpPost, ActionName("DeleteCustomer")] // Use ActionName to map to the DeleteCustomer URL for POST
    [ValidateAntiForgeryToken]
    public ActionResult DeleteCustomerConfirmed(int id)
    {
        var user = db.Users.Find(id);
        if (user == null)
        {
            return HttpNotFound();
        }

        // Double check for bookings to prevent accidental deletion if user navigates directly
        var hasBookings = db.Bookings.Any(b => b.UserId == id);
        if (hasBookings)
        {
            TempData["DeleteError"] = $"Cannot delete customer '{user.Username}' because they have existing bookings.";
            return RedirectToAction("ViewCustomers");
        }

        db.Users.Remove(user);
        db.SaveChanges();
        TempData["SuccessMessage"] = "Customer deleted successfully!";
        return RedirectToAction("ViewCustomers");
    }






    public ActionResult VehiclePerformanceReport()
    {
        // --- Data for Top 10 Most Booked Vehicles (Chart: Bar Chart) ---
        // Sourcing from BookingHistories for completed bookings
        var top10VehiclesForChart = db.BookingHistories
            .Include(b => b.VehicleId) // Ensure Vehicle is included to access Name and Model
            .GroupBy(b => new { b.VehicleId, b.VehicleName, b.VehicleModel }) // Group by the stored Name and Model in history
            .Select(g => new MostBookedVehicleViewModel
            {
                VehicleId = g.Key.VehicleId ?? 0, // Handle nullable VehicleId if it's int?
                VehicleName = g.Key.VehicleName + " (" + g.Key.VehicleModel + ")",
                VehicleModel = g.Key.VehicleModel,
                BookingCount = g.Count(),
                // Use ?? 0M directly for the Sum result
                TotalRevenue = g.Sum(b => b.TotalAmount) ?? 0M
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(10)
            .ToList();

        ViewBag.ChartLabels = top10VehiclesForChart.Select(x => x.VehicleName).ToList();
        ViewBag.ChartData = top10VehiclesForChart.Select(x => x.BookingCount).ToList();


        // --- Data for All Vehicles Summary (Tabular) ---
        // Aggregate data for ALL vehicles based on their historical performance.
        var allVehiclesSummary = db.Vehicles
            .Select(v => new AllVehicleSummaryViewModel
            {
                VehicleName = v.Name,
                VehicleModel = v.Model,
                VehicleType = v.Type,
                // Calculate TotalBookings from BookingHistories for this vehicle
                TotalBookings = db.BookingHistories.Count(bh => bh.VehicleId == v.Id),
                // Calculate TotalRevenue from BookingHistories for this vehicle
                // Use ?? 0M directly instead of GetValueOrDefault()
                TotalRevenue = db.BookingHistories
                                .Where(bh => bh.VehicleId == v.Id)
                                .Sum(bh => bh.TotalAmount) ?? 0M
            })
            .OrderBy(x => x.VehicleName)
            .ToList();

        // Pass the comprehensive tabular data as the model
        return View(allVehiclesSummary);
    }

    public ActionResult FinancialOverviewReport()
    {
        // Data for Monthly Revenue (Chart: Line Chart) - Keep as is
        var monthlyRevenue = db.BookingHistories // Use BookingHistory for completed transactions
            .GroupBy(b => new { Year = b.StartDate.Value.Year, Month = b.StartDate.Value.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(b => b.TotalAmount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList()
            .Select(x => new
            {
                Label = new DateTime(x.Year, x.Month, 1).ToString("MMM"), // Format for chart label
                Amount = x.TotalAmount
            });

        ViewBag.MonthlyRevenueLabels = monthlyRevenue.Select(x => x.Label).ToList();
        ViewBag.MonthlyRevenueData = monthlyRevenue.Select(x => x.Amount).ToList();

        // Data for Overall Financial Summary (Header Cards) - Keep as is
        var totalRevenue = db.BookingHistories.Sum(b => b.TotalAmount) ?? 0;
        var averageBookingAmount = db.BookingHistories.Any() ? db.BookingHistories.Average(b => b.TotalAmount) ?? 0 : 0;
        var totalActiveBookingsValue = db.Bookings.Where(b => !(b.IsReturned ?? false)).Sum(b => b.TotalAmount) ?? 0; // Value of ongoing bookings

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.AverageBookingAmount = averageBookingAmount;
        ViewBag.TotalActiveBookingsValue = totalActiveBookingsValue;

        // Data for Total Revenue Per Car (Tabular) - MODIFIED LOGIC TO FIX NotsupportedException
        var totalRevenuePerCar = db.BookingHistories
            .Join(db.Vehicles, // Assuming 'db.Vehicles' is your DbSet for vehicles
                  bh => bh.VehicleId, // Foreign key in BookingHistory
                  v => v.Id,   // Primary key in Vehicle
                  (bh, v) => new { CarName = v.Name, CarId = v.Id, bh.TotalAmount }) // Changed v.Make to v.Name for CarName
            .GroupBy(x => new { x.CarName, x.CarId }) // Group by car name and ID
            .Select(g => new
            {
                CarName = g.Key.CarName,
                CarId = g.Key.CarId,
                TotalAmount = g.Sum(x => x.TotalAmount) ?? 0
            }) // Project into another anonymous type
            .ToList() // Execute the query and bring data into memory
            .Select(x => new Tuple<string, string, decimal>(
                                x.CarName,
                                x.CarId.ToString(), // Now ToString() is called in memory
                                x.TotalAmount)) // Select car name, ID, and sum of total amounts
            .OrderByDescending(t => t.Item3) // Order by revenue (highest first)
            .ToList();

        // Pass this new list as the model to the view
        return View(totalRevenuePerCar);
    }





    // GET: Customer Activity Report

    public ActionResult CustomerActivityReport()
    {
        // --- Chart Data ---
        var topCustomers = db.BookingHistories
            .Include(b => b.User)
            .GroupBy(b => new { b.UserId, b.User.Username })
            .Select(g => new
            {
                Username = g.Key.Username,
                BookingCount = g.Count()
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(10)
            .ToList();

        // --- Tabular Data ---
        var allCustomersWithBookings = db.Users
            .Where(u => u.Role == "Customer")
            .Select(u => new CustomerBookingSummary // Use the new helper class here
            {
                Username = u.Username,
                Phone = u.Phone,
                TotalBookings = db.Bookings.Count(b => b.UserId == u.Id && !(b.IsReturned ?? false)),
                TotalHistoricalBookings = db.BookingHistories.Count(bh => bh.UserId == u.Id),
                TotalSpent = db.BookingHistories.Where(bh => bh.UserId == u.Id).Sum(bh => (decimal?)bh.TotalAmount) ?? 0
            })
            .OrderByDescending(x => x.TotalHistoricalBookings)
            .ToList();

        // --- Create and Populate the ViewModel ---
        var viewModel = new CustomerActivityReportViewModel
        {
            CustomerLabels = topCustomers.Select(x => x.Username).ToList(),
            CustomerBookingCounts = topCustomers.Select(x => x.BookingCount).ToList(),
            AllCustomersTabularData = allCustomersWithBookings
        };

        // Pass the strongly-typed viewModel to the view
        return View(viewModel);
    }


}
