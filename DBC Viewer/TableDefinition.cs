using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace DBCViewer
{
    [Serializable]
    public class DBFilesClient
    {
        [XmlElement("Table")]
        public List<Table> Tables { get; set; }

        public static DBFilesClient Load(string path)
        {
            XmlSerializer deser = new XmlSerializer(typeof(DBFilesClient));
            using (var fs = new FileStream(path, FileMode.Open))
                return (DBFilesClient)deser.Deserialize(fs);
        }

        public static void Save(DBFilesClient db, string path)
        {
            XmlSerializer ser = new XmlSerializer(typeof(DBFilesClient));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);
            using (var fs = new FileStream(path, FileMode.Create))
                ser.Serialize(fs, db, namespaces);
        }
    }

    [Serializable]
    public class Table
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public int Build { get; set; }
        [XmlElement("Field")]
        public List<Field> Fields { get; set; }
    }

    [Serializable]
    public class Field
    {
        [XmlIgnore]
        public int Index { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Type { get; set; }
        [XmlAttribute, DefaultValue("")]
        public string Format { get; set; } = string.Empty;
        [XmlAttribute, DefaultValue(1)]
        public int ArraySize { get; set; } = 1;
        [XmlAttribute, DefaultValue(false)]
        public bool IsIndex { get; set; } = false;
        [XmlAttribute, DefaultValue(true)]
        public bool Visible { get; set; } = true;
        [XmlAttribute, DefaultValue(100)]
        public int Width { get; set; } = 100;
    }
}
