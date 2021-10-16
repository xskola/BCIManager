using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Logger : MonoBehaviour
{
    /*
     * Logger
     * 
     * This class creates a CSV-file style log
     * 
     * By default, it logs date/time, application run time, and whatever you want to put into the log.
     * 
     * Coroutine WriteLog() is started at the beginning of the application run and writes the log to disk
     * every LOG_FILE_SAVING_INTERVAL seconds (default = 2), preventing data loss in case of unexpected exit.
     *
     * LOG_FILE_SEPARATOR (default = ;) sets the separation character of output CSV files.
     * 
     * output:      file named "log-YEAR-MONTH-DAY-HOUR-MINUTE-SECOND.csv"
     * interface:   Logger.LogEvent("message to log")
     * 
    */

    int LOG_FILE_SAVING_INTERVAL = 2;
    static string LOG_FILE_SEPARATOR = ";";
    static bool SHOW_LOG_IN_UNITY_DEBUG = false;

    string logFileName = @"log-";
    public static List<String> log = new List<string>();
    string[] logCopy;
    public static double startTime;
    public static double runTime;


    void Start()
    {
        string postfix = System.DateTime.Now.ToString("yy-MM-dd-HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        startTime = (double)(DateTime.Now.Ticks) / 10000000f;
        logFileName += postfix + ".csv";

        StartCoroutine(WriteLog());
    }

    void Update()
    {
        runTime = (((double)(DateTime.Now.Ticks) / 10000000f) - startTime);
    }
	
	public static void LogHeader(string additional = "")
	{
		string header = "datetime" + LOG_FILE_SEPARATOR + "timestamp" + LOG_FILE_SEPARATOR + "event";
		if(additional != "")
			header += LOG_FILE_SEPARATOR + additional;
		log.Add(header);
	}	

    IEnumerator WriteLog()
    {
        yield return new WaitForSeconds(1);
        while (true)
        {
            if (log.Count > 0)
            {
                logCopy = new string[log.Count];
                log.CopyTo(logCopy);
                log.Clear();

                using (System.IO.StreamWriter writetext = new System.IO.StreamWriter(logFileName, true))
                {
                    writetext.AutoFlush = true;
                    foreach (string line in logCopy)
                        writetext.WriteLine(line);
                    writetext.Close();
                }
            }

            yield return new WaitForSeconds(LOG_FILE_SAVING_INTERVAL);
        }
    }

    public static void LogEvent(string desc)
    {
        DateTime now = DateTime.Now;
        string datetime = now.ToString("u", DateTimeFormatInfo.InvariantInfo);
        string logline = datetime + LOG_FILE_SEPARATOR;
        if (runTime < 1)
            logline += "0";
        logline += runTime.ToString(
            "################.################",
            CultureInfo.CreateSpecificCulture("en-GB"))
            + LOG_FILE_SEPARATOR + desc;
        log.Add(logline);

        if (SHOW_LOG_IN_UNITY_DEBUG)
            Debug.Log("Logger: LogEvent '" + desc + "' at " + datetime);
    }
}
