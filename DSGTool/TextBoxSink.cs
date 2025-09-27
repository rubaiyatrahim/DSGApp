using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Windows.Forms;

namespace DSGTool.Logging
{
    public class TextBoxSink : ILogEventSink
    {
        private readonly TextBox _textBox;
        private readonly ITextFormatter _formatter;

        public TextBoxSink(TextBox textBox, ITextFormatter formatter)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            string message;
            using (var sw = new System.IO.StringWriter())
            {
                _formatter.Format(logEvent, sw);
                message = sw.ToString();
            }

            try
            {
                if (_textBox.IsHandleCreated && !_textBox.IsDisposed)
                {
                    _textBox.BeginInvoke((MethodInvoker)(() =>
                    {
                        if (!_textBox.IsDisposed)
                            _textBox.AppendText(message);
                    }));
                }
            }
            catch (InvalidOperationException)
            {
                // Safe ignore if disposing
            }
        }
    }
}
