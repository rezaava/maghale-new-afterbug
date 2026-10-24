using System;

namespace MaghaleNegar.Models
{
    public class AuthorRecord
    {
        public string AuthorId { get; set; } = Guid.NewGuid().ToString();

        public string AuthorName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public String Orcid { get; set; } = string.Empty;
        public string AuthorNameEn { get; set; } = string.Empty;

        public string UniversityType { get; set; } = string.Empty; // "Azad" یا "Dolati"

        public string AcademicDegreeFa { get; set; } = string.Empty;
        public string AcademicDegreeEn { get; set; } = string.Empty;
        public string GroupFa { get; set; } = string.Empty;
        public string GroupEn { get; set; } = string.Empty;
        public string FacultyFa { get; set; } = string.Empty;
        public string FacultyEn { get; set; } = string.Empty;
        public string UniversityFa { get; set; } = string.Empty;
        public string UniversityEn { get; set; } = string.Empty;
        public string CityFa { get; set; } = string.Empty;
        public string CityEn { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsSynced { get; set; } = false; // برای سرور در آینده
    }
}