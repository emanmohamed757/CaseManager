namespace CaseManager.BusinessLogic.Interfaces
{
    public interface ILogger
    {
        void LogEvent(string message);

        void LogError(string message);
    }
}
