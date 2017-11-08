using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Resources;
using Sitecore.Shell.Framework.Jobs;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.XamlSharp.Xaml;
using System;
using System.Collections.Specialized;
using System.Globalization;

namespace Sitecore.Shell.Applications.Dialogs.Progress
{
    /// <summary>
    /// Progress dialog page.
    /// </summary>
    public class ProgressPage : XamlMainControl
    {
        /// <summary></summary>
        protected Button Close;

        /// <summary></summary>
        protected Literal HeaderText;

        /// <summary></summary>
        protected Image HeaderSpacer;

        /// <summary></summary>
        protected ThemedImage Icon;

        /// <summary></summary>
        protected Literal Log;

        /// <summary></summary>
        protected Literal MoreInformation;

        /// <summary></summary>
        protected Border MoreInformationContainer;

        /// <summary></summary>
        protected ThemedImage MoreImage;

        /// <summary></summary>
        protected Border Progress;

        /// <summary></summary>
        protected Image ProgressSpacer;

        /// <summary></summary>
        protected Image FooterSpacer;

        /// <summary></summary>
        protected Literal Subtitle;

        /// <summary></summary>
        protected Literal Title;

        /// <summary></summary>
        protected ThemedImage TitleIcon;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Sitecore.Shell.Applications.Dialogs.Progress.ProgressPage" /> is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        protected bool Expanded
        {
            get
            {
                return MainUtil.GetBool(this.ViewState["Expanded"], false);
            }
            set
            {
                this.ViewState["Expanded"] = (value ? "1" : "0");
            }
        }

