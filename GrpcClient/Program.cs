namespace GrpcClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Appointment Consumer...");

            var consumer = new AppointmentConsumer();
            consumer.StartListening();
        }
    }
}
