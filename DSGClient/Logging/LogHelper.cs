using Serilog;
using System;
using System.Xml;

namespace DSGClient
{
    /// <summary>
    /// Thin wrapper around Serilog to keep existing call sites simple.
    /// </summary>
    public static class LogHelper
    {
        public static void Info(string message) =>
            Log.Information(message); //Log.Information("{Message}", message);

        public static void Warn(string message) =>
            Log.Warning(message); //Log.Warning("{Message}", message);

        public static void Error(string message) =>
            Log.Error(message);

        public static void Xml(string label, XmlDocument xml)
        {
            Log.Information("{Label}{NewLine}{Xml}", label, xml.OuterXml);
        }
        public static void Error(Exception ex, string message) =>
            Log.Error(ex, message);
    }
}
