using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Exceptions;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.BusinessLogic.Interfaces.Logging;
using CaseManager.BusinessLogic.Interfaces.Notification;
using Effort.DataLoaders;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Xunit;

namespace CaseManager.Tests
{
    public class CaseServiceShould : IDisposable
    {
        private readonly CaseService _caseService;

        private readonly CaseManagerDbContext _arrangeCaseManagerDbContext;

        private readonly CaseManagerDbContext _assertCaseManagerDbContext;

        private readonly Mock<ILogger> _mockLogger;

        private readonly Mock<INotificationService> _mockNotificationService;

        private readonly Mock<UserContext> _mockUserContext;

        public CaseServiceShould()
        {
            _mockLogger = new Mock<ILogger>();
            _mockNotificationService = new Mock<INotificationService>();
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

            var nextStatusService = new NextStatusService();

            // This is the system under test.
            _caseService = new CaseService(
                mockCaseManagerDbContextFactory.Object,
                _mockLogger.Object,
                _mockUserContext.Object,
                _mockNotificationService.Object,
                nextStatusService);
        }

        public void Dispose()
        {
            _arrangeCaseManagerDbContext.Dispose();
            _assertCaseManagerDbContext.Dispose();
        }

        #region CreateCase
        [Fact]
        public void WhenACaseIsCreated_SaveCaseToDatabase()
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "1"
            };

            // Act.
            _caseService.CreateCase(@case);

