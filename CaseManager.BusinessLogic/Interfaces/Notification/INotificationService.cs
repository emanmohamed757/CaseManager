using System.Collections.Generic;

namespace CaseManager.BusinessLogic.Interfaces.Notification
{
    public interface INotificationService
    {
        void Notify(string message, IEnumerable<string> recepientList, IEnumerable<string> ccList);
    }
}
