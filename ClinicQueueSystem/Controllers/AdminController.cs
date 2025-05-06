using Microsoft.AspNetCore.Mvc;
using ClinicQueueSystem.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ClinicQueueSystem.Models;
using Microsoft.AspNetCore.Http;

namespace ClinicQueueSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            var patientsToday = await _context.Patients
                .Where(p => p.RegistrationTime.Date == today)
                .ToListAsync();

            var servedToday = patientsToday.Count(p => p.IsServed);
            var emergencyCount = patientsToday.Count(p => p.IsEmergency);

            var avgWaitTime = patientsToday
                .Where(p => p.IsServed)
                .Select(p => (DateTime.Now - p.RegistrationTime).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var peakHour = patientsToday
                .GroupBy(p => p.RegistrationTime.Hour)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            ViewBag.AvgWaitTime = Math.Round(avgWaitTime, 2);
            ViewBag.PeakHour = peakHour;
            ViewBag.ServedToday = servedToday;
            ViewBag.EmergencyCount = emergencyCount;

            return View();
        }

        public async Task<IActionResult> PatientsServedToday()
        {
            var today = DateTime.Today;

            var patientsServedToday = await _context.Patients
                .Where(p => p.IsServed && p.RegistrationTime.Date == today)
                .ToListAsync();

            return View(patientsServedToday);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Admin admin)
        {
            if (ModelState.IsValid)
            {
                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard", "Admin"); // Redirect to Dashboard
            }

            return View(admin); // Return to the registration form if validation fails
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == username && a.Password == password);

            if (admin != null)
            {
                HttpContext.Session.SetString("AdminUsername", admin.Username);
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
