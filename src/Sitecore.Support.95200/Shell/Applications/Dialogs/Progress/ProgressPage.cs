using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Reflection;
using Sitecore.Web.UI;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Shell.Applications.Dialogs.Progress
{
    public class ProgressPage : Sitecore.Shell.Applications.Dialogs.Progress.ProgressPage
    {
        // Methods
        protected void CheckStatus()
        {
            Job job = JobManager.GetJob(base.Handle);
            Assert.IsNotNull(job, "job in checkstatus");
            if (job.Status.State == JobState.Finished)
            {
                ReflectionUtil.CallMethod(typeof(ProgressPage), this, "UpdateFinished", true, true, new object[] { job });
            }
            else if (job.Status.Total <= 0L)
            {
                SheerResponse.Timer("CheckStatus", 0x3e8);
                this.UpdateStatus(job);
            }
            else
            {
                string str = (((float)job.Status.Processed) / ((float)job.Status.Total)).ToString("0.00", CultureInfo.InvariantCulture);
                ReflectionUtil.CallMethod(typeof(ProgressPage), this, "UpdateFactor", true, true, new object[] { str });
                this.UpdateStatus(job);
                SheerResponse.Timer("CheckStatus", 500);
            }
        }

        private string Clip(string text, int limit)
        {
            Assert.IsNotNull(text, "text");
            if (text.Length > limit)
            {
                text = text.Substring(0, limit) + "...";
            }
            return text;
        }

        private void UpdateStatus(Job job)
        {
            Assert.ArgumentNotNull(job, "job");
            string str = string.Empty;
            StringCollection messages = job.Status.Messages;
            if (messages.Count > 0)
            {
                str = messages[messages.Count - 1];
            }
            base.Title.Text = str;
            SheerResponse.SetAttribute("Title", "title", str);
            if (base.Expanded)
            {
                string[] strArray;
                int count = messages.Count;
                lock (messages)
                {
                    strArray = new string[count - base.LastUpdatedMessageIndex];
                    for (int i = base.LastUpdatedMessageIndex; i < count; i++)
                    {
                        strArray[i - base.LastUpdatedMessageIndex] = messages[i];
                    }
                }
                base.LastUpdatedMessageIndex = count;
                SheerResponse.Eval(new ScriptInvokationBuilder("appendLog").AddString(strArray.Aggregate<string, string>(string.Empty, (s, s1) => s + s1 + "<br />"), new object[0]).ToString());
            }
        }
    }


}