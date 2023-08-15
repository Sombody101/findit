using System;
using System.Collections.Generic;
using System.IO;
using Spectre.Console;
using System.Text;

class Program
{
    static Args Settings = new Args();
    static void Main(string[] a)
    {
        Console.CancelKeyPress += OnClose;
        AppDomain.CurrentDomain.ProcessExit += OnClose;

        List<string> args = a.ToList();

        if (args.Count is 0 or 1)
        {
            Console.WriteLine("Usage: <args> <search string> <directory>");
            return;
        }

        int i = 0;
        void RemArg()
        {
            args.RemoveAt(i);
        }

        for (; i < args.Count; i++)
        {
            string arg = args[i];
            if (arg is "--")
                break;

            if (arg.StartsWith("--"))
                switch (arg)
                {
                    case "--exclude-binary":
                        if (Settings.GetStringsFromBinaryFiles)
                            log($"Cannot use '{args[i]}' when --extract-strings is set to true", 3);
                        Settings.ExcludeBinaryFiles = true;
                        RemArg();
                        break;
                    case "--extract-strings":
                        if (Settings.ExcludeBinaryFiles)
                            log($"Cannot use '{args[i]}' when --exclude-binary is set to true", 3);
                        Settings.GetStringsFromBinaryFiles = true;
                        RemArg();
                        break;
                    case "--top-level":
                        if (Settings.SearchDepth is not int.MinValue)
                            log($"Cannot use '{args[i]}' when --search-level is set", 3);
                        Settings.SearchDepth = 1;
                        RemArg();
                        break;
                    case "--search-level":
                        if (Settings.OnlyTopDirectory)
                            log($"Cannot use '{args[i]}' when --top-level is set to true", 3);
                        var w = int.TryParse(args[i + 1], out int t);
                        if (!w)
                            log($"Non int value '{args[i + 1]}' entered for switch {args[i]}", 3);
                        if (t is < 0)
                            log($"Non positive value '{args[i + 1]}' entered for switch {args[i]}", 3);
                        Settings.SearchDepth = t;
                        args.RemoveAt(i + 1);
                        RemArg();
                        break;
                    default:
                        log($"Unknown switch '{args[i]}' found at index {i}", 3);
                        break;
                }
            else if (arg.StartsWith('-'))
            {
                for (int x = 1; x < arg.Length; x++)
                    switch (arg[x])
                    {
                        case 'b':
                            if (Settings.GetStringsFromBinaryFiles)
                                log($"Cannot use '{arg[x]}' when --extract-strings is set to true", 3);
                            Settings.ExcludeBinaryFiles = true;
                            break;
                        case 's':
                            if (Settings.ExcludeBinaryFiles)
                                log($"Cannot use '{arg[x]}' when --exclude-binary is set to true", 3);
                            Settings.GetStringsFromBinaryFiles = true;
                            break;
                        case 'l':
                            if (Settings.OnlyTopDirectory)
                                log($"Cannot use '{arg[x]}' when --top-level is set to true", 3);
                            var w = int.TryParse(args[i + 1], out int t);
                            if (!w)
                                log($"Non int value '{args[i + 1]}' entered for switch '{arg[x]}'", 3);
                            if (t is < 0)
                                log($"Non positive value '{args[i + 1]}' entered for switch '{arg[x]}'", 3);
                            Settings.SearchDepth = t;
                            args.RemoveAt(i + 1);
                            break;
                        case 't':
                            if (Settings.SearchDepth is not int.MinValue)
                                log($"Cannot use '{arg[x]}' when --search-level is set", 3);
                            break;
                        default:
                            log($"Unknown switch '{arg[x]}' found at index {x} in '{args[i]}'", 3);
                            break;
                    }
                RemArg();
            }
        }

        if (args.Count is 1 or 0)
            log($"Not enough data entered. Input args should be ", 3);

        string directory = args[args.Count - 1];
        args.RemoveAt(args.Count - 1);

        string searchString = "";
        foreach (string str in args)
            searchString += str + " ";
        searchString = searchString.Substring(0, searchString.Length - 1);
        Console.WriteLine($"Searching for '{searchString}' in {(directory is "." ? "current directory" : directory)}");
        SearchDirectory(directory, searchString, Settings.SearchDepth);
    }

