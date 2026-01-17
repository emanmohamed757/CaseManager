using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Services;
using System.Collections.Generic;
using Xunit;

namespace CaseManager.Tests
{
    public class NextStatusServiceMethod
    {
        private readonly NextStatusService _nextStatusService = new NextStatusService();

        [Theory]
        [InlineData(CaseStatusOption.Proposed, new[] { CaseStatusOption.Approved, CaseStatusOption.Rejected })]
        [InlineData(CaseStatusOption.Approved, new[] { CaseStatusOption.Assigned })]
        [InlineData(CaseStatusOption.Assigned, new[] { CaseStatusOption.Planning, CaseStatusOption.OnHold })]
        [InlineData(CaseStatusOption.Planning, new[] { CaseStatusOption.InProgress, CaseStatusOption.OnHold })]
        [InlineData(CaseStatusOption.InProgress, new[] { CaseStatusOption.PendingReview, CaseStatusOption.Disputed, CaseStatusOption.OnHold })]
        [InlineData(CaseStatusOption.PendingReview, new[] { CaseStatusOption.Disputed })]
        [InlineData(CaseStatusOption.Disputed, new[] { CaseStatusOption.InProgress })]
        [InlineData(CaseStatusOption.OnHold, new[] { CaseStatusOption.InProgress, CaseStatusOption.Planning, CaseStatusOption.Assigned })]
        [InlineData(CaseStatusOption.Rejected, new[] { CaseStatusOption.Proposed })]
        [InlineData(CaseStatusOption.Closed, new CaseStatusOption[0])]
        public void GetNextStatuses_ReturnsTheCorrectStatuses(
            CaseStatusOption currentStatus,
            CaseStatusOption[] expectedNextStatuses)
        {
            // Act.
            List<CaseStatusOption> nextStatuses = _nextStatusService.GetNextStatuses(currentStatus);

            // Assert.
            Assert.Equal(expectedNextStatuses, nextStatuses);
        }
    }
}
