namespace AccessControl.Application.Services.LogService
{
    public interface ILogService
    {
        Task LogToDatabaseAsync(LogServiceRequest logEntry);
    }
}
