namespace GrpcClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Запуск GrpcClient...");

            var consumer = new AppointmentConsumer();
            consumer.StartListening();
        }
    }
}
