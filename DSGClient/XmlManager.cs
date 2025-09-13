using System.Text;
using System.Xml;

namespace DSGClient
{
    /**
     * XML message builder class for DSG.
     */
    internal static class XmlManager
    {
        // Sequence number to track outgoing messages
        private static int _sequence = 1;

        // Xml message to send heartbeat to DSG
        private const string MESSAGE_HEARTBEAT = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><dsgmsg name=\"heartbeat\"></dsgmsg>";

        /**
         * Get bytes from memory stream containing the XML Download message bytes to send to DSG.
         * 
         * @param partitionId: The partition ID to download.
         * @param startingSequenceNumber: The starting sequence number to download.
         * @param endingSequenceNumber: The ending sequence number to download.
         * @param messageTypes: List of message types to download.
         * 
         * @return Array of bytes representing the XML Download message to send to DSG.
         * */
        public static byte[] BuildMessageDownload(string partitionId, string startingSequenceNumber, string endingSequenceNumber, List<MessageType> messageTypes)
        {
            // Xml message fields of Download message
            List<XmlField> fields = new() {
                new() { Name = "partid", Value = partitionId },
                new() { Name = "startseqno", Value = startingSequenceNumber },
                new() { Name = "endseqno", Value = endingSequenceNumber },
            };

            // Add message types to fields list
            messageTypes?.ToList().ForEach(x => fields.Add(new() { Name = "messageType", Value = x.MessageId }));

            return GetBytes(GetXml("dsgmsg", "download", fields));
        }

        /**
         * Get bytes from memory stream containing the XML Heartbeat message bytes to send to DSG.
         * 
         * @return Array of bytes representing the framed XML Heartbeat message to send to DSG.
         * */
        public static byte[] BuildMessageHeartbeat() => GetMessageBytesFramed(MESSAGE_HEARTBEAT, _sequence++, 0);

        /**
         * Get bytes from memory stream containing the XML Login message bytes to send to DSG.
         * 
         * @param userName: user name to log into the DSG.
         * @param password: password to log into the DSG of the user.
         * @param messageTypes: List of message types to subscribe to.
         * 
         * @return Array of bytes representing the XML Login message to send to DSG.
         * */
        public static byte[] BuildMessageLogin(string userName, string password, IEnumerable<MessageType> messageTypes)
        {
            // Xml message fields of Login message
            List<XmlField> fields = new() {
                new() { Name = "userid", Value = userName },
                new() { Name = "password", Value = password },
            };

            // Subscribe to message types
            var xmlGroupFields = new List<XmlField>();
            var xmlSecGroupFields = new List<XmlField>();
            messageTypes?.ToList().ForEach(x => (x.IsSecMsg ? xmlSecGroupFields : xmlGroupFields).Add(new() { Name = "MsgType", Value = x.MessageId }));

            return GetBytes(GetXml("dsgmsg", "login", fields, subscriptionFields: xmlGroupFields, subscriptionSecFields: xmlSecGroupFields));
        }

        /**
         * Get bytes from memory stream containing the XML Logout message bytes to send to DSG.
         * 
         * @param userid: User ID to log out.
         * 
         * @return Array of bytes representing the XML Logout message to send to DSG.
         * */
        public static byte[] BuildMessageLogout(string userid)
        {
            // Xml message fields of Logout message
            List<XmlField> fields = new() {
                new() { Name = "userid", Value = userid },
            };
            return GetBytes(GetXml("dsgmsg", "logout", fields));
        }

        /**
         * Get bytes from memory stream containing the XML message to send to DSG.
         * 
         * @param xmlMemoryStream: MemoryStream containing the XML message to send to DSG.
         * 
         * @return Array of bytes representing the XML message to send to DSG.
         * */
        private static byte[] GetBytes(MemoryStream xmlMemoryStream)
        {
            // Convert xml memory stream to byte array
            var xmlBytes = xmlMemoryStream.ToArray();

            // Write xml memory stream to bytes array
            var final = new byte[xmlBytes.Length + 1];
            Buffer.BlockCopy(xmlBytes, 0, final, 0, xmlBytes.Length);

            // Append the null terminator required by DSG
            final[^1] = 0x00;
            
            // Debug: print message as UTF-8 string
            //Console.WriteLine(Encoding.UTF8.GetString(xmlBytes));

            return final;
        }

