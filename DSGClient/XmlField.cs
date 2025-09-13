namespace DSGClient
{
        public class XmlField
        {
            public string Name { get; set; }
            public string Value { get; set; } 
            public List<XmlField> Children { get; set; } = new List<XmlField>();
        }
}
