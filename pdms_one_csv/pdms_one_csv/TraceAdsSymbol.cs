using System;
using System.IO;
using System.Text;
using TwinCAT.Ads;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace pdms_one_csv
{

    class TraceAdsSymbol
    {
        /// <summary>instance of the ads client</summary>
        private AdsClient _adsclient = new AdsClient();
        /// <summary>ams netid</summary>
        private string _netId;
        /// <summary>ams port</summary>
        private int _port;
        /// <summary>instance of the ads symbol</summary>
        private List<AdsSymbol> _adssymbols = new List<AdsSymbol>();
        /// <summary>list of sample</summary>
        private ConcurrentQueue<Sample> _samples = new ConcurrentQueue<Sample>();

        /// <summary>list of sample</summary>
        private List<Sample> _listsamples1 = new List<Sample>();
        /// <summary>list of sample</summary>
        private List<Sample> _listsamples2 = new List<Sample>();

        /// <summary>sample</summary>
        private Sample _sample = new Sample();

        /// <summary>timer to stop the recording/trace</summary>
        private System.Timers.Timer _timer = new System.Timers.Timer();
        /// <summary>event/callback the the trace recording is over</summary>
        public event EventHandler Completed;
        /// <summary>stringbuilder to save samples as string</summary>
        private StringBuilder _stringbuilder = new StringBuilder();


        /// <summary>standard contructor</summary>
        public TraceAdsSymbol() { }
        /// <summary>constructor</summary>
        /// <param name="netId">ams netid</param>
        /// <param name="port"></param>
        /// <param name="adssymbol">instance of the ads symbol</param>
        public TraceAdsSymbol(string netId, int port, List<AdsSymbol> adssymbols)
        {
            this._netId = netId;
            this._port = port;
            this._port = port;
            this._adssymbols = adssymbols;
        }

        /// <summary>stringbuilder of the samples</summary>
        public StringBuilder stringbuilder
        {
            get { return this._stringbuilder; }
        }

        /// <summary>start the trace and collect the samples</summary>
        /// <param name="time">trace time, after this time the trace is stopped</param>
        public void Start(double time)
        {
            try
            {
                // clear stringbuilder
                this._stringbuilder.Clear();

                // clear samples
                this._samples.Clear();

                Console.WriteLine(DateTime.Now.ToString() + ":\t" + "trace started...");

                // connect ads client
                this._adsclient.Connect(_netId, _port);

                if (this._adsclient.IsConnected)
                {
                    Console.WriteLine(DateTime.Now.ToString() + ":\t" + "connected to: " + "netId: " + _netId + ", port: " + _port);

                    // write header of stringbuilder
                    foreach (AdsSymbol adssymbol in _adssymbols)
                    {
                        this._stringbuilder.AppendLine("AMS NetId: " + _netId);
                        this._stringbuilder.AppendLine("AMS Port: " + _port);
                        this._stringbuilder.AppendLine("Ads Variable: " + adssymbol.varname);
                        this._stringbuilder.AppendLine("Type Ads Variable: " + adssymbol.type.Name);
                        this._stringbuilder.AppendLine("Trace settings: " + "AdsTransMode: " + adssymbol.settings.NotificationMode + ", CycleTime: " + adssymbol.settings.CycleTime + ", MaxDelay: " + adssymbol.settings.MaxDelay);
                        this._stringbuilder.AppendLine();
                    }
                    this._stringbuilder.AppendLine("sample" + ";" + "value1" + ";" + "timestamp1" + ";" + "value2" + ";" + "timestamp2" + ";" + "timedifference");


                    // add ads notification
                    AddAdsNotifications();

                    foreach (AdsSymbol adssymbol in _adssymbols)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + ":\t" + "add variable to notification: " + adssymbol.varname);
                    }

                    // set timer to stop trace
                    SetTimer(time);

                    Console.WriteLine(DateTime.Now.ToString() + ":\t" + "start timer and collect samples");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(Start) + " " + ex.Message);
            }
        }

        /// <summary>set the timer to stop the trace</summary>
        /// <param name="time">trace time, after this time the timer fired an event</param>
        private void SetTimer(double time)
        {
            try
            {
                // set timer intervall
                this._timer.Interval = time;
                // settings to run the event only once
                this._timer.AutoReset = false;
                this._timer.Enabled = true;
                //register the event
                this._timer.Elapsed += TimeElapsedEvent;
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(SetTimer) + " " + ex.Message);
            }
        }

        /// <summary>add ads notifications and ads notification event</summary>
        private void AddAdsNotifications()
        {
            try
            {
                // add event function
                this._adsclient.AdsNotificationEx += adsclient_AdsNotification;
                // add notification handles
                foreach (AdsSymbol adssymbol in _adssymbols)
                {
                    adssymbol.handle = _adsclient.AddDeviceNotificationEx(adssymbol.varname, adssymbol.settings, null, adssymbol.type);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(AddAdsNotifications) + " " + ex.Message);
            }
        }

        /// <summary>ads notification, event is fired when a new notification is pending</summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">data of the event</param>
        private void adsclient_AdsNotification(object sender, AdsNotificationExEventArgs e)
        {
            try
            {   
                if (e.Handle == _adssymbols[0].handle)
                {
                    // add sample to concurrent queue
                    this._samples.Enqueue(new Sample(e.Value, e.Handle, e.TimeStamp));
                    // save sample into list
                    Parallel.Invoke(TransformToList);
                }
                if (e.Handle == _adssymbols[1].handle)
                {
                    // add sample to concurrent queue
                    this._samples.Enqueue(new Sample(e.Value, e.Handle, e.TimeStamp));
                    // save sample into list
                    Parallel.Invoke(TransformToList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(adsclient_AdsNotification) + " " + ex.Message);
            }
        }

        /// <summary>write samples into lists</summary>
        private void TransformToList()
        {
            try
            {
                while (_samples.TryDequeue(out _sample))
                {
                    if (_sample.handle == _adssymbols[0].handle)
                    {
                        //write samples into list
                        this._listsamples1.Add(_sample);
                    }

                    if (_sample.handle == _adssymbols[1].handle)
                    {
                        //write samples into list
                        this._listsamples2.Add(_sample);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(TransformToList) + " " + ex.Message);
            }
        }

        private void SamplestoString()
        {
            try
            {
                int i = 0;
                int j = 0;

                for (i = 0; i < (_listsamples1.Count - 1); i++)
                {
                    j++;
                    long timedifferenceticks = _listsamples2[i].timestamp.Ticks - _listsamples1[i].timestamp.Ticks;
                    double timedifferencedouble = timedifferenceticks * 0.0000001;
                    this._stringbuilder.AppendLine(j.ToString() + ";" + _listsamples1[i].value.ToString() + ";" + _listsamples1[i].timestamp.TimeOfDay.ToString() + ";" + _listsamples2[i].value.ToString() + ";" + _listsamples2[i].timestamp.TimeOfDay.ToString() + ";" + timedifferencedouble.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(SamplestoString) + " " + ex.Message);
            }
        }

        /// <summary>timer event is fired when the specified time has elapsed</summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="e">data of the event</param>
        private void TimeElapsedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + "collecting samples completed");

                // delete timer event function
                this._timer.Elapsed -= TimeElapsedEvent;

                // delete ads notification event function
                this._adsclient.AdsNotificationEx -= adsclient_AdsNotification;

                // delete ads notification handle
                foreach (AdsSymbol adssymbol in _adssymbols)
                {
                    this._adsclient.DeleteDeviceNotification(adssymbol.handle);
                }

                SamplestoString();

                // callback function/event to copy or evaluate the collected samples/data
                Completed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":\t" + nameof(TimeElapsedEvent) + " " + ex.Message);
            }
        }

        /// <summary>release resources</summary>
        public void Dispose()
        {
            // release resources
            this._adsclient.Dispose();

            // realease resources
            this._timer.Dispose();
        }
    }

    /// <summary>class adssymbol</summary>
    public class AdsSymbol
    {
        /// <summary>Full-Name of the ads variable</summary>
        public string varname;
        /// <summary>type of the ads variable</summary>
        public Type type;
        /// <summary>handle of the ads variable</summary>
        public uint handle;
        /// <summary>notification settings of the ads variable</summary>
        public NotificationSettings settings;

        /// <summary>standard contructor</summary>
        public AdsSymbol() { }
        /// <summary>constructor</summary>
        /// <param name="varname">Full-Name of the ads variable</param>
        /// <param name="type">type of the ads variable</param>
        /// <param name="handle">handle of the ads variable</param>
        /// <param name="settings">notification settings of the ads variable</param>
        public AdsSymbol(string varname, Type type, uint handle, NotificationSettings settings)
        {
            this.varname = varname;
            this.type = type;
            this.handle = handle;
            this.settings = settings;
        }

    }

    /// <summary>class sample</summary>
    public class Sample
    {
        /// <summary>value of the sample</summary>
        public object value;
        /// <summary>handle of the sample</summary>
        public uint handle;
        /// <summary>timestamp of the sample</summary>
        public DateTimeOffset timestamp;

        /// <summary>standard contructor</summary>
        public Sample() { }
        /// <summary>constructor</summary>
        /// <param name="value">value of the sample</param>
        /// <param name="timestamp">timestamp of the sample</param>
        public Sample(object value, uint handle, DateTimeOffset timestamp)
        {
            this.value = value;
            this.handle = handle;
            this.timestamp = timestamp;
        }
    }
}
