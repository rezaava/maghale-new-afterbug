using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MaghaleNegar.Constants;


namespace MaghaleNegar.Templates
{
    internal class JournalSubTemplate
    {
        internal string Id;
        internal int Order;
        internal string FileName;
        internal string ResourcePath;
    }

    internal class JournalTemplateAccess
    {
        private static XmlDocument LoadJournalsXml()
        {
            using (Stream stream = DedicatedFunctions.getStream(EmbeddedResourceNames.Journals))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);
                return doc;
            }
        }
        private static XmlNode FindJournal(string journalName)
        {
            if (string.IsNullOrEmpty(journalName))
                return null;

            XmlDocument doc = LoadJournalsXml();
            foreach (XmlNode journal in doc.SelectNodes("//journal"))
            {
                if (journal.Attributes["name"].Value == journalName)
                    return journal;
            }
            return null;
        }

        internal static string GetTemplateFileName(string journalName)
        {
            XmlNode journal = FindJournal(journalName);
            return journal?.Attributes["fileName"].Value;
        }

        internal static Stream GetTemplateFileStream(string journalName)
        {
            XmlNode journal = FindJournal(journalName);
            if (journal == null)
                return null;

            string resourcePath = journal.Attributes["path"].Value + journal.Attributes["fileName"].Value;
            return DedicatedFunctions.getStream(resourcePath);
        }

        internal static List<JournalSubTemplate> GetSubTemplates(string journalName, bool requiredOnly)
        {
            List<JournalSubTemplate> result = new List<JournalSubTemplate>();

            XmlNode journal = FindJournal(journalName);
            if (journal == null)
                return result;

            string path = journal.Attributes["path"].Value;

            HashSet<string> requiredIds = new HashSet<string>();
            foreach (XmlNode required in journal.SelectNodes("requiredSubTemplates/required"))
                requiredIds.Add(required.Attributes["id"].Value);

            foreach (XmlNode subTemplate in journal.SelectNodes("subTemplates/subTemplate"))
            {
                string id = subTemplate.Attributes["id"].Value;
                if (requiredOnly && !requiredIds.Contains(id))
                    continue;

                result.Add(new JournalSubTemplate
                {
                    Id = id,
                    Order = int.Parse(subTemplate.Attributes["order"].Value),
                    FileName = subTemplate.Attributes["fileName"].Value,
                    ResourcePath = path + subTemplate.Attributes["fileName"].Value
                });
            }

            return result.OrderBy(s => s.Order).ToList();
        }

    }
}