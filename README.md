
# Vehicle Rental System (.NET MVC)

A .NET MVC web application for managing a vehicle rental business.  
Customers can browse available cars, rent them for specific dates, and return them with automated billing.  
Admins can manage cars, approve returns, and view rental statistics.

---

## 📌 Features

### **Customer**
- **Browse Vehicles** — View the list of available cars for rent.
- **Select Rental Dates** — Choose start and end dates to rent a vehicle.
- **Automated Billing** — System calculates total cost based on rate per day and rental duration.
- **Return Vehicle Request** — Submit a return request when done using the vehicle.

### **Admin**
- **Add / Manage Cars** — Add new cars with details like model, daily rate, availability status.
- **Approve Return Requests** — Review and approve vehicle return requests.
- **View Statistics** — Track total rentals, revenue, and fleet availability.

---

## 🛠️ Technologies Used
- **ASP.NET MVC** — Backend framework
- **Entity Framework** — Database ORM
- **SQL Server** — Database
- **Bootstrap 5** — Frontend UI
- **C#** — Backend logic

---

## 📂 Project Structure
VehicleRentalSystem/
│
├── Controllers/ # MVC controllers for handling requests
├── Models/ # Entity Framework models
├── Views/ # Razor views for UI
├── Migrations/ # Database migrations
└── appsettings.json # Database configuration

## ⚙️ Installation & Setup

### 1️⃣ Clone Repository
git clone https://github.com/yourusername/VehicleRentalSystem.git


2️⃣ Open in Visual Studio
Open the .sln file in Visual Studio.

3️⃣ Configure Database
Update the connection string in appsettings.json to point to your SQL Server instance:

"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=VehicleRentalDB;Trusted_Connection=True;"
}


4️⃣ Apply Migrations
Open Package Manager Console and run:
Update-Database

