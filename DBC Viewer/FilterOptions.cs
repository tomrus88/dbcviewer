
namespace DBCViewer
{
    struct FilterOptions
    {
        public string Col { get; set; }
        public string Op { get; set; }
        public string Val { get; set; }

        public FilterOptions(string col, string op, string val)
            : this()
        {
            Col = col;
            Op = op;
            Val = val;
        }
    }
}
