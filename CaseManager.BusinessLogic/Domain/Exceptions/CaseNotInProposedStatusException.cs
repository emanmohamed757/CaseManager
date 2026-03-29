using System;

namespace CaseManager.BusinessLogic.Domain.Exceptions
{
    public class CaseNotInProposedStatusException : Exception
    {
        public CaseNotInProposedStatusException()
            : base("The case must be in the proposed status for it to be approved.")
        {
        }
    }
}
