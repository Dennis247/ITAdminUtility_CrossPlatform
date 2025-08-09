using ITAdmin.Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ITAdmin.Shared.Services
{
    public class DatabaseService
    {
        public static async Task InitializeDatabaseAsync()
        {
            using var context = new AppDbContext();
            await context.Database.EnsureCreatedAsync();
        }

        public static void InitializeDatabase()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
        }
    }
}