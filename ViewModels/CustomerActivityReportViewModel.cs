using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VehicleRentalSystem.ViewModels
{


	public class CustomerActivityReportViewModel
	{
		// Property for the chart data
		public List<string> CustomerLabels { get; set; }
		public List<int> CustomerBookingCounts { get; set; }

		// Property for the table data (using a specific class is even better)
		public List<CustomerBookingSummary> AllCustomersTabularData { get; set; }

		public CustomerActivityReportViewModel()
		{
			// Initialize lists to avoid null reference errors in the view
			CustomerLabels = new List<string>();
			CustomerBookingCounts = new List<int>();
			AllCustomersTabularData = new List<CustomerBookingSummary>();
		}
	}

	// A helper class to hold the data for each row in your table
	public class CustomerBookingSummary
	{
		public string Username { get; set; }
		public string Phone { get; set; }
		public int TotalBookings { get; set; }
		public int TotalHistoricalBookings { get; set; }
		public decimal TotalSpent { get; set; }
	}
}