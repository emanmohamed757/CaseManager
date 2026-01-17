namespace CaseManager.BusinessLogic.Interfaces.Logging
{
    public interface ILogger
    {
        void LogEvent(string message);

        void LogError(string message);
    }
}