            // Assert.
            Assert.NotEqual(0, @case.Id);
        }

        [Fact]
        public void WhenACaseIsCreated_SetCaseAuditProperties()
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "1"
            };

            // Act.
            _caseService.CreateCase(@case);

            // Assert.
            Assert.True(@case.CreatedBy != null && @case.UpdatedBy != null && @case.CreatedAt != null && @case.UpdatedAt != null);
        }

        [Fact]
        public void CreateCase_SetsCreatedByAndUpdatedByToCurrentUser()
        {
            // Arrange.
            var @case = new Case
            {
                CaseNumber = "1"
            };

            // Act.
            _caseService.CreateCase(@case);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal(_mockUserContext.Object.Username, updatedCase.CreatedBy);
            Assert.Equal(_mockUserContext.Object.Username, updatedCase.UpdatedBy);
        }

        [Fact]
        public void WhenACaseIsCreated_LogAnEvent()
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "1"
            };

            // Act.
            _caseService.CreateCase(@case);

            // Assert.
            _mockLogger.Verify(x => x.LogEvent(It.IsAny<string>()));
        }

        [Fact]
        public void WhenACaseIsCreated_AssignTheProposedStatusToCase()
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "1"
            };

            // Act.
            _caseService.CreateCase(@case);

            // Assert.
            Assert.True(@case.StatusId == (int)CaseStatusOption.Proposed);
        }

        [Theory]
        [InlineData(DepartmentOption.Audit1)]
        [InlineData(DepartmentOption.Legal)]
        [InlineData(DepartmentOption.Audit2)]
        [InlineData(DepartmentOption.Audit3)]
        public void CreateCase_AssignsTheCorrectDepartmentIdToCase(
            DepartmentOption departmentOption)
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "1"
            };
            _mockUserContext.Object.DepartmentId = (int)departmentOption;

            // Act.
            _caseService.CreateCase(@case);

            // Assert.
            Assert.True(@case.DepartmentId == (int)departmentOption);
        }
        #endregion

        #region ApproveCase
        [Fact]
        public void ApproveCase_Should_AssignTheApprovedStatusToCase()
        {
            // Arrange.
            // Add data for testing.
            _arrangeCaseManagerDbContext.Cases.Add(new Case
            {
                CaseNumber = "2026/001 (Proposed Case)",
                StatusId = 1,
                CreatedAt = DateTime.Now,
                CreatedBy = "eaman",
                UpdatedAt = DateTime.Now,
                UpdatedBy = "eaman",
                IsDeleted = false
            });
            _arrangeCaseManagerDbContext.SaveChanges();

            // TODO: Is this an integration or unit test?
            Case caseWithProposedStatus = _arrangeCaseManagerDbContext.Cases
                .FirstOrDefault(c => c.StatusId == (int)CaseStatusOption.Proposed);

            // Act.
            _caseService.ApproveCase(caseWithProposedStatus.Id);

            // Assert.
            Case @case = _assertCaseManagerDbContext.Cases.Find(caseWithProposedStatus.Id);
            Assert.True(@case.StatusId == (int)CaseStatusOption.Approved);
        }

        [Fact]
        public void ApproveCase_SendsAnEmailToStaffWhoCreatedTheCase()
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "2026/001 (Proposed Case)",
                StatusId = 1,
                CreatedAt = DateTime.Now,
                CreatedBy = "someUser",
                UpdatedAt = DateTime.Now,
                UpdatedBy = "someUser",
                IsDeleted = false
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            _caseService.ApproveCase(@case.Id);

            _mockNotificationService.Verify(x =>
                x.Notify(
                    It.IsAny<string>(),
                    CollectionMatcher(new string[] { "someUser" }),
                    It.Is<IEnumerable<string>>(list => list == null || !list.Any())));
        }

        [Fact]
        public void ApproveCase_LogsAnEvent()
        {
            // Arrange.
            // Add data for testing.
            _arrangeCaseManagerDbContext.Cases.Add(new Case
            {
                CaseNumber = "2026/001 (Proposed Case)",
                StatusId = 1,
                CreatedAt = DateTime.Now,
                CreatedBy = "eaman",
                UpdatedAt = DateTime.Now,
                UpdatedBy = "eaman",
                IsDeleted = false
            });
            _arrangeCaseManagerDbContext.SaveChanges();

            Case caseWithProposedStatus = _arrangeCaseManagerDbContext.Cases
                .FirstOrDefault(c => c.StatusId == (int)CaseStatusOption.Proposed);

            // Act.
            _caseService.ApproveCase(caseWithProposedStatus.Id);

            // Assert.
            _mockLogger.Verify(x => x.LogEvent(It.IsAny<string>()));
        }
        #endregion

        #region GetUnassignedCases
        [Theory]
        [InlineData("userWithNoPermission", true)]
        [InlineData("anotherUser", false)]
        public void GetUnassignedCases_ReturnsOnlyCasesCreatedByOneselfForStaffWithoutViewAllUnassignedCasesInDepartmentPermission(
            string createdBy,
            bool isCaseReturned)
        {
            // Arrange. 
            // Set test user.
            _mockUserContext.Object.Username = "userWithNoPermission";
            _mockUserContext.Object.EffectivePermissions = new List<Permission>();

            // Setup unassigned cases.
            Case case1 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Proposed,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                IsDeleted = false,
            };

            _arrangeCaseManagerDbContext.Cases.Add(case1);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetUnassignedCases();

            // Assert.
            Assert.Equal(cases.Any(), isCaseReturned);
        }

        [Fact]
        public void GetUnassignedCases_ReturnsAllCasesInDepartmentForStaffWithPermissionCalledViewAllUnassignedCasesInDepartment()
        {
            // Arrange. 
            // Set test user.
            _mockUserContext.Object.Username = "userWithPermission";
            _mockUserContext.Object.EffectivePermissions = new List<Permission>
            {
                _arrangeCaseManagerDbContext.Permissions
                    .First(permission => permission.Id == (int)PermissionOption.ViewAllUnassignedCasesInDepartment)
            };

            // Setup unassigned cases.
            Case case1 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Proposed,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            Case case2 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Approved,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            Case case3 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Proposed,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(case1);
            _arrangeCaseManagerDbContext.Cases.Add(case2);
            _arrangeCaseManagerDbContext.Cases.Add(case3);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetUnassignedCases();

            // Assert.
            Assert.Contains(cases, @case => @case.Id == case1.Id);
            Assert.Contains(cases, @case => @case.Id == case2.Id);
            Assert.Contains(cases, @case => @case.Id == case3.Id);
        }

        [Fact]
        public void GetUnassignedCases_DoesNotReturnCasesInAnotherDepartmentIfOnlyPermissionStaffHasIsViewAllUnassignedCasesInDepartment()
        {
            // Arrange. 
            // Set test user.
            _mockUserContext.Object.Username = "userWithPermission";
            _mockUserContext.Object.DepartmentId = (int)DepartmentOption.Audit1;
            _mockUserContext.Object.EffectivePermissions = new List<Permission>
            {
                _arrangeCaseManagerDbContext.Permissions
                    .First(permission => permission.Id == (int)PermissionOption.ViewAllUnassignedCasesInDepartment)
            };

            // Setup unassigned cases.
            Case case1 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Proposed,
                DepartmentId = (int)DepartmentOption.Audit2,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            Case case2 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Approved,
                DepartmentId = (int)DepartmentOption.Audit3,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            Case case3 = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Proposed,
                DepartmentId = (int)DepartmentOption.Legal,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(case1);
            _arrangeCaseManagerDbContext.Cases.Add(case2);
            _arrangeCaseManagerDbContext.Cases.Add(case3);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetUnassignedCases();

            // Assert.
            Assert.DoesNotContain(case1, cases);
            Assert.DoesNotContain(case2, cases);
            Assert.DoesNotContain(case3, cases);
        }

        [Theory]
        [InlineData(CaseStatusOption.Proposed, true)]
        [InlineData(CaseStatusOption.Approved, true)]
        [InlineData(CaseStatusOption.Assigned, false)]
        [InlineData(CaseStatusOption.Rejected, false)]
        public void GetUnassignedCases_ReturnsCasesInTheApprovedOrProposedStatusOnly(
            CaseStatusOption currentCaseStatus,
            bool doesReturnCase)
        {
            // Arrange.
            _mockUserContext.Object.Username = "userWithPermission";
            _mockUserContext.Object.DepartmentId = (int)DepartmentOption.Audit1;
            _mockUserContext.Object.EffectivePermissions = new List<Permission>
            {
                _arrangeCaseManagerDbContext.Permissions
                    .First(permission => permission.Id == (int)PermissionOption.ViewAllUnassignedCasesInDepartment)
            };

            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)currentCaseStatus,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetUnassignedCases();

            // Assert.
            Assert.Equal(cases.Any(), doesReturnCase);
        }
        #endregion

        #region RejectCase
        [Fact]
        public void RejectCase_SetsTheCaseStatusToRejected()
        {
            Case @case = RejectCaseArrange();

            // Act.
            _caseService.RejectCase(@case.Id);

            // Assert.
            Case rejectedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal((int)CaseStatusOption.Rejected, rejectedCase.StatusId);
        }

        [Fact]
        public void RejectCase_SendsAnEmailToStaffWhoCreatedTheCase()
        {
            Case @case = RejectCaseArrange();

            // Act.
            _caseService.RejectCase(@case.Id);

            // Assert.
            _mockNotificationService.Verify(x => 
                x.Notify(
                    It.IsAny<string>(), 
                    CollectionMatcher(new string[] { "userWithPermission" }),
                    It.Is<IEnumerable<string>>(list => list == null || !list.Any())));
        }

        [Fact]
        public void RejectCase_LogsAnEvent()
        {
            Case @case = RejectCaseArrange();

            // Act.
            _caseService.RejectCase(@case.Id);

            // Assert.
            _mockLogger.Verify(x => x.LogEvent(It.IsAny<string>()));
        }

        private Case RejectCaseArrange()
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Proposed,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            return @case;
        }
        #endregion

        #region AssignCase
        [Theory]
        [InlineData(CaseStatusOption.Proposed, true)]
        [InlineData(CaseStatusOption.Rejected, true)]
        [InlineData(CaseStatusOption.Assigned, true)]
        [InlineData(CaseStatusOption.Approved, false)]
        public void AssignCase_ThrowsErrorIfInUnexpectedStatus(
            CaseStatusOption status,
            bool isErrorThrown)
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "10011001",
                StatusId = (int)status,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedBy = "test",
                UpdatedBy = "test",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            Action act = () => _caseService.AssignCase(
                @case.Id,
                _mockUserContext.Object.Username,
                "manager");

            // Assert.
            if (isErrorThrown)
            {
                Assert.Throws<CaseNotInApprovedStatusException>(act);
            }
            else
            {
                // Should pass if error is not supposed to be thrown, which is when it is in approved status.
                Assert.Null(null);
            }
        }

        [Fact]
        public void AssignCase_AssignsGivenDirectorAndManager()
        {
            string managerUsername = "manager";
            Case @case = AssignCaseArrange(managerUsername);

            // Act.
            _caseService.AssignCase(
                @case.Id,
                _mockUserContext.Object.Username,
                managerUsername);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal(_mockUserContext.Object.Username, updatedCase.DirectorUsername);
            Assert.Equal(managerUsername, updatedCase.ManagerUsername);
        }

        [Fact]
        public void AssignCase_AutomaticallyAssignsTeamLeaderAndTeamAssistant()
        {
            string managerUsername = "manager";
            string teamLeaderUsername = "member1";
            string teamAssistantUsername = "member2";
            Case @case = AssignCaseArrange(managerUsername, teamLeaderUsername, teamAssistantUsername);

            // Act.
            _caseService.AssignCase(
                @case.Id,
                _mockUserContext.Object.Username,
                managerUsername);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal(teamLeaderUsername, updatedCase.TeamLeaderUsername);
            Assert.Equal(teamAssistantUsername, updatedCase.TeamAssistantUsername);
        }

        [Fact]
        public void AssignCase_SetsCaseStatusToAssigned()
        {
            string managerUsername = "manager";
            Case @case = AssignCaseArrange(managerUsername);

            // Act.
            _caseService.AssignCase(
                @case.Id,
                _mockUserContext.Object.Username,
                managerUsername);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal((int)CaseStatusOption.Assigned, updatedCase.StatusId);
        }

        private Case AssignCaseArrange(
            string managerUsername,
            string teamLeaderUsername = "member1",
            string teamAssistantUsername = "member2")
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Approved,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            Team team = new Team
            {
                DepartmentId = (int)DepartmentOption.Audit1,
                SupervisorUsername = managerUsername,
                TeamMembers = new List<TeamMember>
                {
                    new TeamMember
                    {
                        Username = teamLeaderUsername,
                        IsTeamLeader = true,

                    },
                    new TeamMember
                    {
                        Username = teamAssistantUsername,
                        IsTeamLeader = false,
                    },
                }
            };
            _arrangeCaseManagerDbContext.Teams.Add(team);
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            return @case;
        }
        #endregion

        #region GetOngoingCases
        [Theory]
        [InlineData(CaseStatusOption.Proposed, false)]
        [InlineData(CaseStatusOption.Approved, false)]
        [InlineData(CaseStatusOption.Assigned, true)]
        [InlineData(CaseStatusOption.Rejected, false)]
        [InlineData(CaseStatusOption.Planning, true)]
        [InlineData(CaseStatusOption.InProgress, true)]
        [InlineData(CaseStatusOption.OnHold, true)]
        [InlineData(CaseStatusOption.PendingReview, true)]
        [InlineData(CaseStatusOption.Disputed, true)]
        [InlineData(CaseStatusOption.Closed, false)]
        public void GetOngoingCases_ReturnsCasesInExpectedStatusesOnly(
            CaseStatusOption status, 
            bool isCaseReturned)
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)status,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetOngoingCases();

            // Assert.
            Assert.Equal(isCaseReturned, cases.Any());
        }

        [Theory]
        [InlineData(new PermissionOption[0], DepartmentOption.Audit1, DepartmentOption.Audit1)]
        [InlineData(new PermissionOption[0], DepartmentOption.Audit1, DepartmentOption.Audit2)]
        [InlineData(new PermissionOption[1] { PermissionOption.ViewAllOngoingCasesInDepartment }, DepartmentOption.Audit1, DepartmentOption.Audit1)]
        [InlineData(new PermissionOption[1] { PermissionOption.ViewAllOngoingCasesInDepartment }, DepartmentOption.Audit1, DepartmentOption.Audit2)]
        public void GetOngoingCases_ReturnsCasesRelevantToUserBasedOnDepartmentAndPermissions(
            PermissionOption[] permissions,
            DepartmentOption userDepartment,
            DepartmentOption caseDepartment)
        {
            // Arrange.
            var allPermissions = _arrangeCaseManagerDbContext.Permissions.ToList();
            _mockUserContext.Object.DepartmentId = (int)userDepartment;
            _mockUserContext.Object.EffectivePermissions = allPermissions.Where(permission => permissions.Contains((PermissionOption)permission.Id)).ToList();
            Case caseCreatedByUser = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)userDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
                ManagerUsername = "a",
                DirectorUsername = "a",
                TeamLeaderUsername = "a",
                TeamAssistantUsername = "a",
            };
            Case caseCreatedByAnotherUserInSameDepartment = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)userDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
                ManagerUsername = "a",
                DirectorUsername = "a",
                TeamLeaderUsername = "a",
                TeamAssistantUsername = "a",
            };
            Case caseWhereCurrentUserIsManager = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)userDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
                ManagerUsername = _mockUserContext.Object.Username,
                DirectorUsername = "a",
                TeamLeaderUsername = "a",
                TeamAssistantUsername = "a",
            };
            Case caseWhereCurrentUserIsDirector = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)userDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
                ManagerUsername = "a",
                DirectorUsername = _mockUserContext.Object.Username,
                TeamLeaderUsername = "a",
                TeamAssistantUsername = "a",
            };
            Case caseWhereCurrentUserIsTeamLeader = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)userDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
                ManagerUsername = "a",
                DirectorUsername = "a",
                TeamLeaderUsername = _mockUserContext.Object.Username,
                TeamAssistantUsername = "a",
            };
            Case caseWhereCurrentUserIsTeamAssistant = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)userDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
                ManagerUsername = "a",
                DirectorUsername = "a",
                TeamLeaderUsername = "a",
                TeamAssistantUsername = _mockUserContext.Object.Username,
            };
            Case caseInDepartment = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.Assigned,
                DepartmentId = (int)caseDepartment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "anotherUser",
                UpdatedBy = "anotherUser",
                IsDeleted = false,
                ManagerUsername = "a",
                DirectorUsername = "a",
                TeamLeaderUsername = "a",
                TeamAssistantUsername = "a",
            };
            _arrangeCaseManagerDbContext.Cases.AddRange(new Case[] {
                caseCreatedByUser,
                caseCreatedByAnotherUserInSameDepartment,
                caseWhereCurrentUserIsDirector,
                caseWhereCurrentUserIsManager,
                caseWhereCurrentUserIsTeamAssistant,
                caseWhereCurrentUserIsTeamLeader,
                caseInDepartment
            });
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetOngoingCases();

            // Assert.
            if (caseDepartment == userDepartment)
            {
                if (permissions.Contains(PermissionOption.ViewAllOngoingCasesInDepartment))
                {
                    Assert.Contains(cases, @case => @case.Id == caseInDepartment.Id);
                }
                else
                {
                    Assert.DoesNotContain(cases, @case => @case.Id == caseInDepartment.Id);
                }
            }
            else
            {
                Assert.DoesNotContain(cases, @case => @case.Id == caseInDepartment.Id);
            }

            Assert.Contains(cases, @case => @case.Id == caseWhereCurrentUserIsManager.Id);
            Assert.Contains(cases, @case => @case.Id == caseWhereCurrentUserIsDirector.Id);
            Assert.Contains(cases, @case => @case.Id == caseWhereCurrentUserIsTeamLeader.Id);
            Assert.Contains(cases, @case => @case.Id == caseWhereCurrentUserIsTeamAssistant.Id);
            Assert.Contains(cases, @case => @case.Id == caseCreatedByUser.Id);
            Assert.Equal(permissions.Contains(PermissionOption.ViewAllOngoingCasesInDepartment), cases.Any(@case => @case.Id == caseCreatedByAnotherUserInSameDepartment.Id));
        }
        #endregion

        #region ChangeCaseStatus
        [Theory]
        [InlineData(CaseStatusOption.Proposed, CaseStatusOption.Approved, false)]
        [InlineData(CaseStatusOption.Proposed, CaseStatusOption.Rejected, false)]
        [InlineData(CaseStatusOption.Proposed, CaseStatusOption.Assigned, true)]
        [InlineData(CaseStatusOption.Approved, CaseStatusOption.Assigned, false)]
        [InlineData(CaseStatusOption.Approved, CaseStatusOption.Planning, true)]
        [InlineData(CaseStatusOption.Assigned, CaseStatusOption.Planning, false)]
        [InlineData(CaseStatusOption.Assigned, CaseStatusOption.OnHold, false)]
        [InlineData(CaseStatusOption.Assigned, CaseStatusOption.Approved, true)]
        [InlineData(CaseStatusOption.Planning, CaseStatusOption.InProgress, false)]
        [InlineData(CaseStatusOption.Planning, CaseStatusOption.OnHold, false)]
        [InlineData(CaseStatusOption.Planning, CaseStatusOption.Assigned, true)]
        [InlineData(CaseStatusOption.InProgress, CaseStatusOption.PendingReview, false)]
        [InlineData(CaseStatusOption.InProgress, CaseStatusOption.Disputed, false)]
        [InlineData(CaseStatusOption.InProgress, CaseStatusOption.OnHold, false)]
        [InlineData(CaseStatusOption.InProgress, CaseStatusOption.Planning, true)]
        [InlineData(CaseStatusOption.PendingReview, CaseStatusOption.Disputed, false)]
        [InlineData(CaseStatusOption.PendingReview, CaseStatusOption.InProgress, true)]
        [InlineData(CaseStatusOption.Disputed, CaseStatusOption.InProgress, false)]
        [InlineData(CaseStatusOption.Disputed, CaseStatusOption.OnHold, true)]
        [InlineData(CaseStatusOption.OnHold, CaseStatusOption.InProgress, false)]
        [InlineData(CaseStatusOption.OnHold, CaseStatusOption.Planning, false)]
        [InlineData(CaseStatusOption.OnHold, CaseStatusOption.Assigned, false)]
        [InlineData(CaseStatusOption.OnHold, CaseStatusOption.Approved, true)]
        [InlineData(CaseStatusOption.Rejected, CaseStatusOption.Proposed, false)]
        [InlineData(CaseStatusOption.Rejected, CaseStatusOption.Approved, true)]
        [InlineData(CaseStatusOption.Closed, CaseStatusOption.Proposed, true)]

        public void ChangeCaseStatus_ThrowsExceptionIfChangeToInvalidStatus(
            CaseStatusOption currentStatus,
            CaseStatusOption nextStatus,
            bool isExceptionThrown)
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)currentStatus,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            Action act = () => _caseService.ChangeCaseStatus(@case.Id, nextStatus);

            // Assert.
            Exception exception = Record.Exception(act);
            Assert.Equal(isExceptionThrown, exception != null);
        }

        [Fact]
        public void ChangeCaseStatus_SetsTheStatusOfTheCaseToTheGivenStatus()
        {
            CaseStatusOption currentStatus = CaseStatusOption.Planning;
            CaseStatusOption nextStatus = CaseStatusOption.InProgress;
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)currentStatus,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = "userWithPermission",
                UpdatedBy = "userWithPermission",
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            _caseService.ChangeCaseStatus(@case.Id, nextStatus);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal(nextStatus, (CaseStatusOption)updatedCase.StatusId);
        }
        #endregion

        #region GetClosedCases
        [Theory]
        [InlineData(CaseStatusOption.Proposed, false)]
        [InlineData(CaseStatusOption.Approved, false)]
        [InlineData(CaseStatusOption.Assigned, false)]
        [InlineData(CaseStatusOption.Rejected, false)]
        [InlineData(CaseStatusOption.Planning, false)]
        [InlineData(CaseStatusOption.InProgress, false)]
        [InlineData(CaseStatusOption.OnHold, false)]
        [InlineData(CaseStatusOption.PendingReview, false)]
        [InlineData(CaseStatusOption.Disputed, false)]
        [InlineData(CaseStatusOption.Closed, true)]
        public void GetClosedCases_ReturnsCasesInClosedStatusOnly(
            CaseStatusOption status,
            bool isCaseReturned)
        {
            // Arrange.
            Case @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)status,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetClosedCases();

            // Assert.
            Assert.Equal(isCaseReturned, cases.Any());
        }
        #endregion

        public static IEnumerable<T> CollectionMatcher<T>(IEnumerable<T> expectation)
        {
            return Match.Create((IEnumerable<T> inputCollection) =>
                                !expectation.Except(inputCollection).Any() &&
                                !inputCollection.Except(expectation).Any());
        }
    }
}
