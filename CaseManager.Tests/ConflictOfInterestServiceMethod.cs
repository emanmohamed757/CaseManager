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
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using Xunit;

namespace CaseManager.Tests
{
    public class ConflictOfInterestServiceMethod
    {
        private readonly ConflictOfInterestService _conflictOfInterestService;

        private readonly CaseManagerDbContext _arrangeCaseManagerDbContext;

        private readonly CaseManagerDbContext _assertCaseManagerDbContext;

        private readonly Mock<ILogger> _mockLogger;

        private readonly Mock<INotificationService> _mockNotificationService;

        private readonly Mock<UserContext> _mockUserContext;

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

            _conflictOfInterestService = new ConflictOfInterestService(
                mockCaseManagerDbContextFactory.Object,
                _mockLogger.Object,
                _mockUserContext.Object,
                _mockNotificationService.Object);
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
                StaffDesignationLevel = (int)ConflictOfInterestStaffDesignationLevel.OperationalStaff,
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
    }
}
