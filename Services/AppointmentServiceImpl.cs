using Grpc.Core;

namespace GrpcService.Services
{
    public class AppointmentServiceImpl : AppointmentService.AppointmentServiceBase
    {
        public override Task<GenerateCodeResponse> GenerateConfirmationCode(GenerateCodeRequest request, ServerCallContext context)
        {
            // Генерация уникального кода
            var confirmationCode = GenerateUniqueCode();

            Console.WriteLine($"[x] Generated Confirmation Code: {confirmationCode} for AppointmentId: {request.AppointmentId}");

            return Task.FromResult(new GenerateCodeResponse
            {
                ConfirmationCode = confirmationCode
            });
        }

        private string GenerateUniqueCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}