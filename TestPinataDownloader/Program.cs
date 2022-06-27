/* 
Copyright ©2022. Nicholas Tantillo. All Rights Reserved. 
Permission to use, copy, modify, and distribute this software
and its documentation for educational, research, and 
not-for-profit purposes, without fee and without a signed licensing
agreement, is hereby granted, provided that the above copyright 
notice, this paragraph and the following two paragraphs appear 
in all copies, modifications, and distributions. 

IN NO EVENT SHALL REGENTS BE LIABLE TO ANY PARTY FOR DIRECT, 
INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING 
LOST PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS 
DOCUMENTATION, EVEN IF REGENTS HAS BEEN ADVISED OF THE POSSIBILITY 
OF SUCH DAMAGE.

NICHOLAS TANTILLO SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING,
BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND 
FITNESS FOR A PARTICULAR PURPOSE. THE SOFTWARE AND ACCOMPANYING 
DOCUMENTATION, IF ANY, PROVIDED HEREUNDER IS PROVIDED "AS IS". 
NICHOLAS TANTILLO HAS NO OBLIGATION TO PROVIDE MAINTENANCE, 
SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

*/


using System;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TestPinataDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                WebClient client = new WebClient();

                var api = "https://api.pinata.cloud/data/";

                Action<string> cout = (x) =>
                {
                    Console.WriteLine(x);

                };

                Func<string, dynamic> get = (x) =>
                {
                    return JObject.Parse(client.DownloadString($"{api}{x}"));
                };



                if (args.Length > 0 && args[0] == "RUN")
                {

                    var JWT = File.ReadAllText("bearer.jwt");

                    client.Headers.Add("Authorization", "Bearer " + JWT);

                    cout($"{get("testAuthentication").message}");
                    dynamic pinnedFiles = get("userPinnedDataTotal");
                    cout($"You have: {pinnedFiles.pin_count} files totalling  {pinnedFiles.pin_size_total}");

                    File.WriteAllText("run.json", "{\"pages\":[");

                    // Get pages synchronously
                    for (int page = 0; page < Math.Ceiling(float.Parse($"{pinnedFiles.pin_count}") / 1000.00); page++)
                    {
                        cout($"Downloading page {page}");
                        dynamic pageFiles = get($"pinList?pageOffset={page}&pageLimit=1000&status=pinned"); // pinned, unpinned or all
                        File.AppendAllText("run.json", (page > 0 ? "," : "") + pageFiles);
                    }

                    File.AppendAllText("run.json", "]}");
                    cout($"Pages saved to run.json, you may now run DOWNLOAD");
                }

                var pdir = "pinata_files";

                if (args.Length > 0 && args[0] == "RESET")
                {
                    if (Directory.Exists(pdir))
                    {
                        Directory.Delete(pdir, true);
                    }
                    File.Delete("run.json");

                }

                if (args.Length > 0 && args[0] == "DOWNLOAD")
                {


                    if (!Directory.Exists(pdir))
                    {
                        Directory.CreateDirectory(pdir);
                    }
                    dynamic fileList = JObject.Parse(File.ReadAllText("run.json"));

                    int pg = 0;
                    int fc = 0;
                    int ec = 0;
                    // First thing is array of pages
                    foreach (dynamic page in fileList.pages)
                    {
                        cout($">>>> Page {pg++}");
                        foreach (dynamic row in page.rows)
                        {
                            var ts = DateTime.Now.Ticks;
                            fc++;

                            if (File.Exists($"{pdir}/{row.metadata.name}"))
                            {
                                cout($">>>>>> Skip: {row.metadata.name}");
                                continue;
                            }


                            var fname = String.IsNullOrEmpty($"{row.metadata.name}") ? $"{pdir}/Unnamed_File_{fc}" : $"{pdir}/{row.metadata.name}";
                            var hash = $"https://gateway.pinata.cloud/ipfs/{row.ipfs_pin_hash}";

                            cout($">>>>>> Download: {fname} from {hash}");

                            try
                            {
                                client.DownloadFile(hash, fname);

                                var elapsedSpan = new TimeSpan(DateTime.Now.Ticks - ts);
                                int sleepTime = 5000 - Convert.ToInt32(elapsedSpan.TotalMilliseconds);
                                if (sleepTime > 0)
                                {
                                    cout($"        Sleep for {sleepTime / 1000.0}s to avoid rate limit.");
                                    System.Threading.Thread.Sleep(sleepTime);
                                }
                            }
                            catch (Exception ex)
                            {
                                ec++;
                                cout($"        Exception with file: {ex.Message}\r\n!!! Retry may be possible if you execute the DOWNLOAD command again.");
                            }

                        }
                    }

                    if (ec > 0)
                    {
                        cout($"Encounterd {ec} errors but retry may be possible. Try running this program again with the DOWNLOAD command.");
                    }


                }


                Console.ReadKey();
            }
            catch (Exception fex)
            {
                Console.WriteLine("FATAL EXCEPTION:" + fex.Message);
            }

        }

    }

}