        /// <summary>
        /// Gets or sets the handle.
        /// </summary>
        /// <value>The handle.</value>
        protected Handle Handle
        {
            get
            {
                return Handle.Parse(StringUtil.GetString(this.ViewState["Handle"]));
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                this.ViewState["Handle"] = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the last index of the updated message.
        /// </summary>
        /// <value>The last index of the updated message.</value>
        protected int LastUpdatedMessageIndex
        {
            get
            {
                return MainUtil.GetInt(this.ViewState["LastUpdatedMessageIndex"], 0);
            }
            set
            {
                this.ViewState["LastUpdatedMessageIndex"] = value;
            }
        }

        /// <summary>
        /// Checks the status.
        /// </summary>
        protected void CheckStatus()
        {
            Job job = JobManager.GetJob(this.Handle);
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
            string factor = ((double)((float)job.Status.Processed / (float)job.Status.Total)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            this.UpdateFactor(factor);
            this.UpdateStatus(job);
            SheerResponse.Timer("CheckStatus", 500);
        }

        /// <summary>
        /// Handles the Close_ click event.
        /// </summary>
        protected void Close_Click()
        {
            Job job = this.GetJob();
            if (job != null)
            {
                job.Status.Expiry = System.DateTime.Now.AddMinutes(1.0);
            }
            SheerResponse.SetDialogValue("Manual close");
            SheerResponse.CloseWindow();
        }

        /// <summary>
        /// Handles the More information_ click event.
        /// </summary>
        protected void ToggleInformation()
        {
            Job job = this.GetJob();
            Assert.IsNotNull(job, "job");
            if (job.Status.Failed)
            {
                this.ShowException(job);
                return;
            }
            this.MoreInformation.Text = (this.Expanded ? Translate.Text("View all messages") : Translate.Text("Hide messages"));
            this.Expanded = !this.Expanded;
            SheerResponse.Eval("toggle()");
        }

        /// <summary>
        /// Shows the exception.
        /// </summary>
        /// <param name="job">The job.</param>
        private void ShowException(Job job)
        {
            string text = "An error occured.";
            if (job.Status.Messages.Count > 0)
            {
                bool flag;
                text = this.GetLastJobErrorMessage(job, out flag);
            }
            text = "<h2> <i>An error occured.</i></h2><br />" + text;
            SheerResponse.SetAttribute("ErrorMessage", "value", text);
            SheerResponse.Eval("showException()");
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load"></see> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"></see> object that contains the event data.</param>
        protected override void OnLoad(System.EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (XamlControl.AjaxScriptManager.IsEvent)
            {
                return;
            }
            this.MoreInformation.Text = Translate.Text("View all messages");
            LongRunningOptions longRunningOptions = LongRunningOptions.Parse();
            this.Handle = Handle.Parse(longRunningOptions.Handle);
            this.HeaderText.Text = longRunningOptions.Title;
            if (!string.IsNullOrEmpty(longRunningOptions.Icon))
            {
                this.Icon.Src = longRunningOptions.Icon;
            }
            Job job = JobManager.GetJob(this.Handle);
            Assert.IsNotNull(job, "job");
        }

        /// <summary>
        /// Gets the job.
        /// </summary>
        /// <returns>The job.</returns>
        private Job GetJob()
        {
            return JobManager.GetJob(this.Handle);
        }

        /// <summary>
        /// Called when this instance has finish.
        /// </summary>
        /// <param name="job">The job.</param>
        private void UpdateFinished(Job job)
        {
            Assert.ArgumentNotNull(job, "job");
            if (job.Status.Failed)
            {
                if ((System.DateTime.Now - job.Status.Expiry).TotalMinutes < 30.0)
                {
                    job.Status.Expiry = System.DateTime.Now.AddMinutes(30.0);
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

        /// <summary>
        /// Updates the factor.
        /// </summary>
        /// <param name="factor">The factor.</param>
        private void UpdateFactor(string factor)
        {
            Assert.ArgumentNotNullOrEmpty(factor, "factor");
            SheerResponse.Eval(new ScriptInvokationBuilder("progressTo").AddString(factor, new object[0]).ToString());
        }

        /// <summary>
        /// Updates the specified status text.
        /// </summary>
        /// <param name="job">The job.</param>
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
            if (!this.Expanded)
            {
                return;
            }
            string[] array;
            lock (messages)
            {
                array = new string[messages.Count - this.LastUpdatedMessageIndex];
                for (int i = this.LastUpdatedMessageIndex; i < messages.Count; i++)
                {
                    array[i - this.LastUpdatedMessageIndex] = messages[i];
                }
            }
            this.LastUpdatedMessageIndex = messages.Count;
            SheerResponse.Eval(new ScriptInvokationBuilder("appendLog").AddString(StringUtil.Join(array, "<br />") + "<br />", new object[0]).ToString());
        }

        /// <summary>
        /// Clips the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>The clip.</returns>
        private string Clip(string text, int limit)
        {
            Assert.IsNotNull(text, "text");
            if (text.Length > limit)
            {
                text = text.Substring(0, limit) + "...";
            }
            return text;
        }

        /// <summary>
        /// Returns the last error message for the job.
        /// </summary>
        /// <param name="job">Job to return error message for.</param>
        /// <param name="bIsExceptionMsg">True, if the last error message is an exception message.</param>
        /// <returns>A last error message. If no error message found, the method returns the last message.</returns>
        /// <remarks>The method traverses job messages from the last to the first looking for "Error" and "Exception"
        /// translated strings at the beginning of each message. The return message is stripped off "Error" and "Exception" prefix.
        /// If no error or exception message is found, the method returns the last message.
        /// <paramref name="bIsExceptionMsg" /></remarks> indicates, whether returned message was logged as a result of an exception.
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
                bIsExceptionMsg = text.StartsWith(array[0], System.StringComparison.OrdinalIgnoreCase);
                if (bIsExceptionMsg)
                {
                    return StringUtil.RemovePrefix(array[0], text);
                }
                if (text.StartsWith(array[1], System.StringComparison.OrdinalIgnoreCase))
                {
                    return StringUtil.RemovePrefix(array[1], text);
                }
            }
            return job.Status.Messages[job.Status.Messages.Count - 1];
        }
    }
}