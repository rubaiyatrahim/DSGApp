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

        /**
         * Constructor of the text writer.
         * 
         * @param writeAction: Action to write to the text box.
         * */
        public TextBoxWriter(Action<string> writeAction)
        {
            _writeAction = writeAction;
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
            _writeAction(value);            
        }

        /**
         * Write a string to the text box.
         * 
         * @param value: String to write.
         * */
        public override void Write(string value)
        {
            _writeAction(value);
        }
    }
}
