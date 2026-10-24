using MaghaleNegar.Models;
using System.Collections.Generic;

namespace MaghaleNegar.Interfaces
{
    public interface IAuthorStore
    {
        IReadOnlyList<AuthorRecord> GetAll();
        AuthorRecord FindByName(string authorName);
        void Save(AuthorRecord author);
        bool Delete(string authorId);
    }
}