    static void SearchDirectory(string directory, string searchString, int searchDepth)
    {
        //Console.CursorVisible = false;
        int references = 0;
        foreach (var filePath in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (Settings.SearchDepth is not int.MinValue)
            {
                if (searchDepth is 0)
                    break;
                searchDepth--;
            }
            int lineNumber = 0;
            string? line = "";
            bool isBinaryFile = false;
            int windowHeight = Console.WindowHeight;
            int lineCount = File.ReadLines(filePath).Count();

            int pos = Console.CursorTop;
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            void TrackLine()
            {
                Console.CursorVisible = false;
                Console.SetCursorPosition(0, Console.WindowHeight);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, pos);

                // Write found string to console
                AnsiConsole.MarkupLine($"{filePath}:{lineNumber}: " +
                $"{(line ?? "").TrimStart().Replace("[", "[[").Replace("]", "]]").Replace("\n", "\\n").Replace(searchString, $"[red3]{searchString}[/]")}");
                Console.CursorVisible = true;
            }

            byte[] fileBytes = new byte[fileStream.Length];
            fileStream.Read(fileBytes, 0, fileBytes.Length);
            isBinaryFile = IsBinary(fileBytes);
            if (Settings.ExcludeBinaryFiles && isBinaryFile)
                continue;
            if (!Settings.GetStringsFromBinaryFiles && isBinaryFile)
            {
                int count = 0;
                var strs = GetStrings(fileBytes, Encoding.UTF32);
                foreach (string str in strs)
                {
                    count++;
                    if (str.Contains(searchString))
                    {
                        line = str;
                        TrackLine();
                        references++;
                    }

                    Console.CursorVisible = false;
                    pos = Console.CursorTop;
                    Console.SetCursorPosition(0, Console.WindowHeight);
                    Console.Write($"Searching file: {filePath} extracted strings {count}/{strs.Length}".PadRight(Console.BufferWidth));
                    Console.CursorVisible = true;
                }
                continue;
            }

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
                    TrackLine();
                    references++;
                }

                Console.CursorVisible = false;
                pos = Console.CursorTop;
                Console.SetCursorPosition(0, Console.WindowHeight);
                Console.Write($"Searching file: {filePath}  {lineNumber}/{lineCount}".PadRight(Console.BufferWidth));
                Console.CursorVisible = true;
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
        //if (severity is 4) // Show help
        //    
        if (severity is >= 3)
            Environment.Exit(severity);
    }

    static bool IsBinary(byte[] bytes)
    {
        int threshold = 0;
        int maxThreshold = bytes.Length > 1024 ? 1024 : bytes.Length;

        for (int i = 0; i < maxThreshold; i++)
            if (bytes[i] is 0)
                threshold++;

        // If more than 10% of the first 1024 bytes are null bytes, assume it's binary
        return threshold > maxThreshold * 0.1;
    }

    static string[] GetStrings(byte[] bytes, Encoding encoding)
    {
        List<string> extractedStrings = new List<string>();

        StringBuilder stringBuilder = new StringBuilder();
        foreach (byte b in bytes)
        {
            // Check if the byte represents a printable ASCII character
            if (b >= 32 && b <= 126)
                stringBuilder.Append((char)b);
            else if (stringBuilder.Length > 0)
            // Add the extracted string to the list if it is not empty
            {
                extractedStrings.Add(stringBuilder.ToString());
                stringBuilder.Clear();
            }
        }

        // Add the last extracted string if it is not empty
        if (stringBuilder.Length > 0)
            extractedStrings.Add(stringBuilder.ToString());

        return extractedStrings.ToArray();
    }

    static void OnClose(object? e, EventArgs x)
        => _OnClose();

    static void OnClose(object? sender, ConsoleCancelEventArgs x)
        => _OnClose();

    static void _OnClose()
    {
        Console.CursorVisible = true;
    }
}

class Args
{
    public bool OnlyTopDirectory = false;
    public int SearchDepth = int.MinValue;
    public bool ExcludeBinaryFiles = false;
    public bool GetStringsFromBinaryFiles = false;
}