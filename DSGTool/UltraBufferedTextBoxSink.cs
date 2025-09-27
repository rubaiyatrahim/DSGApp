using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace DSGTool
{
    public class UltraBufferedRichTextBoxSink : ILogEventSink, IDisposable
    {
        private readonly RichTextBox _richTextBox;
        private readonly ITextFormatter _formatter;
        private readonly ConcurrentQueue<(LogEvent logEvent, string formatted)> _queue = new();
        private readonly Timer _timer;
        private readonly int _maxLines;
        private readonly int _maxBatchSize;

        public UltraBufferedRichTextBoxSink(RichTextBox richTextBox, ITextFormatter formatter,
            int flushIntervalMs = 500, int maxLines = 10000, int maxBatchSize = 50)
        {
            _richTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _maxLines = maxLines;
            _maxBatchSize = maxBatchSize;

            _timer = new Timer(FlushLogs, null, flushIntervalMs, flushIntervalMs);
        }

        public void Emit(LogEvent logEvent)
        {
            using var sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            _queue.Enqueue((logEvent, sw.ToString()));
        }

        private void FlushLogs(object? state)
        {
            if (_queue.IsEmpty || !_richTextBox.IsHandleCreated || _richTextBox.IsDisposed) return;

            _richTextBox.BeginInvoke((MethodInvoker)(() =>
            {
                if (_richTextBox.IsDisposed) return;

                int count = 0;
                while (_queue.TryDequeue(out var entry) && count < _maxBatchSize)
                {
                    AppendSingle(entry.formatted, entry.logEvent.Level);
                    count++;
                }

                if (_richTextBox.Lines.Length > _maxLines)
                {
                    var lines = _richTextBox.Lines;
                    int removeCount = lines.Length - _maxLines;
                    string[] newLines = new string[_maxLines];
                    Array.Copy(lines, removeCount, newLines, 0, _maxLines);
                    _richTextBox.Lines = newLines;
                }
            }));
        }

        private void AppendSingle(string text, LogEventLevel level)
        {
            Color color = level switch
            {
                LogEventLevel.Information => Color.Black,
                LogEventLevel.Warning => Color.Orange,
                LogEventLevel.Error => Color.Red,
                LogEventLevel.Fatal => Color.DarkRed,
                LogEventLevel.Debug => Color.Gray,
                LogEventLevel.Verbose => Color.DarkGray,
                _ => Color.Black
            };

            int start = _richTextBox.TextLength;
            _richTextBox.SelectionStart = start;
            _richTextBox.SelectionLength = 0;
            _richTextBox.SelectionColor = color;

            _richTextBox.AppendText(text);
            _richTextBox.SelectionStart = _richTextBox.TextLength;
            _richTextBox.ScrollToCaret();
        }

        public void Dispose()
        {
            _timer.Dispose();
            FlushLogs(null);
        }
    }
}
