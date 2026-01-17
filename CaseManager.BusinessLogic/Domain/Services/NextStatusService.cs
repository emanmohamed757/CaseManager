using CaseManager.BusinessLogic.Domain.Enums;
using System.Collections.Generic;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class NextStatusService
    {
        public List<CaseStatusOption> GetNextStatuses(CaseStatusOption currentStatus)
        {
            var nextStatuses = new List<CaseStatusOption>();

            switch (currentStatus)
            {
                case CaseStatusOption.Proposed:
                    nextStatuses.Add(CaseStatusOption.Approved);
                    nextStatuses.Add(CaseStatusOption.Rejected);
                    break;
                case CaseStatusOption.Approved:
                    nextStatuses.Add(CaseStatusOption.Assigned);
                    break;
                case CaseStatusOption.Assigned:
                    nextStatuses.Add(CaseStatusOption.Planning);
                    nextStatuses.Add(CaseStatusOption.OnHold);
                    break;
                case CaseStatusOption.Planning:
                    nextStatuses.Add(CaseStatusOption.InProgress);
                    nextStatuses.Add(CaseStatusOption.OnHold);
                    break;
                case CaseStatusOption.InProgress:
                    nextStatuses.Add(CaseStatusOption.PendingReview);
                    nextStatuses.Add(CaseStatusOption.Disputed);
                    nextStatuses.Add(CaseStatusOption.OnHold);
                    break;
                case CaseStatusOption.PendingReview:
                    nextStatuses.Add(CaseStatusOption.Disputed);
                    break;
                case CaseStatusOption.Disputed:
                    nextStatuses.Add(CaseStatusOption.InProgress);
                    break;
                case CaseStatusOption.OnHold:
                    nextStatuses.Add(CaseStatusOption.InProgress);
                    nextStatuses.Add(CaseStatusOption.Planning);
                    nextStatuses.Add(CaseStatusOption.Assigned);
                    break;
                case CaseStatusOption.Rejected:
                    nextStatuses.Add(CaseStatusOption.Proposed);
                    break;
            }

            return nextStatuses;
        }
    }
}
