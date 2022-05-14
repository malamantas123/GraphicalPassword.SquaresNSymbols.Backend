using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web.Http;
using GraphicalPassword.SquaresNSymbos.Backend;

namespace Server
{
    public class Function1
    {
        private readonly IDbContextFactory<ServerDbContext> m_serverDbContextFactory;
        
        public Function1(IDbContextFactory<ServerDbContext> contextFactory)
        {
            m_serverDbContextFactory = contextFactory;
        }

        [FunctionName("LoginUserAsync")]
        public async Task<IActionResult> LoginUserAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "login")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("LoginUserAsync was triggered");

            User user;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                var reqData = await streamReader.ReadToEndAsync();
                user = JsonConvert.DeserializeObject<User>(reqData);
            }

            using var dbContext = m_serverDbContextFactory.CreateDbContext();

            var dbUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == user.Email);

            if (dbUser == null)
                return new NotFoundObjectResult("User with specified email does not exist");

            if (dbUser.IncorrectLoginAttempts == 3)
                return new BadRequestObjectResult("Login using link provided in the email");

            if (SquaresNSymbols.ComparePasswords(dbUser.Password, user.Password))
            {
                dbUser.IncorrectLoginAttempts = 0;
                await dbContext.SaveChangesAsync();
                return new OkObjectResult(dbUser.Username);
            }
            else
            {
                if (++dbUser.IncorrectLoginAttempts == 3)
                {
                    try
                    {
                        SendEmail(dbUser.Email, "Login link", $"Use this link to login => http://localhost:3000/slogin?email={dbUser.Email}");
                        return new BadRequestObjectResult("Incorrect login attempt limit reached. Special login link has been sent to you via email");
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex.Message);
                        return new InternalServerErrorResult();
                    }
                }

                await dbContext.SaveChangesAsync();
                return new BadRequestObjectResult("Incorrect password");
            }
        }

        [FunctionName("SpecialLoginAsync")]
        public async Task<IActionResult> SpecialLoginAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "speciallogin")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SpecialLoginAsync was triggered");

            User user;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                var reqData = await streamReader.ReadToEndAsync();
                user = JsonConvert.DeserializeObject<User>(reqData);
            }

            using var dbContext = m_serverDbContextFactory.CreateDbContext();

            var dbUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == user.Email);

            if (dbUser == null)
                return new NotFoundObjectResult("User with specified email does not exist");

            if (SquaresNSymbols.ComparePasswords(dbUser.Password, user.Password))
            {
                dbUser.IncorrectLoginAttempts = 0;
                await dbContext.SaveChangesAsync();
                return new OkObjectResult(dbUser.Username);
            }
            else
            {
                return new BadRequestObjectResult("Incorrect password");
            }
        }

        [FunctionName("RegisterUserAsync")]
        public async Task<IActionResult> RegisterUserAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "register")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("RegisterUserAsync was triggered");

            User user;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                var reqData = await streamReader.ReadToEndAsync();
                user = JsonConvert.DeserializeObject<User>(reqData);
                user.IncorrectLoginAttempts = 0;
            }

            using var dbContext = m_serverDbContextFactory.CreateDbContext();

            if (await dbContext.Users.AnyAsync(x => x.Email == user.Email))
                return new System.Web.Http.ConflictResult();

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return new OkResult();
        }

        [FunctionName("SendPasswordResetEmailAsync")]
        public async Task<IActionResult> SendPasswordResetEmailAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "resetemail")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SendPasswordResetEmailAsync was triggered");

            var email = req.Query["email"].FirstOrDefault();

            using var dbContext = m_serverDbContextFactory.CreateDbContext();

            if (!await dbContext.Users.AnyAsync(x => x.Email == email))
                return new NotFoundObjectResult("User with specified email does not exist");

            try
            {
                SendEmail(email, "Password reset", $"Use this link to enter new password => http://localhost:3000/reset?email={email}");
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new InternalServerErrorResult();
            }

            return new OkResult();
        }

        [FunctionName("UpdatePasswordAsync")]
        public async Task<IActionResult> UpdatePasswordAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "updatepassword")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("UpdatePasswordAsync was triggered");

            User user;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                var reqData = await streamReader.ReadToEndAsync();
                user = JsonConvert.DeserializeObject<User>(reqData);
            }

            using var dbContext = m_serverDbContextFactory.CreateDbContext();

            var dbUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == user.Email);
            dbUser.Password = user.Password;

            await dbContext.SaveChangesAsync();
            return new OkResult();
        }

        public static void SendEmail(string recipientEmail, string subject, string body)
        {
            MailMessage message = new MailMessage();
            SmtpClient smtp = new SmtpClient();
            message.From = new MailAddress("squaresnsymbols@gmail.com");
            message.To.Add(new MailAddress(recipientEmail));
            message.Subject = subject;
            message.IsBodyHtml = false;
            message.Body = body;
            smtp.Port = 587;
            smtp.Host = "smtp.gmail.com";
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("squaresnsymbols@gmail.com", "Squares++11");
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
        }
    }
}
