// Models/MostBookedVehicleViewModel.cs (or ViewModels/MostBookedVehicleViewModel.cs)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// CHANGE THIS LINE:
namespace VehicleRentalSystem.ViewModels // Use your project's root namespace + .ViewModels
{
    public class MostBookedVehicleViewModel
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; }
        public string VehicleModel { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }


    public class AllVehicleSummaryViewModel
    {
        public string VehicleName { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleType { get; set; } // Added for comprehensive summary
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

