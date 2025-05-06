using ClinicQueueSystem.Data;
using ClinicQueueSystem.Models;
using ClinicQueueSystem.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ClinicQueueSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<QueueHub> _hubContext;
        private readonly IConfiguration _configuration;

        public PatientController(ApplicationDbContext context, IHubContext<QueueHub> hubContext, IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _configuration = configuration;
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
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] Patient patient)
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

                return Ok(new { message = "Patient registered successfully" });
            }

            return BadRequest(ModelState);
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            var patient = _context.Patients.FirstOrDefault(p => p.Name == loginRequest.Name && p.Password == loginRequest.Password);
            if (patient == null)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var token = GenerateJwtToken(patient);
            return Ok(new { token });
        }

        private string GenerateJwtToken(Patient patient)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, patient.Name),
                new Claim("PatientId", patient.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
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