        /**
         * Get framed XML message bytes to send to DSG.
         * 
         * @param xml: XML message string to send to DSG.
         * @param sequence: Sequence number of the message.
         * @param messageId: Message id of the message.
         * 
         * @return Array of bytes representing the framed XML message to send to DSG.
         * */
        private static byte[] GetMessageBytesFramed(string xml, int sequence, int messageId)
        {
            // Convert xml string to bytes array
            byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

            // Inner header (16 bytes)
            byte[] innerHeader = new byte[16];

            // Payload = inner header + xml
            byte[] payload = new byte[innerHeader.Length + xmlBytes.Length];    // Declare a payload bytes array
                                                                                // of size equal to lengths of innerHeader and xmlBytes
            
            Buffer.BlockCopy(innerHeader, 0, payload, 0, innerHeader.Length);               // Copy innerHeader to payload
            Buffer.BlockCopy(xmlBytes, 0, payload, innerHeader.Length, xmlBytes.Length);    // Copy xmlBytes to payload

            // Outer header (14 bytes)
            byte[] outerHeader = new byte[14];
            Buffer.BlockCopy(BitConverter.GetBytes(sequence), 0, outerHeader, 0, 4);        // Put sequence number into outer header
            Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, outerHeader, 4, 4);  // Put payload length into outer header
            Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, outerHeader, 8, 4);       // Put messageId into outer header
            outerHeader[12] = (byte)'1';    // Put partition id 1 (as required by DSG) into outer header
            outerHeader[13] = (byte)'3';    // Put message format 3 (as required by DSG) into outer header

            // Full message = outer header + payload
            byte[] fullMessage = new byte[outerHeader.Length + payload.Length];
            Buffer.BlockCopy(outerHeader, 0, fullMessage, 0, outerHeader.Length);           // Put outer header into full message
            Buffer.BlockCopy(payload, 0, fullMessage, outerHeader.Length, payload.Length);  // Put payload into full message

            return fullMessage;
        }

        /**
         * Build XML message memory stream to send to DSG.
         * 
         * @param root: Root element of the XML message.
         * @param name: Name of the XML message.
         * @param fields: List of fields of the XML message.
         * @param subscriptionFields: List of subscription Message Id fields of the XML message.
         * @param subscriptionSecFields: List of subscription Sec Message Id fields of the XML message.
         * 
         * @return MemoryStream containing the XML message to send to DSG.
         * */
        private static MemoryStream GetXml(string root, string name, List<XmlField> fields, List<XmlField>? subscriptionFields = null, List<XmlField>? subscriptionSecFields = null)
        {
            using (var ms = new MemoryStream())
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false), // with no Byte Order Mark
                    OmitXmlDeclaration = false,
                    Indent = false
                };

                using (var xw = XmlWriter.Create(ms, settings))
                {
                    xw.WriteStartDocument();
                    
                    // Add root element
                    xw.WriteStartElement(root);
                    xw.WriteAttributeString("name", name);

                    // Add fields
                    WriteXmlFields(xw, fields);

                    // Add subscription fields if provided
                    if (subscriptionFields is not null) WriteGroupXmlFields(xw, "SubsMsgTypes", subscriptionFields);
                    if (subscriptionSecFields is not null) WriteGroupXmlFields(xw, "SubsSecMsgTypes", subscriptionSecFields);

                    // Close root element
                    xw.WriteEndElement();

                    xw.WriteEndDocument();
                }
                return ms;
            }
        }

        /**
         * Write XML fields to XML writer.
         * 
         * @param xw: XML writer to write to.
         * @param fields: List of fields to write.
         * */
        static private void WriteXmlFields(XmlWriter xw, List<XmlField> fields)
        {
            foreach (var field in fields)
            {
                xw.WriteStartElement("field");
                xw.WriteAttributeString("name", field.Name);

                if (field.Children != null && field.Children.Any())
                {
                    WriteXmlFields(xw, field.Children); // Recursively write children
                }
                else
                {
                    xw.WriteString(field.Value ?? string.Empty);
                }

                xw.WriteEndElement();
            }
        }

        /**
         * Write XML group fields to XML writer.
         * 
         * @param xw: XML writer to write to.
         * @param name: Name of the group.
         * @param fields: List of fields to write.
         * */
        static private void WriteGroupXmlFields(XmlWriter xw, string name, List<XmlField> fields)
        {
            // Start element for the repeating group
            xw.WriteStartElement("RptGrp");
            xw.WriteAttributeString("name", name);
            WriteXmlFields(xw, fields);
            
            // Close element for the repeating group
            xw.WriteEndElement();
        }

    }
}
