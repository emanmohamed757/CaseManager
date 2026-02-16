using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using CaseManager.BusinessLogic.Domain.Enums;
using CaseManager.BusinessLogic.Domain.Exceptions;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.BusinessLogic.Interfaces.Logging;
using CaseManager.BusinessLogic.Interfaces.Notification;
using CaseManager.Tests.Helpers;
using Effort.DataLoaders;
using Moq;
using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Net.NetworkInformation;
using Xunit;

namespace CaseManager.Tests
{
    public class ConflictOfInterestServiceMethod
    {
        private readonly ConflictOfInterestService _conflictOfInterestService;

        private readonly CaseManagerDbContext _arrangeCaseManagerDbContext;

        private readonly CaseManagerDbContext _assertCaseManagerDbContext;

        private readonly HRDbContext _arrangeHRDbContext;

        private readonly HRDbContext _assertHRDbContext;

        private readonly Mock<ILogger> _mockLogger;

        private readonly Mock<INotificationService> _mockNotificationService;

        private readonly Mock<UserContext> _mockUserContext;

        private readonly TeamFactory _teamFactory;

        public ConflictOfInterestServiceMethod()
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

            var dataLoader2 = new EntityDataLoader("name=HRDbContext");
            DbConnection connection2 = Effort.EntityConnectionFactory.CreateTransient("name=HRDbContext", dataLoader2);

            _arrangeHRDbContext = new HRDbContext(connection2);
            _assertHRDbContext = new HRDbContext(connection2);

            var mockHRDbContextFactory = new Mock<IDbContextFactory<HRDbContext>>();
            mockHRDbContextFactory.Setup(x => x.Create()).Returns(() =>
            {
                return new HRDbContext(connection2);
            });

            // Sytem under test.
            _conflictOfInterestService = new ConflictOfInterestService(
                mockCaseManagerDbContextFactory.Object,
                mockHRDbContextFactory.Object,
                _mockLogger.Object,
                _mockUserContext.Object,
                _mockNotificationService.Object);

            _teamFactory = new TeamFactory(_arrangeCaseManagerDbContext);

            _arrangeHRDbContext.Employees.Add(
                new Employee
                {
                    Username = _mockUserContext.Object.Username,
                    DepartmentId = 1,
                    DesignationId = (int)DesignationOption.OperationalStaff,
                    Name = "REEEE"
                });
            _arrangeHRDbContext.SaveChanges();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void DeclareConflictOfInterest_DoesNotAllowDeclaringOnAlreadyDeclaredCase(
            bool declaredByCurrentUser,
            bool isExceptionThrown)
        {
            // Arrange.
            var @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
            };
            var conflict = new ConflictOfInterest
            {
                Case = @case,
                DeclaredDate = DateTime.Now,
                StaffDesignationId = (int)ConflictOfInterestStaffDesignationLevel.OperationalStaff,
                Username = _mockUserContext.Object.Username,
            };

            if (!declaredByCurrentUser)
            {
                conflict.Username = "another user";
            }

            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.ConflictOfInterests.Add(conflict);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            Action act = () => _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            var exception = Record.Exception(act) as ConfictOfInterestDeclarationException;

            Assert.True((exception != null) == isExceptionThrown);
        }

