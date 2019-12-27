using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using manga_dl.Common;

namespace manga_dl.Console
{
    class Program
    {
        static readonly string defaultApi = "mangadex";

        static string[] languagePreference = {
            "gb"
        };

        static void Main(string[] args)
        {
            if(args.Length < 1) 
            {
                Help();
                return;
            }
            switch(args[0])
            {
                case "manga": {
                    if(args.Length < 2)
                        Help();
                    else
                        Manga(args[1], args.Length >= 3 ? args[2] : defaultApi, args.Length >= 4 ? args[3] : false);
                } break;
                default: {
                    Help();
                } break;
            }
        }

        static void Help()
        {
            System.Console.WriteLine("Incorrect parameters. TODO help section");
        }

        static void Manga(string strId, string api, bool concurrent)
        {
            uint id;
            if(!uint.TryParse(strId, out id))
            {
                System.Console.Error.WriteLine($"[Manga] ERROR: Unable to parse {id}");
                return;
            }

            switch(api)
            {
                case "mangadex": {
                    Task<Manga> task = MangaDex.GetMangaDataAsync(id);
                    System.Console.Write($"Downloading {id} from mangadex");
                    while(!task.IsCompleted)
                    {
                        Thread.Sleep(1000);
                        System.Console.Write(".");
                    }
                    System.Console.WriteLine();
                    if(!task.IsCompletedSuccessfully)
                    {
                        task.Exception.Handle(e => {
                            if(e is System.Net.Http.HttpRequestException httpEx) 
                            {
                                // Have to do this since .net is stupid
                                if(httpEx.ToString().Contains("Forbidden"))
                                {
                                    System.Console.Error.WriteLine(
                                        "[Manga] ERROR: You probably got tempbanned for too many requests\n" + 
                                        "Check https://mangadex.org for more information"
                                    );
                                    return true;
                                }
                            }
                            return false;
                        });
                        return;
                    }

                    Manga manga = task.Result;

                    Dictionary<string, List<uint>> goodChapters = manga.Chapters
                        .Where(c => languagePreference.Contains(c.Key))
                        .ToDictionary(p => p.Key, p => p.Value);

                    // Get number of chapters for each language
                    Dictionary<string, uint> languages = goodChapters
                        .Select(p => new KeyValuePair<string, uint>(p.Key, (uint)p.Value.Count))
                        .ToDictionary(p => p.Key, p => p.Value);

                    string lang;
                    if(languages.Count > 1)
                    {
                        System.Console.WriteLine("Multiple languages detected, please choose");
                        int i = 0;
                        foreach(KeyValuePair<string, uint> pair in languages)
                        {
                            System.Console.WriteLine($"{i}. {pair.Key} (Contains {pair.Value} chapters)");
                            i++;
                        }
                        uint choice;
                        do {
                            System.Console.WriteLine();
                            System.Console.Write("> ");
                        } while(!uint.TryParse(System.Console.ReadLine(), out choice) && choice > languages.Count);
                        lang = languages.Keys.ToArray()[choice];
                    }
                    else
                    {
                        lang = languages.Keys.First();
                    }

                    List<uint> chapterIds = goodChapters[lang];

                    Task<Chapter>[] chapterTasks = new Task<Chapter>[chapterIds.Count];
                    for(int i = 0; i < chapterIds.Count; i++) 
                    {
                        if(i % 10 == 4)
                        {
                            System.Console.WriteLine("Too many chapters, delaying threads");
                            int index = i; // store a copy of the index for the lambda
                            chapterTasks[i] = Task.Run(async () => {
                                await Task.Delay(1000);
                                return await MangaDex.GetChapterDataAsync(chapterIds[index]);
                            });
                        }
                        chapterTasks[i] = MangaDex.GetChapterDataAsync(chapterIds[i]);
                        try 
                        {
                            if(!concurrent)
                                chapterTasks[i].Wait();
                        }
                        catch(Exception e)
                        {
                            throw e;
                        }
                        System.Console.WriteLine($"{chapterTasks.Count(t => t?.IsCompleted ?? false)} / {chapterTasks.Length}");
                    }
                    Task<Chapter[]> chaptersTask = Task.WhenAll(chapterTasks);
                    int count = chapterTasks.Count();
                    while(!chaptersTask.IsCompleted) 
                    {
                        System.Console.WriteLine($"{chapterTasks.Count(t => t.IsCompleted)} / {count}");
                        Thread.Sleep(1000);
                    }
                    if(!chaptersTask.IsCompletedSuccessfully)
                        chaptersTask.Exception.Handle(e => false);
                    Chapter[] chapters = chaptersTask.Result;
                    Array.Sort(chapters, (Chapter a, Chapter b) => {
                        return a.Volume.CompareTo(b.Volume);
                    });
                    Array.Sort(chapters, (Chapter a, Chapter b) => {
                        return a.ChapterNo.CompareTo(b.ChapterNo);
                    });

                    string pwd = Directory.GetCurrentDirectory();
                    string mangaDirPath = Path.Combine(pwd, "Mangas");
                    if(!Directory.Exists(mangaDirPath))
                        Directory.CreateDirectory(mangaDirPath);
                    string currentMangaDirPath = Path.Combine(mangaDirPath, manga.Title);
                    if(!Directory.Exists(currentMangaDirPath))
                        Directory.CreateDirectory(currentMangaDirPath);
                    foreach(Chapter chapter in chapters) 
                    {
                        string chapterDirPath = Path.Combine(currentMangaDirPath, $"{chapter.Volume}-{chapter.ChapterNo}");
                        string pdfFilePath = Path.Combine(currentMangaDirPath, $"{chapter.Volume}-{chapter.ChapterNo}.pdf");
                        if(!Directory.Exists(chapterDirPath))
                            Directory.CreateDirectory(chapterDirPath);
                        using(WebClient wc = new WebClient())
                        {
                            string[] filePaths = new string[chapter.PageUrls.Length];
                            for(int i = 0; i < chapter.PageUrls.Length; i++)
                            {
                                string filePath = filePaths[i] = Path.Combine(chapterDirPath, $"page{i}.png");
                                if(File.Exists(filePath))
                                {
                                    System.Console.WriteLine($"{filePath} already exists. Skipping");
                                    continue;
                                }
                                int tries = 0;
                                void downloadImage() 
                                {
                                    tries++;
                                    try
                                    {
                                        byte[] data = wc.DownloadData(chapter.PageUrls[i]);
                                        using(MemoryStream ms = new MemoryStream(data))
                                        {
                                            using(Bitmap img = new Bitmap(ms))
                                            {
                                                img.Save(filePath, ImageFormat.Png);
                                            }
                                        }
                                    }
                                    catch (WebException e)
                                    {
                                        System.Console.Error.WriteLine($"[Manga] ERROR: {e.ToString()}");
                                        if(tries > 10)
                                        {
                                            throw e;
                                        }
                                        System.Console.WriteLine($"WebException, retry {tries + 1} in 1 second");
                                        Thread.Sleep(1000);
                                        downloadImage();
                                    }
                                }
                                downloadImage();
                            }
                            Converter.CreatePDF(filePaths, pdfFilePath);
                        }
                    }
                    System.Console.WriteLine("Download complete");

                } break;
                default: {
                    System.Console.Error.WriteLine($"[Manga] ERROR: Unable to find api {api}");
                    return;
                }
            }
        }
    }
}
