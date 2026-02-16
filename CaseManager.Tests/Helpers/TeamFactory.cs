using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Enums;
using System;
using System.Collections.Generic;

namespace CaseManager.Tests.Helpers
{
    internal class TeamFactory
    {
        private readonly CaseManagerDbContext _dbContext;

        public TeamFactory(CaseManagerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void CreateTeam(
            Case caseToDeclareConflictOfInterestOn,
            IEnumerable<TeamMemberSummary> teamMemberSummaries)
        {
            (List<Team> teams, List<ConflictOfInterest> conflicts) = CreateTeamHelper(teamMemberSummaries, caseToDeclareConflictOfInterestOn);
            _dbContext.Teams.AddRange(teams);
            _dbContext.ConflictOfInterests.AddRange(conflicts);
            _dbContext.SaveChanges();
        }

        private (List<Team>, List<ConflictOfInterest>) CreateTeamHelper(
            IEnumerable<TeamMemberSummary> teamMemberSummaries,
            Case caseToDeclareConflictOfInterestOn)
        {
            if (teamMemberSummaries == null) return (null, null);

            var teams = new List<Team>();
            var conflicts = new List<ConflictOfInterest>();
            foreach (TeamMemberSummary rootTeamMemberSummary in teamMemberSummaries)
            {
                if (rootTeamMemberSummary.TeamMembers == null) continue;

                var team = new Team
                {
                    DepartmentId = rootTeamMemberSummary.DepartmentId ?? (int)DepartmentOption.Audit1,
                    SupervisorUsername = rootTeamMemberSummary.Username,
                };
                teams.Add(team);

                bool isTeamLeaderSet = false;
                foreach(TeamMemberSummary teamMemberSummary in rootTeamMemberSummary.TeamMembers)
                {
                    var teamMember = new TeamMember
                    {
                        IsTeamLeader = !isTeamLeaderSet,
                        Username = teamMemberSummary.Username,
                    };
                    isTeamLeaderSet = true;
                    team.TeamMembers.Add(teamMember);

                    if (teamMemberSummary.HasConflictOfInterest)
                    {
                        conflicts.Add(new ConflictOfInterest
                        {
                            Case = caseToDeclareConflictOfInterestOn,
                            DeclaredDate = DateTime.Now,
                            StaffDesignationId = teamMemberSummary.DesignationId,
                            Username = teamMemberSummary.Username
                        });
                    }

                    (List<Team> teamMemberTeams, List<ConflictOfInterest> teamMemberTeamConflicts) =
                        CreateTeamHelper(teamMemberSummary.TeamMembers, caseToDeclareConflictOfInterestOn);

                    if (teamMemberTeams != null) teams.AddRange(teamMemberTeams);
                    if (teamMemberTeamConflicts != null) conflicts.AddRange(teamMemberTeamConflicts);
                }
            }

            return (teams, conflicts);
        }
    }
}

//internal class TeamMemberSummary
//{
//    public string Username { get; set; }

//    public string Name { get; set; }

//    public IEnumerable<TeamMemberSummary> TeamMembers { get; set; }
//}