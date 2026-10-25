using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaghaleNegar.Templates
{
    internal class JournalTemplateMap
    {
        private static readonly Dictionary<string, string> map = new Dictionary<string, string>
        {
            { "نشریه علمی ابتکار و خلاقیت در علوم انسانی", "ArticleTemplate-Ebtekar.docx" },
            { "رویکرد الهی ـ فرهنگی در تدریس", "ArticleTemplate-TCAT.docx" },
            { "جستارنامه ادبیات تطبیقی", "ArticleTemplate-JOSTARNAMEH.docx" },
            { "پژوهش های بازاریابی اسلامی", "ArticleTemplate-IMR.docx" },
            { "International Journal of Applied Arts Studies", "ArticleTemplate-IJAPAS.docx" },
            { "The Journal of Radar and Optical Remote Sensing", "ArticleTemplate-JRORS.docx" },
            { "Artificial Intelligence Tools in Software and Data Engineering (AITSDE)", "ArticleTemplate-AIT.docx" },
        };
        private static readonly HashSet<string> englishJournals = new HashSet<string>
        {
            "International Journal of Applied Arts Studies",
            "The Journal of Radar and Optical Remote Sensing",
            "Artificial Intelligence Tools in Software and Data Engineering (AITSDE)",
        };

        internal static bool IsEnglish(string journalName)
        {
            return journalName != null && englishJournals.Contains(journalName);
        }

        internal static string GetFileName(string journalName)
            {
                if (journalName != null && map.TryGetValue(journalName, out string file))
                    return file;
                return null; // قالب ندارد
            }
        }
    }
