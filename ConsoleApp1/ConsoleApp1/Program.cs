using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp1
{
    public class Train
    {
        public int Id { get; set; }
        public string Carrier { get; set; }
        public float Mileage { get; set; }
        public string TechnicalNumber { get; set; }
        public DateOnly RegistrationDate { get; set; }
        public string ManagerPhoneNumber { get; set; }

        public static Train[] TestData() => new Train[]
        {
        new Train
        {
            Carrier = "XYZ Railways",
            Mileage = 1200.5f,
            TechnicalNumber = "TR12345",
            RegistrationDate = new DateOnly(2023, 1, 15),
            ManagerPhoneNumber = "+1234567890"
        },
        new Train
        {
            Carrier = "ABC Express",
            Mileage = 800.2f,
            TechnicalNumber = "TRS4321",
            RegistrationDate = new DateOnly(2023, 2, 20),
            ManagerPhoneNumber = "+9876543210"
        }
      };


    }
    public class ApplicationContext : DbContext
    {
        public DbSet<Train> Trains { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {

        }
    }

    public class DatabaseService
    {
        private DbContextOptions<ApplicationContext> GetConnectionOptions()
        {
            ConfigurationBuilder builder = new();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var config = builder.Build();
            string connectionString = config.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            return optionsBuilder.UseSqlServer(connectionString).Options;
        }

        public async Task EnsurePopulate()
        {
            using (ApplicationContext db = new ApplicationContext(GetConnectionOptions()))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                db.Trains.AddRange(Train.TestData());
                await db.SaveChangesAsync();
            }
        }
        public async Task<Train> GetTrainById(int id)
        {
            using (ApplicationContext db = new ApplicationContext(GetConnectionOptions()))
            {
                return await db.Trains.FirstOrDefaultAsync(e => e.Id == id);
            }
        }
        public async Task AddTrain(Train train)
        {
            using (ApplicationContext db = new ApplicationContext(GetConnectionOptions()))
            {
                db.Trains.Add(train);
                await db.SaveChangesAsync();
            }
        }

        public async Task UpdateTrain(Train train)
        {
            using (ApplicationContext db = new ApplicationContext(GetConnectionOptions()))
            {
                db.Trains.Update(train);
                await db.SaveChangesAsync();
            }
        }
        public async Task RemoveTrain(Train train)
        {
            using (ApplicationContext db = new ApplicationContext(GetConnectionOptions()))
            {
                db.Trains.Remove(train);
                await db.SaveChangesAsync();
            }
        }
    }

    class Program
    {
        static async Task Main()
        {
            DatabaseService databaseService = new DatabaseService();
            await databaseService.EnsurePopulate();

            await databaseService.AddTrain(new Train
            {
                Carrier = "BelX",
                Mileage = 19800.5f,
                TechnicalNumber = "KA321",
                RegistrationDate = new DateOnly(2002, 03, 12),
                ManagerPhoneNumber = "+3806892331"
            });

            var currentTrain = await databaseService.GetTrainById(3);
            if (currentTrain != null)
            {
                currentTrain.Mileage += 10_000;
                await databaseService.UpdateTrain(currentTrain);
            }
            var currentTrainToDelete = await databaseService.GetTrainById(1);
            if (currentTrainToDelete != null)
            {
                await databaseService.RemoveTrain(currentTrainToDelete);
            }
        }
    }
}

