using System.Linq;
using System.Web.Mvc;
using VehicleRentalSystem.Models;

public class AccountController : Controller
{
    VehicleRentalDbEntities db = new VehicleRentalDbEntities();

    // GET: Login
    public ActionResult Login()
    {
        return View();
    }

    // POST: Login
    [HttpPost]
    public ActionResult Login(string username, string password)
    {
        var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        if (user != null)
        {
            Session["UserId"] = user.Id;
            Session["Username"] = user.Username;
            Session["Role"] = user.Role;

            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Dashboard", "Customer");
        }

        ViewBag.Message = "Invalid username or password.";
        return View();
    }

    // GET: Logout
    public ActionResult Logout()
    {
        Session.Clear();
        return RedirectToAction("Login", "Account");
    }
}
