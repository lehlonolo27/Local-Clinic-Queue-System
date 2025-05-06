using ClinicQueueSystem.Data;
using ClinicQueueSystem.Models;
using ClinicQueueSystem.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClinicQueueSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<QueueHub> _hubContext;

        public PatientController(ApplicationDbContext context, IHubContext<QueueHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var queue = await _context.Patients
                .Where(p => !p.IsServed)
                .OrderByDescending(p => p.IsEmergency)
                .ThenByDescending(p => p.Age >= 65)
                .ThenBy(p => p.QueueNumber)
                .ToListAsync();

            return View(queue);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Patient patient)
        {
            if (ModelState.IsValid)
            {
                patient.RegistrationTime = DateTime.Now;

                // Automatically serve emergency patients
                if (patient.IsEmergency)
                {
                    patient.IsServed = true;
                }

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                // Notify clients about the queue update
                await _hubContext.Clients.All.SendAsync("QueueUpdated");

                if (patient.IsEmergency)
                {
                    return RedirectToAction("Confirmation", new { id = patient.Id });
                }

                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var viewModel = new ConfirmationViewModel
            {
                Message = $"Patient {patient.Name} has been successfully registered with Queue Number {patient.QueueNumber}."
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsServed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                patient.IsServed = true;
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("QueueUpdated");
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> GetQueuePartial()
        {
            var queue = await _context.Patients
                .Where(p => !p.IsServed)
                .OrderByDescending(p => p.IsEmergency)
                .ThenByDescending(p => p.Age >= 65)
                .ThenBy(p => p.QueueNumber)
                .ToListAsync();
            return PartialView("_QueuePartial", queue);
        }

        public async Task<IActionResult> ServePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            patient.IsServed = true;
            await _context.SaveChangesAsync();

            // Notify clients about the queue update
            await _hubContext.Clients.All.SendAsync("QueueUpdated");

            return RedirectToAction(nameof(Index));
        }
    }
}
