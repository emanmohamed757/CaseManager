using Autofac;
using System;

namespace CaseManager.WinForms
{
    internal static class FormFactory
    {
        static IContainer _container;
        
        public static void SetContainer(IContainer container)
        {
            _container = container;
        }

        public static LoginForm CreateLoginForm()
        {
            return _container.Resolve<LoginForm>();
        }

        public static MainForm CreateMainForm()
        {
            return _container.Resolve<MainForm>();
        }
    }
}
