using System.Collections.Generic;

namespace CaseManager.BusinessLogic.Interfaces.Notification
{
    public interface INotificationService
    {
        void Notify(string sender, string message, IEnumerable<string> recepientList, IEnumerable<string> ccList);
    }
}
