using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using EpisodeRenamer.Core;

namespace EpisodeRenamer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ErrorLogger.LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EpisodeRenamer", "error.log");
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLogger.Write(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;
        if (Environment.GetEnvironmentVariable("EPISODE_RENAMER_HEADLESS") == "1") return;
        try
        {
            MessageBox.Show(
                "حدث خطأ غير متوقع.\n\n" + e.Exception.Message + "\n\nتم حفظ التفاصيل في:\n" + ErrorLogger.LogPath,
                "خطأ غير متوقع",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
        }
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            ErrorLogger.Write(exception, "AppDomain.UnhandledException");
    }
}