using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace manga_dl.Common
{
    public class MangaDex : IApi
    {
        public static readonly string BaseUrl = "https://mangadex.org";

        private static int _requestCount = 0;
        public static int RequestCount { 
            get {
                return _requestCount;
            }
            set {
                _requestCount = value;
            }
        }

        public async Task<Manga> GetMangaDataFromApiAsync(uint id) => await MangaDex.GetMangaDataAsync(id);
        public static async Task<Manga> GetMangaDataAsync(uint id)
        {            
            using (HttpClient http = new HttpClient())
            {
                RequestCount++;
                string json = await http.GetStringAsync($"{BaseUrl}/api/manga/{id}");

                JObject obj = JObject.Parse(json);

                JObject mangaobj = obj["manga"] as JObject;
                JObject chapters = obj["chapter"] as JObject;

                Manga manga = new Manga 
                {
                    CoverUrl = BaseUrl + mangaobj["cover_url"].ToObject<string>(),
                    Title = mangaobj["title"].ToObject<string>(),
                    Author = mangaobj["author"].ToObject<string>(),
                    Chapters = new Dictionary<string, List<uint>>(),
                };
                IEnumerable<KeyValuePair<string, uint>> preDict = chapters.Properties()
                    .Select(p => new KeyValuePair<string, uint>((p.Value as JObject)["lang_code"].ToObject<string>(), uint.Parse(p.Name)));
                foreach(KeyValuePair<string,uint> pair in preDict) 
                {
                    if(!manga.Chapters.ContainsKey(pair.Key))
                        manga.Chapters[pair.Key] = new List<uint>();
                    manga.Chapters[pair.Key].Add(pair.Value);
                }
                return manga;
            }
        }

        public async Task<Chapter> GetChapterDataFromApiAsync(uint id) => await MangaDex.GetChapterDataAsync(id);
        public static async Task<Chapter> GetChapterDataAsync(uint id)
        {
            using (HttpClient http = new HttpClient())
            {
                RequestCount++;
                string json;
                try
                {
                    json = await http.GetStringAsync($"{BaseUrl}/api/chapter/{id}");
                }
                catch(HttpRequestException e)
                {
                    if((e.InnerException as SocketException).SocketErrorCode == SocketError.TryAgain)
                    {
                        System.Console.WriteLine("Socket error, retrying in 1 second");
                        await Task.Delay(1000);
                        return await GetChapterDataAsync(id);
                    }
                    throw e;
                }

                JObject obj = JObject.Parse(json);

                try 
                {
                    JArray pages = obj["page_array"] as JArray;
                    string[] urls = new string[pages.Count];
                    for(int i = 0; i < pages.Count; i++)
                    {
                        urls[i] = obj["server"].ToObject<string>() + obj["hash"].ToObject<string>() + "/" + pages[i].ToObject<string>();
                    }

                    return new Chapter 
                    {
                        Title = obj["title"].ToObject<string>(),
                        Volume = obj["volume"].TryToObject<uint>() ?? 0,
                        ChapterNo = obj["chapter"].TryToObject<float>() ?? 0,
                        Language = obj["lang_name"].ToObject<string>(),
                        PageUrls = urls,
                    };
                }
                catch(FormatException e)
                {
                    System.Console.Error.WriteLine(e.ToString());
                    throw e;
                }
            }

        }
    }
    public static class IEnumerableExtensionMethods
    {
        public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
        {
            foreach (T item in @this)
            {
                action(item);
            }
        }
    }

    public static class JsonExtensions
    {
        public static T? TryToObject<T>(this JToken token) where T : struct
        {
            bool isNullOrEmpty = (token == null) ||
                (token.Type == JTokenType.Array && !token.HasValues) ||
                (token.Type == JTokenType.Object && !token.HasValues) ||
                (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                (token.Type == JTokenType.Null);
            T? value;
            if(isNullOrEmpty)
                value = null;
            else
            {
                try 
                {
                    value = token.ToObject<T>();
                }
                catch(Exception)
                {
                    value = null;
                }
            }
            return value;
        }
    }
}
