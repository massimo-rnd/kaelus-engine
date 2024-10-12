// Extractor.cs
// created by druffko
// Copyright 2024 druffko

using HtmlAgilityPack;
using kaleidolib.lib;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Xml;
using System.Threading.Tasks;
using System.Text;

namespace kaelusengine.engine
{
    public class Extractor
    {
        #region statics

        internal static Boolean savetoFile;
        internal static String filePath;

        // List to store all found email addresses
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

        // Method to scan for links on the page and extract emails
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

            // Process each link to extract email addresses
            foreach (var link in links)
            {
                Console.WriteLine($"Processing link: {link}");

                // Fetch the page content for each link or decode it if it's obfuscated
                if (link.Contains("/cdn-cgi/l/email-protection#"))
                {
                    // Extract and decode the email from the URL fragment
                    string encodedEmail = link.Split('#').Last();
                    string decodedEmail = DecodeCloudflareEmail(encodedEmail);

                    if (!foundEmails.Contains(decodedEmail))
                    {
                        foundEmails.Add(decodedEmail);
                        Console.WriteLine(Color.green($"Decoded Cloudflare email from URL: {decodedEmail}"));
                    }
                }
                else
                {
                    // Fetch page content normally if not Cloudflare protected
                    string linkContent = FetchPageContent(link);

                    if (!string.IsNullOrEmpty(linkContent))
                    {
                        // Process the HTML to extract email addresses
                        ProcessHtml(linkContent);
                    }
                }
            }
        }

        // Fetch page content using HtmlAgilityPack
        private static string FetchPageContent(string url)
        {
            try
            {
                var web = new HtmlWeb();
                var doc = web.Load(url);
                return doc.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine(Color.red($"An error occurred while loading the page: {ex.Message}"));
                return string.Empty;
            }
        }

        // Method to decode Cloudflare obfuscated emails
        private static string DecodeCloudflareEmail(string encoded)
        {
            int r = Convert.ToInt32(encoded.Substring(0, 2), 16);
            StringBuilder decodedEmail = new StringBuilder();

            for (int i = 2; i < encoded.Length; i += 2)
            {
                int c = Convert.ToInt32(encoded.Substring(i, 2), 16) ^ r;
                decodedEmail.Append((char)c);
            }

            return decodedEmail.ToString();
        }

        // Process HTML to extract both standard and obfuscated emails
        private static void ProcessHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Use regular expressions to find email addresses
            var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
            var matches = emailRegex.Matches(html);

            foreach (Match match in matches)
            {
                string email = match.Value;
                if (!foundEmails.Contains(email))
                {
                    foundEmails.Add(email);
                    Console.WriteLine(Color.green($"Found email: {email}"));
                }
            }

            // Look for Cloudflare obfuscated emails (data-cfemail attribute)
            var obfuscatedEmailNodes = doc.DocumentNode.SelectNodes("//a[@data-cfemail]");
            if (obfuscatedEmailNodes != null)
            {
                foreach (var node in obfuscatedEmailNodes)
                {
                    var encodedEmail = node.GetAttributeValue("data-cfemail", null);
                    if (!string.IsNullOrEmpty(encodedEmail))
                    {
                        string decodedEmail = DecodeCloudflareEmail(encodedEmail);
                        if (!foundEmails.Contains(decodedEmail))
                        {
                            foundEmails.Add(decodedEmail);
                            Console.WriteLine(Color.green($"Decoded Cloudflare email: {decodedEmail}"));
                        }
                    }
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
            using (StreamWriter sw = File.CreateText(filePath)) ;
        }

        #endregion
    }
}
