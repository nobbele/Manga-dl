using System.Threading.Tasks;

namespace manga_dl.Common
{
    public interface IApi
    {
        Task<Manga> GetMangaDataFromApiAsync(uint id);
        Task<Chapter> GetChapterDataFromApiAsync(uint id);
    }
}