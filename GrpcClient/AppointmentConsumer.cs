using DoctorAppointmentWebApi.Messages;
using EasyNetQ;
using Grpc.Net.Client;
using GrpcService;

namespace GrpcClient
{
    public class AppointmentConsumer
    {
        private readonly IBus _bus;

        public AppointmentConsumer()
        {
            // Создание подключения через EasyNetQ
            _bus = RabbitHutch.CreateBus("amqp://guest:guest@localhost:5672");
        }

        public void StartListening()
        {
            Console.WriteLine("Listening for appointment messages...");

            _bus.PubSub.SubscribeAsync<AppointmentMessage>("appointment_subscription", async appointment =>
            {
                try
                {
                    Console.WriteLine($"[x] Received Appointment: {appointment.AppointmentId}");

                    // Вызов gRPC-сервиса для генерации кода
                    Console.WriteLine("[*] Attempting to call gRPC service...");
                    var confirmationCode = await GetConfirmationCodeFromGrpcService(appointment.AppointmentId, appointment.PatientId);

                    Console.WriteLine($"[x] Generated Confirmation Code for Appointment {appointment.AppointmentId}: {confirmationCode}");

                    // Отправка сгенерированного кода обратно в RabbitMQ
                    Console.WriteLine("[*] Attempting to publish confirmation code...");
                    PublishConfirmationCode(appointment.AppointmentId, confirmationCode);

                    Console.WriteLine("[x] Successfully processed appointment.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Error while processing appointment: {ex.Message}");
                }
            });

            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }


        private async Task<string> GetConfirmationCodeFromGrpcService(Guid appointmentId, Guid patientId)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress("http://localhost:5055", new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true
                    }
                });
                var client = new AppointmentService.AppointmentServiceClient(channel);

                // Запрос к gRPC-сервису
                var response = await client.GenerateConfirmationCodeAsync(new GenerateCodeRequest
                {
                    AppointmentId = appointmentId.ToString(),
                    PatientId = patientId.ToString()
                });

                return response.ConfirmationCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error in GetConfirmationCodeFromGrpcService: {ex.Message}");
                throw; // Проброс исключения
            }
        }


        private void PublishConfirmationCode(Guid appointmentId, string confirmationCode)
        {
            var message = new ConfirmationMessage
            {
                AppointmentId = appointmentId,
                ConfirmationCode = confirmationCode
            };

            _bus.PubSub.Publish(message);
            Console.WriteLine($"[x] Published Confirmation Code for Appointment {appointmentId}: {confirmationCode}");
        }
    }
}
