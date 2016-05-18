using System;
using System.IO;
using System.Xml;

namespace DBCViewer
{
    class DBReaderFactory
    {
        public static IWowClientDBReader GetReader(string file, XmlElement def)
        {
            IWowClientDBReader reader;

            var ext = Path.GetExtension(file).ToUpperInvariant();
            if (ext == ".DBC")
                reader = new DBCReader(file);
            else if (ext == ".DB2")
                try
                {
                    reader = new DB2Reader(file);
                }
                catch
                {
                    try
                    {
                        reader = new DB3Reader(file);
                    }
                    catch
                    {
                        try
                        {
                            reader = new DB4Reader(file);
                        }
                        catch
                        {
                            reader = new DB5Reader(file, def);
                        }
                    }
                }
            else if (ext == ".ADB")
                reader = new ADBReader(file);
            else if (ext == ".WDB")
                reader = new WDBReader(file);
            else if (ext == ".STL")
                reader = new STLReader(file);
            else
                throw new InvalidDataException(String.Format("Unknown file type {0}", ext));

            return reader;
        }
    }
}
