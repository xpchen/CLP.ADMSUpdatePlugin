using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace CLP.ADMSUpdatePlugin
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("CLP.ADMSUpdatePlugin_Module");

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            
            return true;
        }

        protected override void Uninitialize()
        {
            ProjectOpenedEvent.Unsubscribe(OnProjectOpenedEvent);
            Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
            base.Uninitialize();
        }

        protected override bool Initialize()
        {
          
            ProjectOpenedEvent.Subscribe(OnProjectOpenedEvent);
            // UI 线程未处理异常（可拦截并阻止进程崩溃）
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Task 未观察到的异常（避免进程被终结；.NET4+默认不崩，但建议观察）
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // AppDomain 级别（有些致命异常无法继续运行，但至少记录并提示）
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            if (!String.IsNullOrEmpty(Project.Current?.HomeFolderPath))
            {
                string logPath = System.IO.Path.Combine(Project.Current.HomeFolderPath, "logs", $"ADMSUpdate_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.log");
                LoggerHelper.Configure<ADMSUpdateDockpaneViewModel>(logPath, "ADMSUpdate");
            }
            return base.Initialize();
        }

        private void OnProjectOpenedEvent(ProjectEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.Project?.HomeFolderPath))
            {
                string logPath = System.IO.Path.Combine(args.Project.HomeFolderPath, "logs", $"ADMSUpdate_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.log");
                LoggerHelper.Configure<ADMSUpdateDockpaneViewModel>(logPath, "ADMSUpdate");
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception, "UI thread");
            MessageBox.Show($"发生未处理错误（UI）：{e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

            // 控制是否让应用继续运行：一般非致命错误可以吞掉
            e.Handled = true;
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "TaskScheduler");
            e.SetObserved(); // 标记为已处理，避免默认升级为异常终止
        }

        private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogException(ex, "AppDomain");
            MessageBox.Show($"发生致命错误：{ex?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            // 注意：某些类型（StackOverflow/OutOfMemory）即使提示后也不一定能安全继续
        }

        private void LogException(Exception? ex, string channel)
        {
            try
            {
                // 这里换成你的 Logger（例如 LoggerHelper）
                LoggerHelper.Error(ex, $"Unhandled ({channel})");
            }
            catch { /* 避免日志再抛异常 */ }
        }

        #endregion Overrides

    }
}
