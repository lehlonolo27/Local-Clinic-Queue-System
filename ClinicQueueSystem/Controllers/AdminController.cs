using ClinicQueueSystem.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClinicQueueSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var served = _context.Patients.Where(p => p.IsServed);
            double avgWait = served.Any() ? served.Average(p => (DateTime.Now - p.RegistrationTime).TotalMinutes) : 0;
            int peakHour = _context.Patients
                .GroupBy(p => p.RegistrationTime.Hour)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            ViewBag.AverageWait = avgWait;
            ViewBag.PeakHour = peakHour;
            return View();
        }
    }
}
