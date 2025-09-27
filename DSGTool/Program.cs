using DSGTool.Logging;
using Serilog;
using Serilog.Formatting.Display;
using System;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DSGTool
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new MainForm(); // instantiate first to access txtLog

            var formatter = new MessageTemplateTextFormatter(
    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
    null
);

            // Initialize logging for console and file only here
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.Sink(new UltraBufferedRichTextBoxSink(mainForm.TextLog, formatter, flushIntervalMs: 300, maxBatchSize: 100))
            .CreateLogger();

            Log.Information("txtLog sink wired successfully");
            Log.Information("Application started");

            // Run MainForm (MainForm will configure the txtLog sink internally)
            Application.Run(mainForm);

            Log.CloseAndFlush();
        }
    }
}
