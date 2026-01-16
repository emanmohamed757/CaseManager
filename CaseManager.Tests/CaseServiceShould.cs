using Xunit;
using CaseManager.BusinessLogic.Services;
using CaseManager.BusinessLogic.Data;
using System.Data.Common;
using System;
using System.Linq;
using Moq;
using CaseManager.BusinessLogic.Interfaces;
using CaseManager.BusinessLogic.Enums;
using Effort.DataLoaders;
using System.Collections.Generic;
using CaseManager.BusinessLogic.Authorization;

namespace CaseManager.Tests
{
    public class CaseServiceShould : IDisposable
    {
        private readonly CaseService _caseService;

        private readonly CaseManagerDbContext _caseManagerDbContext;

        private readonly Mock<ILogger> _mockLogger;

        private readonly Mock<UserContext> _mockUserContext;

        public CaseServiceShould()
        {
            _mockLogger = new Mock<ILogger>();
            _mockUserContext = new Mock<UserContext>();
            _mockUserContext.SetupAllProperties();

            // dataLoader for loading static data.
            var dataLoader = new EntityDataLoader("name=CaseManagerDbContext");
            DbConnection connection = Effort.EntityConnectionFactory.CreateTransient("name=CaseManagerDbContext", dataLoader);
            _caseManagerDbContext = new CaseManagerDbContext(connection);

            _caseService = new CaseService(_caseManagerDbContext, _mockLogger.Object, _mockUserContext.Object);

            // Add data for testing.
            _caseManagerDbContext.Cases.Add(new Case
            {
                CaseNumber = "2026/001 (Proposed Case)",
                StatusId = 1,
                CreatedAt = DateTime.Now,
                CreatedBy = "eaman",
                UpdatedAt = DateTime.Now,
                UpdatedBy = "eaman",
                IsDeleted = false
            });
            _caseManagerDbContext.SaveChanges();
        }

        public void Dispose()
        {
            _caseManagerDbContext.Dispose();
        }

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

        [Fact]
        public void ApproveCase_Should_AssignTheApprovedStatusToCase()
        {
            // Arrange.
            // TODO: Is this an integration or unit test?
            Case caseWithProposedStatus = _caseManagerDbContext.Cases
                .FirstOrDefault(c => c.StatusId == (int)CaseStatusOption.Proposed);

            // Act.
            _caseService.ApproveCase(caseWithProposedStatus.Id);

            // Assert.
            Case @case = _caseManagerDbContext.Cases.Find(caseWithProposedStatus.Id);
            Assert.True(@case.StatusId == (int)CaseStatusOption.Approved);
        }

        [Fact]
        public void ApproveCase_LogsAnEvent()
        {
            // Arrange.
            Case caseWithProposedStatus = _caseManagerDbContext.Cases
                .FirstOrDefault(c => c.StatusId == (int)CaseStatusOption.Proposed);

            // Act.
            _caseService.ApproveCase(caseWithProposedStatus.Id);

            // Assert.
            _mockLogger.Verify(x => x.LogEvent(It.IsAny<string>()));
        }

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
            
            _caseManagerDbContext.Cases.Add(case1);
            _caseManagerDbContext.SaveChanges();

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
                _caseManagerDbContext.Permissions
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
            _caseManagerDbContext.Cases.Add(case1);
            _caseManagerDbContext.Cases.Add(case2);
            _caseManagerDbContext.Cases.Add(case3);
            _caseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetUnassignedCases();

            // Assert.
            Assert.Contains(case1, cases);
            Assert.Contains(case2, cases);
            Assert.Contains(case3, cases);
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
                _caseManagerDbContext.Permissions
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
            _caseManagerDbContext.Cases.Add(case1);
            _caseManagerDbContext.Cases.Add(case2);
            _caseManagerDbContext.Cases.Add(case3);
            _caseManagerDbContext.SaveChanges();

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
                _caseManagerDbContext.Permissions
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
            _caseManagerDbContext.Cases.Add(@case);
            _caseManagerDbContext.SaveChanges();

            // Act.
            List<Case> cases = _caseService.GetUnassignedCases();

            // Assert.
            Assert.Equal(cases.Any(), doesReturnCase);
        }

        [Fact]
        public void RejectCase_SetsTheCaseStatusToRejected()
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
            _caseManagerDbContext.Cases.Add(@case);
            _caseManagerDbContext.SaveChanges();

            // Act.
            _caseService.RejectCase(@case.Id);

            // Assert.
            Case rejectedCase = _caseManagerDbContext.Cases.Find(@case.Id);
            Assert.Equal((int)CaseStatusOption.Rejected, rejectedCase.StatusId);
        }

        [Fact]
        public void RejectCase_SendsAnEmailToStaffWhoCreatedTheCase()
        {

        }
    }
}
