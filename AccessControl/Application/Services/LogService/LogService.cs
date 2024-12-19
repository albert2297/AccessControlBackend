using AccessControl.Infrastructure.Persistence.DbContext;
using AccessControl.Infrastructure.Persistence.Entities;

namespace AccessControl.Application.Services.LogService
{
    public class LogService(AccessControlDbContext context) : ILogService
    {
        public async Task LogToDatabaseAsync(LogServiceRequest logEntry)
        {
            var datetime = DateTime.Now;
            var log = new LogEntity
            {
                EventName = logEntry.EventName,
                Details = logEntry.Details,
                UserId = logEntry.UserId,
                Timestamp = datetime,
                Email = logEntry.Email,
                CreatedDate = datetime,
                UpdatedDate = datetime
            };

            context.LogEntities.Add(log);
            await context.SaveChangesAsync();
        }
    }
}
