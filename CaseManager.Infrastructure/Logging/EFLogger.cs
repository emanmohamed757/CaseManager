using CaseManager.BusinessLogic.Data;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Interfaces.Logging;
using System;
using System.Data.Entity.Infrastructure;

namespace CaseManager.Infrastructure.Logging
{
    public class EFLogger : ILogger
    {
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContextFactory;

        public EFLogger(IDbContextFactory<CaseManagerDbContext> caseManagerDbContextFactory)
        {
            _caseManagerDbContextFactory = caseManagerDbContextFactory;
        }

        public void LogError(string message)
        {
            Log(message);
        }

        public void LogEvent(string message)
        {
            Log(message);
        }

        private void Log(string message)
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                dbContext.Logs.Add(new Log
                {
                    Message = message,
                    CreatedAt = DateTime.Now
                });

                dbContext.SaveChanges();
            }
        }
    }
}
