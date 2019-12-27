using System.Collections.Generic;

namespace manga_dl.Common
{
    public class Manga
    {
        public string CoverUrl;
        public string Title;
        public string Author;
        public Dictionary<string, List<uint>> Chapters;
    }
}