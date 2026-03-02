using Autofac;
using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using CaseManager.BusinessLogic.Domain.Services;
using CaseManager.BusinessLogic.Interfaces.Notification;
using CaseManager.Infrastructure.Notification;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaseManager.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341/")
                .CreateLogger();

            var builder = new ContainerBuilder();
            RegisterServices(builder);
            RegisterForms(builder);
            var container = builder.Build();

            var userContext = container.Resolve<UserContext>();
            LogContext.PushProperty("UserContext", userContext, true);

            FormFactory.SetContainer(container);

            LoginForm loginForm = FormFactory.Create<LoginForm>();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {

                Application.Run(FormFactory.Create<MainForm>());
            }
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.RegisterType<UserContext>().AsSelf().SingleInstance();
            builder.RegisterType<AuthorizationService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<CaseService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<HRService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<TeamService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<NextStatusService>().AsSelf().InstancePerLifetimeScope();
            builder.RegisterType<DbContextFactory<HRDbContext>>().As<IDbContextFactory<HRDbContext>>().InstancePerLifetimeScope();
            builder.RegisterType<DbContextFactory<CaseManagerDbContext>>().As<IDbContextFactory<CaseManagerDbContext>>().InstancePerLifetimeScope();
            builder.RegisterType<EFNotificationService>().As<INotificationService>().InstancePerLifetimeScope();
        }

        private static void RegisterForms(ContainerBuilder builder)
        {
            builder.RegisterType<LoginForm>();
            builder.RegisterType<MainForm>();
            builder.RegisterType<AnotherForm>();
            builder.RegisterType<AssignCaseForm>();
        }
    }
}
