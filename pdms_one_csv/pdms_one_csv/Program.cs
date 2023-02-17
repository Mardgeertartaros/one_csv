using System;
using System.IO;
using TwinCAT.Ads;
using System.Text;
using System.Collections.Generic;

namespace pdms_one_csv
{

    class Program
    {
        // ams netid
        static string netId = "169.254.24.40.1.1";

        // ams port
        static int port = (int)AmsPort.PlcRuntime_851;

        // desktop path of the .csv file to save the samples
        static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop).ToString(), "test.csv");


        // ads symbol symbols for which the samples are collected
        static AdsSymbol adssymbol1 = new AdsSymbol("GVL_Datalogger.St_30_VAR.fActPos", typeof(double), 0, new NotificationSettings(AdsTransMode.OnChange, 0, 0));
        static AdsSymbol adssymbol2 = new AdsSymbol("GVL_Datalogger.St_30_VAR.fCurrentScaled", typeof(double), 0, new NotificationSettings(AdsTransMode.OnChange, 0, 0));

        // 
        static List<AdsSymbol> adssymbols = new List<AdsSymbol>();


        // object to collect data and start a measurement using a ads symbol
        static TraceAdsSymbol trace = null;// new TraceAdsSymbol(netId, port, adssymbol1, adssymbol2);
        //trace = new TraceAdsSymbol(netId, port, adssymbol, adssymbol2);

        static AdsClient adsclient = new AdsClient();

        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            adssymbols.Add(adssymbol1);
            adssymbols.Add(adssymbol2);

            trace = new TraceAdsSymbol(netId, port, adssymbols);

            // start trace measurement for 5 seconds ( 5000 )
            trace.Start(5000);

            // event to recognized that the measurement is finished
            trace.Completed += tracecompleted;

            adsclient.Connect(netId, 10000);

            // keep consol open
            Console.ReadKey();
        }

        /// <summary>event is fired by the trace object when the specified time has elapsed</summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">data of the event</param>
        static void tracecompleted(object sender, EventArgs e)
        {
            // stringbuilder to copy collected data
            StringBuilder stringbuilder = new StringBuilder();

            // copy stringbuilder
            stringbuilder = trace.stringbuilder;

            // save stringbuilder as string in csv-file on the desktop
            File.WriteAllText(path, stringbuilder.ToString());

            //Dispose 
            trace.Dispose();

            // close console
            Environment.Exit(0);
        }
    }
}
