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
            _bus = RabbitHutch.CreateBus("amqp://guest:guest@localhost:5672");
        }

        public void StartListening()
        {
            Console.WriteLine("Слушаем очередь и ожидаем appointment...");

            _bus.PubSub.SubscribeAsync<AppointmentMessage>("appointment_subscription", async appointment =>
            {
                try
                {
                    Console.WriteLine($"[x] Полученный Appointment: {appointment.AppointmentId}");
                    
                    Console.WriteLine("[*] Стучимся в gRPC Service...");
                    var confirmationCode = await GetConfirmationCodeFromGrpcService(appointment.AppointmentId, appointment.PatientId);

                    Console.WriteLine($"[v] Сгенерирован уникальный код для appointment - {appointment.AppointmentId}: {confirmationCode}");
                    
                    Console.WriteLine("[*] Формируем сообщение и отправляем код в RabbitMQ...");
                    PublishConfirmationCode(appointment.AppointmentId, confirmationCode);

                    Console.WriteLine("[v] Сообщение успешно отправлено.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Ошибка при получении записи: {ex.Message}");
                }
            });

            Console.WriteLine("Нажмите любую кнопку для завершения процесса");
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
                
                var response = await client.GenerateConfirmationCodeAsync(new GenerateCodeRequest
                {
                    AppointmentId = appointmentId.ToString(),
                    PatientId = patientId.ToString()
                });

                return response.ConfirmationCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Ошибка при выполнении метода <<GetConfirmationCodeFromGrpcService>>: {ex.Message}");
                throw;
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
            Console.WriteLine($"[v] Сгенерированный уникальный код для записи - {appointmentId}: {confirmationCode}" +
                              $" был передан в RabbitMQ для дальнейших действий");
        }
    }
}
