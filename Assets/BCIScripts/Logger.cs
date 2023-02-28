using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Logger
{
    /*
     * Logger
     * 
     * This class creates a CSV-file style log
     * By default, it logs date/time, application run time, and whatever you want to put into the log.
     * 
     * This is a static class, not inheriting from MonoBehavior. To use a single log, use NewLog() for registering it
     * (pass "this" as the instance parameter) and then just refer to Logger.Add() to add to it. To use more than one
     * log, use return value of type Log from NewLog to refer to it (i.e., Log.Add() for adding entries).
     * 
     * The "header" parameter of the constructor specifies the number of columns of the output CSV file and each call
     * of the Add() function must be with corresponding number of values.
     * 
     * Parameters:
     * logDirInAppData: if true, logs are saved to %APPDATA% on Windows and to app data folder on Android
     * logSaveInterval: how often (in seconds) are logs flushed to the storage
     * saveLogDirectory: name of the logs output directory
     * 
     */

    private static bool logDirInAppData = false;
    private static int logSaveInterval = 2;
    private static string saveLogDirectory = "ExpLogs";

    /* *************************************************************************************************************** */

    public static string DirPath { get; private set; } = "";
    public static string FileId { get; private set; } = "";
    public static Log? MainLog { get; private set; }

    public class Log
    {
        public string Filename { get; set; }
        public bool PrintLogsToDebugLog { get; set; } = true;
        public bool IsEmpty() => lines.Count <= 1;
        public int LineCount => lines.Count;
        private string name;
        private List<string> lines;
        private string[] keys;
        private string header;

        public Log(string name, string[] header)
        {
            lines = new();
            this.name = name;
            this.Filename = name + ".csv";
            keys = header;
            this.header = "datetime,runtime," + string.Join(",", header);
            lines.Add(this.header);
        }

        public void Add(params string[] values)
        {
            if (values.Length != keys.Length)
            {
                throw new ArgumentOutOfRangeException("values",
                    $"Number of parameters ({values.Length}) does not match number of expected parameters ({keys.Length}).\n" +
                    $"Constructor header: {string.Join(", ", keys)} \n" +
                    $"Passed header data: {string.Join(", ", values)}"
                    );
            }
            string runtime = Time.realtimeSinceStartup.ToString("################.###");
            string datetime = DateTime.Now.ToString("u", System.Globalization.DateTimeFormatInfo.InvariantInfo);
            string logline = $"{datetime},{runtime}";
            foreach (var value in values)
            {
                var formattedValue = double.TryParse(value, out double doubleValue) ? doubleValue.ToString("################.###") : value;
                logline += "," + formattedValue.Replace(",", ";");
            }
            lines.Add(logline);

            if (PrintLogsToDebugLog)
                Debug.Log(logline);
        }

        public void Pull(out string[] dest)
        {
            dest = new string[LineCount];
            lines.CopyTo(dest);
            lines.Clear();
        }
    }

    public static void Add(params string[] values)
    {
        if (MainLog != null)
        {
            MainLog.Add(values);
        }
        else
        {
            throw new ApplicationException("Main log has not been registered yet.");
        }
    }
    public static void Add(string value) => Add(new string[] { value });

    static IEnumerator DumpLog(Log log)
    {
        string filePath;

        yield return new WaitForSeconds(1);
        while (DirPath == "" || FileId == "")
            yield return new WaitForSeconds(1);

        filePath = Path.Combine(DirPath, FileId + log.Filename);
        while (true)
        {
            if (log.IsEmpty())
            {
                yield return new WaitForSeconds(logSaveInterval);
                continue;
            }

            log.Pull(out string[] logCopy);
            using (System.IO.StreamWriter writetext = new System.IO.StreamWriter(filePath, true))
            {
                writetext.AutoFlush = true;
                foreach (string line in logCopy)
                    writetext.WriteLine(line);
                writetext.Close();
            }
            yield return new WaitForSeconds(logSaveInterval);
        }
    }

    public static Log NewLog(MonoBehaviour instance, string name, string[] keys, bool main)
    {
        if (FileId == "" || DirPath == "")
            SetUpPaths();
        Log log = new Log(name, keys);
        instance.StartCoroutine(DumpLog(log));
        if (main)
            MainLog = log;
        return log;
    }

    static void SetUpPaths()
    {
        FileId = System.DateTime.Now.ToString("yyMMdd-HHmmss-", System.Globalization.CultureInfo.InvariantCulture);
        if (logDirInAppData)
        {
            DirPath = Path.Combine(Application.persistentDataPath, saveLogDirectory);
            Debug.Log("Logger output directory " + DirPath);
        }
        else
            DirPath = saveLogDirectory;
        Directory.CreateDirectory(DirPath);
    }

    public static string ToStr(float n) => n.ToString("################.###");
    public static string ToStr(double n) => n.ToString("################.###");

}
