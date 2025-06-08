using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

    namespace ConsoleApp2
    {
        public class Movie
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public float Rating { get; set; }
            public int ReleaseYear { get; set; }
        }

        public class DailyTask
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime DueDate { get; set; }
        }

        public class ApplicationContext : DbContext
        {
            public DbSet<Movie> Movies { get; set; }
            public DbSet<DailyTask> Tasks { get; set; }

            public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
            {
            }
        }
        public class DatabaseService
        {
            private readonly DbContextOptions<ApplicationContext> _options;

            public DatabaseService()
            {
                _options = GetConnectionOptions();
            }

            private DbContextOptions<ApplicationContext> GetConnectionOptions()
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsetting.json");

            var config = builder.Build();
                string connectionString = config.GetConnectionString("DefaultConnection");

                return new DbContextOptionsBuilder<ApplicationContext>()
                    .UseSqlServer(connectionString)
                    .Options;
            }

            public async Task InitializeDatabase()
            {
                using var db = new ApplicationContext(_options);
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }

            public async Task AddMovies(List<Movie> movies)
            {
                using var db = new ApplicationContext(_options);
                await db.Movies.AddRangeAsync(movies);
                await db.SaveChangesAsync();
            }

            public async Task<List<Movie>> GetMoviesByRating(float minRating, float maxRating)
            {
                using var db = new ApplicationContext(_options);
                return await db.Movies
                    .Where(m => m.Rating >= minRating && m.Rating <= maxRating)
                    .ToListAsync();
            }

            public async Task UpdateMovies(List<Movie> movies)
            {
                using var db = new ApplicationContext(_options);
                db.Movies.UpdateRange(movies);
                await db.SaveChangesAsync();
            }

            public async Task DeleteLowRatedMovies(float ratingThreshold)
            {
                using var db = new ApplicationContext(_options);
                var lowRatedMovies = await db.Movies
                    .Where(m => m.Rating < ratingThreshold)
                    .ToListAsync();

                db.Movies.RemoveRange(lowRatedMovies);
                await db.SaveChangesAsync();
            }

            public async Task IncrementOldMoviesRating(int yearThreshold)
            {
                using var db = new ApplicationContext(_options);
                await db.Movies
                    .Where(m => m.ReleaseYear < yearThreshold)
                    .ForEachAsync(m => m.Rating += 0.1f);

                await db.SaveChangesAsync();
            }

            public async Task AddTask(DailyTask task)
            {
                using var db = new ApplicationContext(_options);
                await db.Tasks.AddAsync(task);
                await db.SaveChangesAsync();
            }

            public async Task<List<DailyTask>> GetAllTasks()
            {
                using var db = new ApplicationContext(_options);
                return await db.Tasks.ToListAsync();
            }

            public async Task MarkTaskCompleted(int taskId)
            {
                using var db = new ApplicationContext(_options);
                var task = await db.Tasks.FindAsync(taskId);
                if (task != null)
                {
                    task.IsCompleted = true;
                    await db.SaveChangesAsync();
                }
            }
            public async Task<List<DailyTask>> GetTodaysTasks()
            {
                using var db = new ApplicationContext(_options);
                var today = DateTime.Today;
                return await db.Tasks
                    .Where(t => t.DueDate.Date == today && !t.IsCompleted)
                    .ToListAsync();
            }
            public async Task DeleteCompletedTasks()
            {
                using var db = new ApplicationContext(_options);
                var completedTasks = await db.Tasks
                    .Where(t => t.IsCompleted)
                    .ToListAsync();

                db.Tasks.RemoveRange(completedTasks);
                await db.SaveChangesAsync();
            }
        }

        class Program
        {
            static async Task Main()
            {
                var dbService = new DatabaseService();
                await dbService.InitializeDatabase();

                //1
                var newMovies = new List<Movie>
            {
                new Movie { Title = "Inception", Rating = 8.8f, ReleaseYear = 2010 },
                new Movie { Title = "The Matrix", Rating = 8.7f, ReleaseYear = 1999 },
                new Movie { Title = "Interstellar", Rating = 8.6f, ReleaseYear = 2014 }
            };
                await dbService.AddMovies(newMovies);

                //2
                var moviesInRange = await dbService.GetMoviesByRating(7.0f, 8.5f);
                Console.WriteLine("Movies in rating range:");
                foreach (var movie in moviesInRange)
                {
                    Console.WriteLine($"{movie.Title} - {movie.Rating}");
                }

                //3
                var moviesToUpdate = await dbService.GetMoviesByRating(8.5f, 9.0f);
                foreach (var movie in moviesToUpdate)
                {
                    movie.Rating += 0.1f;
                }
                await dbService.UpdateMovies(moviesToUpdate);

                //4
                await dbService.DeleteLowRatedMovies(3.4f);

                //5
                var movieToEdit = (await dbService.GetMoviesByRating(8.0f, 9.0f)).First();
                movieToEdit.Rating = 9.0f;
                await dbService.UpdateMovies(new List<Movie> { movieToEdit });

                //6
                await dbService.IncrementOldMoviesRating(2005);

                await dbService.AddTask(new DailyTask
                {
                    Description = "Buy groceries",
                    DueDate = DateTime.Today
                });

                var allTasks = await dbService.GetAllTasks();
                Console.WriteLine("\nAll tasks:");
                foreach (var task in allTasks)
                {
                    Console.WriteLine($"{task.Description} - {task.DueDate:d} - Completed: {task.IsCompleted}");
                }

                if (allTasks.Count > 0)
                {
                    await dbService.MarkTaskCompleted(allTasks[0].Id);
                }

                var todaysTasks = await dbService.GetTodaysTasks();
                Console.WriteLine("\nToday's tasks:");
                foreach (var task in todaysTasks)
                {
                    Console.WriteLine(task.Description);
                }

                await dbService.DeleteCompletedTasks();

                Console.WriteLine("\nOperations completed!");
            }
        }
    }
