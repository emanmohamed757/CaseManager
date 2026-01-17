using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Exceptions;
using CaseManager.BusinessLogic.Interfaces.Logging;
using CaseManager.BusinessLogic.Interfaces.Notification;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class ConflictOfInterestService
    {
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContextFactory;

        private readonly ILogger _logger;

        private readonly UserContext _userContext;

        private readonly INotificationService _notificationService;

        public ConflictOfInterestService(
            IDbContextFactory<CaseManagerDbContext> caseManagerDbContextFactory,
            ILogger logger,
            UserContext userContext,
            INotificationService notificationService)
        {
            _caseManagerDbContextFactory = caseManagerDbContextFactory;
            _logger = logger;
            _userContext = userContext;
            _notificationService = notificationService;
        }

        public void DeclareConflictOfInterest(int caseId)
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                if (dbContext.ConflictOfInterests.Any(conflict => 
                    conflict.CaseId == caseId 
                    && conflict.Username == _userContext.Username))
                {
                    throw new ConfictOfInterestDeclarationException();
                }
            }
        }
    }
}
