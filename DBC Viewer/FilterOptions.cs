
namespace DBCViewer
{
    struct FilterOptions
    {
        public string Col { get; set; }
        public string Val { get; set; }
        public ComparisonType Type { get; set; }
        public FilterOptions(string col, ComparisonType type, string val)
            : this()
        {
            Col = col;
            Val = val;
            Type = type;
        }
    }
}
