﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Threading;
using System.Text;
using System.Net;

using ZD.Common;
using ZDO.IpResolve;

namespace Site
{
    public class QueryLogger
    {
        #region Singleton management

        private static QueryLogger instance;

        public static QueryLogger Instance
        {
            get { return instance; }
        }

        public static void Init()
        {
            instance = new QueryLogger();
        }

        public static void Shutdown()
        {
            QueryLogger al = instance;
            instance = null;
            if (al != null) al.shutdown();
        }

        #endregion

        private const string fmtTime = "{0}-{1:00}-{2:00}!{3:00}:{4:00}:{5:00}";
        internal static string FormatTime(DateTime dt)
        {
            return string.Format(fmtTime, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        }

        private interface IAuditItem
        {
            string LogLine { get; }
        }

        private class QueryItem : IAuditItem
        {
            private readonly IpResolver ipr;
            private readonly DateTime time;
            private readonly string hostAddr;
            private readonly int resCount;
            private readonly int msecLookup;
            private readonly int msecTotal;
            private readonly SearchLang lang;
            private readonly string query;

            public QueryItem(IpResolver ipr, string hostAddr, int resCount, int msecLookup, int msecTotal,
                SearchLang lang, string query)
            {
                this.ipr = ipr;
                this.time = DateTime.UtcNow;
                this.hostAddr = hostAddr;
                this.resCount = resCount;
                this.msecLookup = msecLookup;
                this.msecTotal = msecTotal;
                this.lang = lang;
                this.query = query;
            }

            public string LogLine
            {
                get
                {
                    IPAddress addr = null;
                    if (!IPAddress.TryParse(hostAddr, out addr)) addr = null;
                    string country = addr == null ? ipr.GetNoCountry() : ipr.GetContryCode(addr);

                    StringBuilder sb = new StringBuilder();
                    sb.Append(QueryLogger.FormatTime(time));
                    sb.Append('\t');
                    sb.Append(country);
                    sb.Append('\t');
                    sb.Append(lang == SearchLang.Chinese ? "ZHO" : "TRG");
                    sb.Append('\t');
                    int sec = msecTotal / 1000;
                    int ms = msecTotal - sec * 1000;
                    sb.Append(string.Format("{0:00}.{1:000}", sec, ms));
                    sb.Append('\t');
                    sec = msecLookup / 1000;
                    ms = msecLookup - sec * 1000;
                    sb.Append(string.Format("{0:00}.{1:000}", sec, ms));
                    sb.Append('\t');
                    sb.Append(resCount.ToString());
                    sb.Append('\t');
                    sb.Append(query);

                    return sb.ToString();
                }
            }
        }

        private readonly IpResolver ipResolver;
        private readonly string logPath;
        private Thread thr;
        private AutoResetEvent evt = new AutoResetEvent(false);
        private readonly List<IAuditItem> ilist = new List<IAuditItem>();
        private bool closing = false;

        private QueryLogger()
        {
            ipResolver = new IpResolver();
            string logPath = HttpRuntime.AppDomainAppPath;
            logPath = Path.Combine(logPath, @"_data\logs");
            this.logPath = logPath;
            thr = new Thread(threadFun);
            thr.IsBackground = true;
            thr.Start();
        }

        private void shutdown()
        {
            closing = true;
            evt.Set();
            thr.Join(2000);
        }

        private void threadFun(object ctxt)
        {
            List<IAuditItem> myList = new List<IAuditItem>();
            while (!closing)
            {
                evt.WaitOne(1000);
                lock (ilist)
                {
                    myList.Clear();
                    myList.AddRange(ilist);
                    ilist.Clear();
                }
                if (myList.Count == 0) continue;
                using (StreamWriter swQueryLog = new StreamWriter(Path.Combine(logPath, "queries.txt"), true))
                {
                    foreach (IAuditItem itm in myList)
                    {
                        if (itm is QueryItem) swQueryLog.WriteLine(itm.LogLine);
                    }
                    swQueryLog.Flush();
                }
            }
        }

        public void LogQuery(string hostAddr, int resCount, int msecLookup, int msecTotal, SearchLang lang, string query)
        {
            QueryItem itm = new QueryItem(ipResolver, hostAddr, resCount,
                msecLookup, msecTotal, lang, query);
            lock (ilist)
            {
                ilist.Add(itm);
                evt.Set();
            }
        }
    }
}