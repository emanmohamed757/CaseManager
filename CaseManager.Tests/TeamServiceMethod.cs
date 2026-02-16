using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.BusinessLogic.Interfaces.Notification;
using Effort.DataLoaders;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using Xunit;

namespace CaseManager.Tests
{
    public class TeamServiceMethod : IDisposable
    {
        private readonly CaseManagerDbContext _arrangeCaseManagerDbContext;

        private readonly CaseManagerDbContext _assertCaseManagerDbContext;

        private readonly Mock<UserContext> _mockUserContext;

        private readonly TeamService _teamService;

        public TeamServiceMethod()
        {
            _mockUserContext = new Mock<UserContext>();
            _mockUserContext.SetupAllProperties();
            _mockUserContext.Object.Username = "testDefaultCurrentUser";

            // Create the connection that exists for the lifetime of this class, and hence the lifetime of EACH test.
            // dataLoader for loading static data.
            var dataLoader = new EntityDataLoader("name=CaseManagerDbContext");
            DbConnection connection = Effort.EntityConnectionFactory.CreateTransient("name=CaseManagerDbContext", dataLoader);

            _arrangeCaseManagerDbContext = new CaseManagerDbContext(connection);
            _assertCaseManagerDbContext = new CaseManagerDbContext(connection);

            var mockCaseManagerDbContextFactory = new Mock<IDbContextFactory<CaseManagerDbContext>>();
            mockCaseManagerDbContextFactory.Setup(x => x.Create()).Returns(() =>
            {
                return new CaseManagerDbContext(connection);
            });

            _teamService = new TeamService(mockCaseManagerDbContextFactory.Object, _mockUserContext.Object);
        }

        public void Dispose()
        {
            _arrangeCaseManagerDbContext.Dispose();
            _assertCaseManagerDbContext.Dispose();
        }

        //[Fact]
        public void GetImmediateSubordinates_ReturnsOnlyThoseWhoAreInTheTeamAssignedToYou()
        {
            // Arrange.
            Team yourTeam = new Team
            {
                DepartmentId = (int)DepartmentOption.Audit1,
                SupervisorUsername = "testDefaultCurrentUser",
                TeamMembers = new List<TeamMember>
                {
                    new TeamMember
                    {
                        Username = "member1",
                        IsTeamLeader = true,
                    },
                    new TeamMember
                    {
                        Username = "member2",
                        IsTeamLeader = false,
                    },
                }
            };

            Team anotherTeam = new Team
            {
                DepartmentId = (int)DepartmentOption.Audit1,
                SupervisorUsername = "someOtherUser",
                TeamMembers = new List<TeamMember>
                {
                    new TeamMember
                    {
                        Username = "member3",
                        IsTeamLeader = true,
                    },
                    new TeamMember
                    {
                        Username = "member4",
                        IsTeamLeader = false,
                    },
                }
            };

            _arrangeCaseManagerDbContext.Teams.Add(yourTeam);
            _arrangeCaseManagerDbContext.Teams.Add(anotherTeam);
            _arrangeCaseManagerDbContext.SaveChanges();

            List<string> subordinates = _teamService.GetImmediateSubordinates();

            Assert.Contains("member1", subordinates);
            Assert.Contains("member2", subordinates);
            Assert.DoesNotContain("member3", subordinates);
            Assert.DoesNotContain("member4", subordinates);
        }
    }
}
