using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Interfaces.Notification;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;

namespace CaseManager.Infrastructure.Notification
{
    public class EFNotificationService : INotificationService
    {
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContextFactory;

        public EFNotificationService(IDbContextFactory<CaseManagerDbContext> caseManagerDbContextFactory)
        {
            _caseManagerDbContextFactory = caseManagerDbContextFactory;
        }

        public void Notify(string message, IEnumerable<string> recepientList, IEnumerable<string> ccList)
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                dbContext.Emails.Add(new Email
                {
                    CCList = ccList == null ? null : string.Join(",", ccList),
                    RecipientList = recepientList == null ? null : string.Join(",", recepientList),
                    Message = message,
                    Sender = "notification@email.com",
                });

                dbContext.SaveChanges();
            }
        }
    }
}
