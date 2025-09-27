using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace DSGTool.Logging
{
    public class BufferedTextBoxSink : ILogEventSink, IDisposable
    {
        private readonly TextBox _textBox;
        private readonly ITextFormatter _formatter;
        private readonly ConcurrentQueue<string> _queue = new();
        private readonly Timer _timer;

        public BufferedTextBoxSink(TextBox textBox, ITextFormatter formatter, int flushIntervalMs = 100)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            // Timer flushes queued logs periodically
            _timer = new Timer(FlushLogs, null, flushIntervalMs, flushIntervalMs);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            // Render log line with formatter (timestamp, level, message, exception)
            string message;
            using (var sw = new System.IO.StringWriter())
            {
                _formatter.Format(logEvent, sw);
                message = sw.ToString();
            }

            _queue.Enqueue(message);
        }

        private void FlushLogs(object? state)
        {
            if (_queue.IsEmpty) return;

            if (_textBox.IsHandleCreated && !_textBox.IsDisposed)
            {
                _textBox.BeginInvoke((MethodInvoker)(() =>
                {
                    while (_queue.TryDequeue(out var message))
                    {
                        if (!_textBox.IsDisposed)
                            _textBox.AppendText(message);
                    }
                }));
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
            FlushLogs(null);
        }
    }
}
