namespace ClinicQueueSystem.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Age { get; set; }
        public bool IsEmergency { get; set; }
        public DateTime RegistrationTime { get; set; }
        public int QueueNumber { get; set; }
        public bool IsServed { get; set; }
        public string Password { get; set; } // Add this property
    }
}