        [Fact]
        public void DeclareConflictOfInterest_WhenOperationalStaffDeclaresAndThereAreMultipleAvailableTeamMembers_NotifiesManager()
        {
            // Arrange.
            var team = new Team
            {
                DepartmentId = 1,
                SupervisorUsername = "manager",
            };
            var memberOne = new TeamMember
            {
                Team = team,
                Username = _mockUserContext.Object.Username
            };
            var memberTwo = new TeamMember
            {
                Team = team,
                Username = "memberTwo"
            };
            var memberThree = new TeamMember
            {
                Team = team,
                Username = "availableMember"
            };
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberOne);
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberTwo);
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberThree);
            _arrangeCaseManagerDbContext.Teams.Add(team);
            _arrangeCaseManagerDbContext.SaveChanges();

            var @case = new Case
            {
                CaseNumber = "10011001",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedBy = "test",
                UpdatedBy = "test",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false,
                ManagerUsername = "manager"
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            _mockNotificationService.Verify(x =>
                x.Notify(
                    It.IsAny<string>(),
                    MockHelpers.CollectionMatcher(new string[] { "manager" }),
                    MockHelpers.CollectionMatcher(new string[] { _mockUserContext.Object.Username })),
                Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DeclareConflictOfInterest_WhenOperationalStaffDeclaresAndThereIsOnlyOneAvailableTeamMember_AutoAssignsThatMemberToCase(
            bool isCurrentUserTeamLeader)
        {
            // Arrange.
            var team = new Team
            {
                DepartmentId = 1,
                SupervisorUsername = "manager",
            };
            var memberOne = new TeamMember
            {
                Team = team,
                Username = _mockUserContext.Object.Username
            };
            var memberTwo = new TeamMember
            {
                Team = team,
                Username = "memberTwo"
            };
            var availableMember = new TeamMember
            {
                Team = team,
                Username = "availableMember"
            };

            var @case = new Case
            {
                CaseNumber = "10011001",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedBy = "test",
                UpdatedBy = "test",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false,
                ManagerUsername = "manager",
                TeamLeaderUsername = isCurrentUserTeamLeader ? _mockUserContext.Object.Username : memberTwo.Username,
                TeamAssistantUsername = !isCurrentUserTeamLeader ? _mockUserContext.Object.Username : memberTwo.Username
            };

            var conflict = new ConflictOfInterest
            {
                Case = @case,
                DeclaredDate = DateTime.Now,
                StaffDesignationId = (int)DesignationOption.OperationalStaff,
                Username = memberTwo.Username
            };

            _arrangeCaseManagerDbContext.ConflictOfInterests.Add(conflict);
            _arrangeCaseManagerDbContext.Cases.Add(@case);
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberOne);
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberTwo);
            _arrangeCaseManagerDbContext.TeamMembers.Add(availableMember);
            _arrangeCaseManagerDbContext.Teams.Add(team);

            _arrangeCaseManagerDbContext.SaveChanges();

            // Act.
            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal(availableMember.Username, isCurrentUserTeamLeader ? updatedCase.TeamLeaderUsername : updatedCase.TeamAssistantUsername);
        }

        [Fact]
        public void DeclareConflictOfInterest_SetsCaseAsAwaitingReassignment()
        {
            // Arrange.
            var @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
                ManagerUsername = "manager"
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);

            var team = new Team
            {
                DepartmentId = 1,
                SupervisorUsername = "manager",
            };
            var memberOne = new TeamMember
            {
                Team = team,
                Username = _mockUserContext.Object.Username
            };
            var memberTwo = new TeamMember
            {
                Team = team,
                Username = "memberTwo"
            };
            var memberThree = new TeamMember
            {
                Team = team,
                Username = "availableMember"
            };
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberOne);
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberTwo);
            _arrangeCaseManagerDbContext.TeamMembers.Add(memberThree);
            _arrangeCaseManagerDbContext.Teams.Add(team);
            _arrangeCaseManagerDbContext.SaveChanges();

            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.True(updatedCase.IsAwaitingReassignment);
        }

        [Fact]
        public void DeclareConflictOfInterest_WhenManagerHasNoStaffToAssignTo_EscalatesAssignmentToDirector()
        {
            // Arrange.
            Case @case = ArrangeMethodFor_DeclareConflictOfInterest_WhenManagerHasNoStaffToAssignTo();

            // Act.
            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            _mockNotificationService.Verify(x =>
                x.Notify(
                    It.IsAny<string>(),
                    MockHelpers.CollectionMatcher(new string[] { "Mr.Director" }),
                    MockHelpers.CollectionMatcher(new string[] { "manager", _mockUserContext.Object.Username })),
                Times.Once);
        }

        [Fact]
        public void DeclareConflictOfInterest_WhenManagerHasNoStaffToAssignTo_FreezesTheCaseForReassignment()
        {
            Case @case = ArrangeMethodFor_DeclareConflictOfInterest_WhenManagerHasNoStaffToAssignTo();

            // Act.
            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.True(updatedCase.IsAwaitingReassignment);
        }

        private Case ArrangeMethodFor_DeclareConflictOfInterest_WhenManagerHasNoStaffToAssignTo()
        {
            var @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
                ManagerUsername = "manager",
                DirectorUsername = "Mr.Director"
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);

            _teamFactory.CreateTeam(
                @case,
                new[]
                {
                    // Director.
                    new TeamMemberSummary
                    {
                        DepartmentId = (int)DepartmentOption.Audit1,
                        DesignationId = (int)DesignationOption.Director,
                        Username = "Mr.Director",
                        TeamMembers = new[]
                        {
                            // Manager.
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.Manager,
                                Username = "manager",
                                TeamMembers = new[]
                                {
                                    // Operational Staff.
                                    new TeamMemberSummary
                                    {
                                        DepartmentId = (int)DepartmentOption.Audit1,
                                        DesignationId = (int)DesignationOption.OperationalStaff,
                                        Username = "memberOne",
                                        HasConflictOfInterest = true,
                                    },
                                    new TeamMemberSummary
                                    {
                                        DepartmentId = (int)DepartmentOption.Audit1,
                                        DesignationId = (int)DesignationOption.OperationalStaff,
                                        Username = "memberTwo",
                                        HasConflictOfInterest = true,
                                    },
                                    new TeamMemberSummary
                                    {
                                        DepartmentId = (int)DepartmentOption.Audit1,
                                        DesignationId = (int)DesignationOption.OperationalStaff,
                                        Username = _mockUserContext.Object.Username,
                                    },
                                }
                            }
                        }
                    }
                });
            return @case;
        }

        [Fact]
        public void DeclareConflictOfInterest_ShouldNotAutoReassignStaffWhoIsAlreadyAssignedToTheCase()
        {
            var @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
                ManagerUsername = "manager",
                DirectorUsername = "Mr.Director",
                TeamLeaderUsername = _mockUserContext.Object.Username,
                TeamAssistantUsername = "memberTwo"
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);

            _teamFactory.CreateTeam(
                @case,
                new[]
                {
                    // Manager.
                    new TeamMemberSummary
                    {
                        DepartmentId = (int)DepartmentOption.Audit1,
                        DesignationId = (int)DesignationOption.Manager,
                        Username = "manager",
                        TeamMembers = new[]
                        {
                            // Operational Staff.
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = "memberTwo",
                            },
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = _mockUserContext.Object.Username,
                            },
                        }
                    }
                });

            // Act. 
            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            Case updatedCase = _assertCaseManagerDbContext.Cases.Find(@case.Id);
            Assert.NotEqual("memberTwo", updatedCase.TeamLeaderUsername);
        }

        //[Fact]
        public void DeclareConflictOfInterest_WhenCaseHasStaffSupervisedByDifferentManagarsAndOneDeclares_NotifiesTheManagerOfTheOtherStaffMemberIfTheyHaveMoreThanOneAvailableStaff()
        {
            var @case = new Case
            {
                CaseNumber = "dawdaw",
                StatusId = (int)CaseStatusOption.InProgress,
                DepartmentId = (int)DepartmentOption.Audit1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = _mockUserContext.Object.Username,
                UpdatedBy = _mockUserContext.Object.Username,
                IsDeleted = false,
                ManagerUsername = "manager 1",
                DirectorUsername = "Mr.Director",
                TeamLeaderUsername = _mockUserContext.Object.Username,
                TeamAssistantUsername = "memberThree"
            };
            _arrangeCaseManagerDbContext.Cases.Add(@case);

            _teamFactory.CreateTeam(
                @case,
                new[]
                {
                    // Manager.
                    new TeamMemberSummary
                    {
                        DepartmentId = (int)DepartmentOption.Audit1,
                        DesignationId = (int)DesignationOption.Manager,
                        Username = "manager 1",
                        TeamMembers = new[]
                        {
                            // Operational Staff.
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = "memberTwo",
                            },
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = _mockUserContext.Object.Username,
                            },
                        }
                    },
                    // Manager.
                    new TeamMemberSummary
                    {
                        DepartmentId = (int)DepartmentOption.Audit1,
                        DesignationId = (int)DesignationOption.Manager,
                        Username = "manager 2",
                        TeamMembers = new[]
                        {
                            // Operational Staff.
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = "memberThree",
                            },
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = "memberFour",
                            },
                            new TeamMemberSummary
                            {
                                DepartmentId = (int)DepartmentOption.Audit1,
                                DesignationId = (int)DesignationOption.OperationalStaff,
                                Username = "memberFive",
                            },
                        }
                    }
                });

            // Act.
            _conflictOfInterestService.DeclareConflictOfInterest(@case.Id);

            // Assert.
            _mockNotificationService.Verify(x =>
                x.Notify(
                    It.IsAny<string>(),
                    MockHelpers.CollectionMatcher(new string[] { "manager 2" }),
                    MockHelpers.CollectionMatcher(new string[] { "manager 1", _mockUserContext.Object.Username })),
                Times.Once);
        }
    }
}
