using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace Updater
{
    public partial class frmMain : Form
    {
        const string app_name = "WizAdmin";
        const string app_exe = "WizAdmin.exe";

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Text = app_name + " Updater v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //Connect to server======================
                backgroundWorker.ReportProgress(0, "Connecting...");

                //Get current infos
                SystemInfos current_info = JsonConvert.DeserializeObject<SystemInfos>(File.ReadAllText(@"system.lmt"));

                //Get latest infos
                SystemInfos latest_info = JsonConvert.DeserializeObject<SystemInfos>(File.ReadAllText(Path.Combine(current_info.server_url, @"system.lmt")));

                //Compare versions======================
                backgroundWorker.ReportProgress(0, "Comparing version...");
                Version current_v = new Version(current_info.version);
                Version latest_v = new Version(latest_info.version);
                if (current_v.CompareTo(latest_v) >= 0)
                {
                    MessageBox.Show("Nothing to update, you are using the latest version!", "Information");
                    return;
                }

                //Estimate time======================
                backgroundWorker.ReportProgress(0, "Estimating time...");

                //Get Job list
                Jobs jobs = JsonConvert.DeserializeObject<Jobs>(File.ReadAllText(Path.Combine(current_info.server_url, @"jobs.lmt")));

                //delegate for progress bar
                pgrUpdating.Invoke((MethodInvoker)delegate
                {
                    pgrUpdating.Maximum = jobs.copy.Count + jobs.delete.Count;
                });

                //Update======================
                backgroundWorker.ReportProgress(0, "Updating...");

                //Do Copy jobs
                int progress = 0;
                foreach (string cp in jobs.copy)
                {
                    backgroundWorker.ReportProgress(progress, "Updating: " + cp);
                    File.Copy(Path.Combine(current_info.server_url, cp), cp, true);
                    progress++;
                }

                //Do Delete jobs
                foreach (string dl in jobs.delete)
                {
                    backgroundWorker.ReportProgress(progress, "Updating: " + dl);
                    File.Delete(dl);
                    progress++;
                }
                backgroundWorker.ReportProgress(progress, "Finished");

                MessageBox.Show("Updated to latest version " + latest_info.version + " successfully !\n\n--- CHANGE LOGS ---\n" + latest_info.change_log, "Information");
            }
            catch
            {
                MessageBox.Show("Something wrongs, please try again later !", "Error");
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pgrUpdating.Value = e.ProgressPercentage;
            lblInfos.Text = e.UserState.ToString();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                if (MessageBox.Show("Are you sure you want to close ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Run main application
            Process.Start(app_exe);
            Application.Exit();
        }
    }

    public class Jobs
    {
        public List<string> copy { get; set; }
        public List<string> delete { get; set; }
    }

    public class SystemInfos
    {
        public string version { get; set; }
        public string server_url { get; set; }
        public string change_log { get; set; }
    }
}
