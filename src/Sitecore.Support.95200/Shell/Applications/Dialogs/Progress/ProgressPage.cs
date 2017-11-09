using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Resources;
using Sitecore.Web.UI;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Sitecore.Support.Shell.Applications.Dialogs.Progress
{
    public class ProgressPage : Sitecore.Shell.Applications.Dialogs.Progress.ProgressPage
    {
        protected new void CheckStatus()
        {
            Job job = JobManager.GetJob(base.Handle);
            Assert.IsNotNull(job, "job in checkstatus");
            if (job.Status.State == JobState.Finished)
            {
                this.UpdateFinished(job);
                return;
            }
            if (job.Status.Total <= 0L)
            {
                SheerResponse.Timer("CheckStatus", 1000);
                this.UpdateStatus(job);
                return;
            }
            string factor = ((double)((float)job.Status.Processed / (float)job.Status.Total)).ToString("0.00", CultureInfo.InvariantCulture);
            this.UpdateFactor(factor);
            this.UpdateStatus(job);
            SheerResponse.Timer("CheckStatus", 500);
        }

        private void UpdateStatus(Job job)
        {
            Assert.ArgumentNotNull(job, "job");
            string text = string.Empty;
            StringCollection messages = job.Status.Messages;
            if (messages.Count > 0)
            {
                text = messages[messages.Count - 1];
            }
            this.Title.Text = text;
            SheerResponse.SetAttribute("Title", "title", text);
            if (base.Expanded)
            {
                int count = messages.Count;
                StringCollection obj = messages;
                string[] array;
                lock (obj)
                {
                    array = new string[count - base.LastUpdatedMessageIndex];
                    for (int i = base.LastUpdatedMessageIndex; i < count; i++)
                    {
                        array[i - base.LastUpdatedMessageIndex] = messages[i];
                    }
                }
                base.LastUpdatedMessageIndex = count;
                SheerResponse.Eval(new ScriptInvokationBuilder("appendLog").AddString(array.Aggregate(string.Empty, (string s, string s1) => s + s1 + "<br />"), new object[0]).ToString());
            }
        }

        private void UpdateFinished(Job job)
        {
            Assert.ArgumentNotNull(job, "job");
            if (job.Status.Failed)
            {
                if ((DateTime.Now - job.Status.Expiry).TotalMinutes < 30.0)
                {
                    job.Status.Expiry = DateTime.Now.AddMinutes(30.0);
                }
                this.HeaderSpacer.Class = "error";
                this.Progress.Visible = false;
                this.ProgressSpacer.Visible = false;
                this.FooterSpacer.Visible = false;
                this.Title.Text = "An error occured.";
                this.Title.Class = "error";
                this.TitleIcon.Visible = true;
                this.MoreImage.Src = Images.GetThemedImageSource("Applications/16x16/document_pinned.png", ImageDimension.id16x16);
                this.MoreImage.Class = "error";
                bool flag;
                string lastJobErrorMessage = this.GetLastJobErrorMessage(job, out flag);
                this.Subtitle.Text = StringUtil.Clip(lastJobErrorMessage, 120, true);
                this.Subtitle.Visible = true;
                this.MoreInformationContainer.Visible = flag;
                if (flag)
                {
                    this.MoreInformation.Text = "View error";
                }
                this.Close.Visible = true;
                return;
            }
            SheerResponse.SetDialogValue("Finished");
            SheerResponse.CloseWindow();
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

        private void UpdateFactor(string factor)
        {
            Assert.ArgumentNotNullOrEmpty(factor, "factor");
            SheerResponse.Eval(new ScriptInvokationBuilder("progressTo").AddString(factor, new object[0]).ToString());
        }

        private string GetLastJobErrorMessage(Job job, out bool bIsExceptionMsg)
        {
            Assert.ArgumentNotNull(job, "job");
            bIsExceptionMsg = false;
            if (job.Status.Messages.Count == 0)
            {
                return "An error occured.";
            }
            string[] array = new string[]
            {
                Translate.Text("#Exception: "),
                Translate.Text("#Error: ")
            };
            for (int i = job.Status.Messages.Count - 1; i >= 0; i--)
            {
                string text = job.Status.Messages[i];
                bIsExceptionMsg = text.StartsWith(array[0], StringComparison.OrdinalIgnoreCase);
                if (bIsExceptionMsg)
                {
                    return StringUtil.RemovePrefix(array[0], text);
                }
                if (text.StartsWith(array[1], StringComparison.OrdinalIgnoreCase))
                {
                    return StringUtil.RemovePrefix(array[1], text);
                }
            }
            return job.Status.Messages[job.Status.Messages.Count - 1];
        }
    }
}