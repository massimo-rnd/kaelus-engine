// Kaelus.cs
// created by druffko
// Copyright 2024 druffko

using kaelusengine.engine;
using kaleidolib.lib;

namespace kaelusengine
{
    public class Kaelus
    {
        public static void Main()
        {
            LogoPrinter.PrintLogo();

            Console.WriteLine(Color.red(Lining.underline("Welcome to Kaelus-Engine - Next Generation E-Mail Grabbing\n")));

            string url = "";

            // Get URL to be scanned
            Console.WriteLine("Please enter the URL you want to have scanned:");
            url = Console.ReadLine();

            // Save results to file?
            Console.WriteLine("Would you like to save the extracted emails to a file? (Y/N)");
            string decision = Console.ReadLine();
            if (decision.ToUpper().Equals("Y"))
            {
                // Extractor saveToFile = true;
                Console.WriteLine(Color.green("Will do!"));
            }
            else
            {
                Console.WriteLine(Color.yellow("Alright, wont save the output."));
            }

            Console.WriteLine("Scanning " + Color.red(Lining.thickunderline(url) + " for email-addresses..."));
            //Do Something
            Extractor.ExtractSourceCode(url);
        }
    }
}
