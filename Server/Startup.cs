using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Web.Http;

[assembly: FunctionsStartup(typeof(Server.Startup))]
namespace Server
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string cs = @"..\..\..\test.db";

            if (!File.Exists(cs))
            {
                SQLiteConnection.CreateFile(cs);
                var con = new SQLiteConnection($"Data Source={cs};Version=3;");
                con.Open();

                using var cmd = new SQLiteCommand(con);

                cmd.CommandText = @"CREATE TABLE Users(userId INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT,
                    email TEXT, password TEXT, incorrectLoginAttempts INT)";
                cmd.ExecuteNonQuery();

                con.Close();
            }

            builder.Services.AddDbContextFactory<ServerDbContext>(options => options.UseSqlite($"Data Source={cs}"));            
        }
    }
}