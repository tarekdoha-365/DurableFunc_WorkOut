namespace FunctionLoversRegistration
{
    public class RegistrationResult
    {
        public RegistrationStatus RegistrationStatus { get; set; }
        public string Reason { get; set; }
    }
    public enum RegistrationStatus { Pending, NoValidData, Ok }
}