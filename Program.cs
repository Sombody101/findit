using System;
using System.Collections.Generic;
using System.IO;
using Spectre.Console;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            Console.WriteLine("Usage: <search string> <directory>");
            return;
        }

        string searchString = args[0];
        string directory = args.Length == 2 ? args[1] : ".";
        Console.WriteLine($"Finding '{searchString}' in {directory}");
        SearchDirectory(directory, searchString);
    }

    static void SearchDirectory(string directory, string searchString)
    {
        //Console.CursorVisible = false;
        int references = 0;
        foreach (var filePath in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
        {
            int lineNumber = 0;
            string line;
            int windowHeight = Console.WindowHeight;
            int lineCount = File.ReadLines(filePath).Count();

            int pos = Console.CursorTop;
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(fileStream);
            while ((line = streamReader.ReadLine()) != null)
            {
                //wait();
                lineNumber++;
                if (line.Contains(searchString))
                {
                    Console.SetCursorPosition(0, pos);
                    //pos = Console.CursorTop;
                    //Console.SetCursorPosition(0, windowHeight - 2);
                    //Console.Write($" \r".PadRight(Console.BufferWidth));
                    //if (pos == Console.BufferHeight - 1)
                    //{
                    //    pos -= 2;
                    //}
                    //else if (pos == Console.BufferHeight - 2)
                    //{
                    //}
                    //else
                    //    Console.SetCursorPosition(0, pos);

                    // Remove track string from buffer
                    Console.SetCursorPosition(0, Console.BufferHeight);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, pos);

                    // Write found string to console
                    AnsiConsole.MarkupLine($"{filePath}:{lineNumber}: " +
                    $"{line.TrimStart().Replace("[", "[[").Replace("]", "]]").Replace("\n", "\\n").Replace(searchString, $"[red3]{searchString}[/]")}");
                    references++;
                }

                pos = Console.CursorTop;
                Console.SetCursorPosition(0, Console.BufferHeight);
                Console.Write($"Searching file: {filePath}  {lineNumber}/{lineCount}".PadRight(Console.BufferWidth));
            }
        }
        Console.WriteLine($"\n----------------------------\nFound {references} reference{(references == 1 ? "" : "s")}");
        Console.CursorVisible = true;
    }

    static void wait()
    {
        async Task _wait()
        {
            await Task.Delay(500);
        }
        _wait().GetAwaiter().GetResult();
    }

    static void log(string message, byte severity = 1, bool logAndExit = false, bool showHelpOnCrash = false)
    {
        AnsiConsole.MarkupLine($"findit: [" +
            $"{(severity == 1 ? "white" : (severity == 2 ? "yellow" : "red"))}" +
            $"]{(severity == 1 ? "message" : (severity == 2 ? "warning" : "fatal"))}[/]: " +
            $"{message}");
        if (severity is 3)
            Environment.Exit(3);
    }

        static bool IsBinary(byte[] bytes)
    {
        int threshold = 0;
        int maxThreshold = bytes.Length > 1024 ? 1024 : bytes.Length;

        for (int i = 0; i < maxThreshold; i++)
        {
            if (bytes[i] == 0)
                threshold++;
        }

        // If more than 10% of the first 1024 bytes are null bytes, assume it's binary
        return threshold > maxThreshold * 0.1;
    }

    static string[] ExtractStrings(byte[] bytes, Encoding encoding)
    {
        string fileContent = encoding.GetString(bytes);
        string[] strings = fileContent.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        return strings;
    }
}
