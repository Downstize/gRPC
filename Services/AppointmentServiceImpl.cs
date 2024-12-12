using Grpc.Core;

namespace GrpcService.Services
{
    public class AppointmentServiceImpl : AppointmentService.AppointmentServiceBase
    {
        public override Task<GenerateCodeResponse> GenerateConfirmationCode(GenerateCodeRequest request, ServerCallContext context)
        {
            try
            {
                var confirmationCode = GenerateUniqueCode(request.AppointmentId, request.PatientId);

                Console.WriteLine($"[x] Сгенерирован уникальный код записи: {confirmationCode} для пациента с уникальным идентификатором: {request.PatientId}," +
                                  $" с уникальным идентификатором записи: {request.AppointmentId}");

                return Task.FromResult(new GenerateCodeResponse
                {
                    ConfirmationCode = confirmationCode
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Ошибка при выполнении метода <<GenerateConfirmationCode>>: {ex.Message}");
                throw;
            }
        }


        private string GenerateUniqueCode(string appointmentId, string patientId)
        {
            var combinedString = $"{appointmentId}-{patientId}";

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combinedString));
                
                return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16).ToUpper();
            }
        }

    }
}