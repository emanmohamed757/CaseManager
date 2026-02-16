using System.Collections.Generic;

namespace CaseManager.Tests.Helpers
{
    internal class TeamMemberSummary
    {
        public string Username { get; set; }

        public int? DepartmentId { get; set; }

        public bool HasConflictOfInterest { get; set; }

        public int DesignationId { get; set; }

        public IEnumerable<TeamMemberSummary> TeamMembers { get; set; }
    }
}
