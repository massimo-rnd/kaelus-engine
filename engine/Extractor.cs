// Extractor.cs
// created by druffko
// Copyright 2024 druffko

using HtmlAgilityPack;
using kaleidolib.lib;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Xml;

namespace kaelusengine.engine
{
    public class Extractor
    {
        #region statics

        internal static Boolean savetoFile;
        internal static String filePath;

        //List to store all found email addresses
        internal static HashSet<string> foundEmails = new HashSet<string>();

        #endregion

        #region static methods

        internal static void ExtractSourceCode(string url)
        {
            if (savetoFile)
            {
                CreateFile();
            }

            bool isUrl = IsUrl(url);

            if (isUrl)
            {
                Console.WriteLine(Color.green($"{url} is a valid URL."));
            }
            else
            {
                Console.WriteLine(Color.red($"You provided an invalid URL. {url} is not a valid URL."));
                Console.WriteLine(Color.yellow("Don't worry, I'll try to fix your url..."));
                url = "https://" + url;
                Console.WriteLine(Color.green($"Done! Your URL is now {url}"));
            }

            // Scan the main index page and get all links on it
            ScanLinks(url);

            // Display the collected emails at the end
            DisplayCollectedEmails();
        }

        // Method to scan for links on the page
        internal static void ScanLinks(String url)
        {
            List<string> links = new List<string>();

            // Fetch page source
            string pageContent = FetchPageContent(url);

            if (string.IsNullOrEmpty(pageContent))
            {
                Console.WriteLine("No content found on the page.");
                return;
            }

            // Use HtmlAgilityPack to parse the HTML and find all links
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                string href = link.GetAttributeValue("href", string.Empty);

                // If it's a relative link, convert it to an absolute URL
                Uri baseUri = new Uri(url);
                Uri fullUri = new Uri(baseUri, href);

                if (fullUri.Host == baseUri.Host) // Ensure it's the same domain
                {
                    links.Add(fullUri.ToString());
                }
            }

            // Scan each link for email addresses
            foreach (string link in links)
            {
                Console.WriteLine(kaleidolib.lib.Formatting.dim($"Scanning {link} for email addresses..."));
                string content = FetchPageContent(link);
                if (!string.IsNullOrEmpty(content))
                {
                    GetMails(content);
                }
            }
        }

        // Method to fetch the page content
        internal static string FetchPageContent(string url)
        {
            try
            {
                using (HttpClient client = new())
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch {url}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching page content: {ex.Message}");
            }
            return string.Empty;
        }

        // Method to collect emails found on a page
        internal static void GetMails(String sourceCode)
        {
            string emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";

            // Use Regex.Matches to find all occurrences of email addresses in the input string
            MatchCollection matches = Regex.Matches(sourceCode, emailPattern, RegexOptions.IgnoreCase);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    // Add to the HashSet to ensure uniqueness
                    foundEmails.Add(match.Value);
                }
            }
        }

        // Display collected emails and filter duplicates
        internal static void DisplayCollectedEmails()
        {
            if (foundEmails.Count > 0)
            {
                Console.WriteLine(Color.green("Good news! I found the following unique email addresses:"));
                foreach (var email in foundEmails)
                {
                    Console.WriteLine(email);
                }
            }
            else
            {
                Console.WriteLine(Color.red("No email addresses found."));
            }
        }

        static bool IsUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out Uri result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmss");
        }
        internal static void CreateFile()
        {
            //get timestamp
            String timeStamp = GetTimestamp(DateTime.Now);

            //create file
            filePath = timeStamp + "-output.txt";
            using (StreamWriter sw = File.CreateText(filePath));
        }

        #endregion

    }
}
