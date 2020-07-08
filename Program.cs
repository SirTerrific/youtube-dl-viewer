using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace youtube_dl_viewer
{
    public class Program
    {
        public static readonly string   DataDir      = @"/home/mike/ytdl_copy/";
        public static readonly string[] ExtVideo     = { "mkv", "mp4", "webm", "avi", "flv", "wmv", "mpg", "mpeg" };
        public static readonly string[] ExtThumbnail = { "jpg", "jpeg", "webp", "png" };

        public static string data_json = "";
        
        public static void Main(string[] args)
        {
            InitData();
            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        private static void InitData()
        {
            var datafiles = Directory.EnumerateFiles(DataDir).ToList();
            var processedFiles = new List<string>();

            var filesSubs = datafiles.Where(p => p.EndsWith(".vtt")).ToList();
            var filesInfo = datafiles.Where(p => p.EndsWith(".info.json")).ToList();

            var resultVideos = new JArray();
            
            foreach (var pathJson in filesInfo)
            {
                JObject jinfo;
                try
                {
                    jinfo = JObject.Parse(File.ReadAllText(pathJson));
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not parse file: '{pathJson}'", e);
                }
                
                var dir = Path.GetDirectoryName(pathJson);
                if (dir == null) continue;

                var filenameJson = Path.GetFileName(pathJson);

                var filenameBase = filenameJson.Substring(0, filenameJson.Length - ".info.json".Length);

                var pathDesc = Path.Combine(dir, filenameBase + ".description");
                if (!datafiles.Contains(pathDesc)) pathDesc = null;

                var pathVideo = ExtVideo.Select(ext => Path.Combine(dir, filenameBase + "." + ext)).FirstOrDefault(p => datafiles.Contains(p));
                if (pathVideo == null) continue;

                var pathThumb = ExtThumbnail.Select(ext => Path.Combine(dir, filenameBase + "." + ext)).FirstOrDefault(p => datafiles.Contains(p));

                var pathSubs = filesSubs
                    .Where(p => dir == Path.GetDirectoryName(p))
                    .Where(p => Path.GetFileName(p).EndsWith(".vtt"))
                    .Where(p => Path.GetFileName(p).StartsWith(filenameBase + "."))
                    .ToList();
                
                processedFiles.Add(pathJson);
                if (pathDesc != null) processedFiles.Add(pathDesc);
                if (pathThumb != null) processedFiles.Add(pathThumb);
                processedFiles.Add(pathVideo);
                processedFiles.AddRange(pathSubs);
                
                resultVideos.Add(new JObject
                (
                    new JProperty("meta", new JObject
                    (
                        new JProperty("directory", dir),
                        
                        new JProperty("filename_base", filenameBase),
                        
                        new JProperty("path_json", pathJson),
                        new JProperty("path_description", pathDesc),
                        new JProperty("path_video", pathVideo),
                        new JProperty("path_thumbnail", pathThumb),
                        new JProperty("paths_subtitle", new JObject(pathSubs.Select(p => new JProperty(Path.GetFileNameWithoutExtension(p).Substring(filenameBase.Length+1), p))))
                    )),
                    new JProperty("data", new JObject
                    (
                        new JProperty("info", jinfo),
                        new JProperty("description", (pathDesc != null) ? File.ReadAllText(pathDesc) : null)
                    ))
                ));
            }
            
            var result = new JObject
            (
                new JProperty("videos", resultVideos),
                new JProperty("missing", new JArray(datafiles.Except(processedFiles).ToArray<object>()))
            );

            data_json = result.ToString(Formatting.Indented);
        }
    }
}