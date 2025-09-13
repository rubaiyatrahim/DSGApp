using System;
using System.IO;
using System.Text;

namespace DSGTool
{
    /**
     * A text writer that writes to a text box.
     * */
    public class TextBoxWriter : TextWriter
    {
        // Action to write to the text box
        private readonly Action<string> _writeAction;
        private readonly Action<string> _logAction;

        private readonly System.Text.StringBuilder _buffer = new();

        /**
         * Constructor of the text writer.
         * 
         * @param writeAction: Action to write to the text box.
         * */
        public TextBoxWriter(Action<string> logAction)
        {
            _logAction = logAction;
        }

        // UTF-8 encoding
        public override Encoding Encoding => Encoding.UTF8;

        /**
         * Write a line to the text box.
         * 
         * @param value: Line to write.
         * */
        public override void WriteLine(string value)
        {
            if (!string.IsNullOrEmpty(value))
                _logAction(value);
        }

        /**
         * Write a string to the text box.
         * 
         * @param value: String to write.
         * */
        public override void Write(char value)
        {
            if (value == '\n') // char vs char ✅
            {
                if (_buffer.Length > 0)
                {
                    _logAction(_buffer.ToString());
                    _buffer.Clear();
                }
            }
            else
            {
                _buffer.Append(value);
            }
        }

    }
}
