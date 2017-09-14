namespace Sitecore.Support.Shell.Applications.Dialogs.Progress
{
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Jobs;
    using Sitecore.Resources;
    using Sitecore.Web.UI;
    using Sitecore.Web.UI.Sheer;
    using System.Collections.Specialized;

    public class ProgressPage : Sitecore.Shell.Applications.Dialogs.Progress.ProgressPage
    {
        protected new void CheckStatus()
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
            //sitecore.support.95200. here we implement count instead of messages.Count not to get messages.Count value increased during the FOR loop for the trees with big number of subitems
            int count = messages.Count;
            lock (messages)
            {
                array = new string[count - this.LastUpdatedMessageIndex];
                for (int i = this.LastUpdatedMessageIndex; i < count; i++)
                {
                    array[i - this.LastUpdatedMessageIndex] = messages[i];
                }
            }
            this.LastUpdatedMessageIndex = count;
            SheerResponse.Eval(new ScriptInvokationBuilder("appendLog").AddString(StringUtil.Join(array, "<br />") + "<br />", new object[0]).ToString());
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
        /// Updates the factor.
        /// </summary>
        /// <param name="factor">The factor.</param>
        private void UpdateFactor(string factor)
        {
            Assert.ArgumentNotNullOrEmpty(factor, "factor");
            SheerResponse.Eval(new ScriptInvokationBuilder("progressTo").AddString(factor, new object[0]).ToString());
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
