using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace Download_Free_MS_eBooks
{
    public partial class MainForm : Form
    {
        private List<Download> urls;
        private List<Download> downloads;
        private WebClient wClient;
        private string downloadLocation;
        private bool logYet = false;

        public MainForm()
        {
            InitializeComponent();

            // URI to string for column sorting
            this.olvColumn1.AspectToStringConverter = delegate(object x) {
                Uri url = (Uri)x;
                return url.ToString();
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Load up a WebClient at launch since downloading is our primary function here
            wClient = new WebClient();

            // Call our function down there when we've finished downloading the list of books
            wClient.DownloadStringCompleted += WClient_DownloadStringCompleted;

            // Have to wrap all our downloads in try/catch blocks to avoid errors
            try
            {
                // Download list of books
                wClient.DownloadStringAsync(new Uri("https://drive.google.com/uc?export=download&id=0B-ULAF28Y63jY00wZEtWSmRTUWM"));
            }
            catch (WebException)
            {
                // Darn! We got a WebException error. What should we do about it?
                MessageBox.Show("File list URL is down. Look for update from developer of this software.");

                // TODO: List your email or something, it's looking bleak here man!
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Lock the download button
            // TODO: Add pause/resume functionality
            button1.Enabled = false;
            button1.Text = "Downloading";

            // Return if they don't select a location
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) { return; }

            // Place selected download location in a global variable to persist through different functions
            downloadLocation = folderBrowserDialog1.SelectedPath;

            // Create our download list
            downloads = new List<Download>();

            // Get checkmarked items from our listview and queue them up in the download listy
            foreach (Download details in objectListView1.Objects)
            {
                if (details.Active)
                {
                    downloads.Add(details);
                }
            }

            // Set up some progress notifications
            toolStripProgressBar1.Maximum = downloads.Count;
            toolStripStatusLabel1.Text = "Downloading " + downloads[0].Name;

            // Call our function down there when the file has finished downloading
            wClient.DownloadFileCompleted += WClient_DownloadFileCompleted;

            // Have to wrap all our downloads in try/catch blocks to avoid errors
            try
            {
                // Download book using a different thread
                wClient.DownloadFileAsync(downloads[0].URL, downloadLocation + "\\" + downloads[0].Name);
            }
            catch (WebException)
            {
                // Darn! We got a WebException error. What should we do about it?
                toolStripStatusLabel1.Text = "Error downloading: " + downloads[0].Name;

                // Let's at least log which file it was
                LogLostFile(downloads[0].URL.ToString());

                // TODO: Uncheck all items that got successfully downloaded
                //       Leaving all the failed downloads still checked to try again or export as a list

                // TODO: Add retrying
            }

            // Remove the file we just started downloading from the queue
            downloads.Remove(downloads[0]);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cancel any background task
            wClient.CancelAsync();
        }

        #region Downloader events

        private void WClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            // Check if the user has canceled the downloads, if so return
            if (e.Cancelled) { return; }

            // Split the downloads by new line
            string[] lines = e.Result.Split(new string[] { "\r\n", "\t\t" }, StringSplitOptions.None);

            // Create a url download list
            urls = new List<Download>();

            // Go through each line of book downloads list
            foreach (string line in lines)
            {
                // Filter out empty lines and the unneccesarry line "MSFT eBooks"
                if (!String.IsNullOrEmpty(line) && line.Trim() != "MSFT eBooks")
                {
                    // Split the line by commas
                    string[] parts = line.Split(',');

                    // Have to wrap this because we are testing for valid URLs as well
                    try
                    {
                        // Add a new Uri to our download list (the type cast causes the URI validation)
                        urls.Add(new Download(new Uri(parts[0].Trim()), parts[1].Trim()));
                    }
                    catch (UriFormatException)
                    {
                        // Oh noes! We got a UriFormatException. What should we do?
                        toolStripStatusLabel1.Text = "Invalid URL: " + parts[0];

                        // Let's log that nasty erraneous string (well.. hopefully anyways)
                        LogLostFile(parts[0]);
                    }
                }
            }

            // Add the list of downloads to our list view
            objectListView1.SetObjects(urls);
        }

        private void WClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            // Check if the user has canceled the downloads, if so return
            if (e.Cancelled) { return; }

            // Check for an error while downloading (since it happened on another thread)
            if (e.Error != null)
            {
                toolStripStatusLabel1.Text = "Error downloading: " + downloads[0].Name;
                LogLostFile(downloads[0].URL.ToString());
            }

            // Progress notification woot!
            toolStripProgressBar1.PerformStep();

            // Do we have any more downloads in the queue?
            if (downloads.Count > 0)
            {
                // Progress notification woot!
                toolStripStatusLabel1.Text = "Downloading " + downloads[0].Name;

                // Have to wrap all our downloads in try/catch blocks to avoid errors
                try
                {
                    // Download book using a different thread (that mysterious thread)
                    wClient.DownloadFileAsync(downloads[0].URL, downloadLocation + "\\" + downloads[0].Name);
                }
                catch (WebException)
                {
                    // Darn! We got a WebException error. What should we do about it?
                    toolStripStatusLabel1.Text = "Error downloading: " + downloads[0].Name;

                    // Let's at least log which file it was
                    LogLostFile(downloads[0].URL.ToString());

                    // TODO: Uncheck all items that got successfully downloaded
                    //       Leaving all the failed downloads still checked to try again or export as a list

                    // TODO: Add retrying
                }

                // Remove the file we just started downloading from the queue
                downloads.Remove(downloads[0]);
            }
            else
            {
                button1.Text = "Download";
                //
                button1.Enabled = true;
                //
                toolStripStatusLabel1.Text = "Finished";
                //
                MessageBox.Show("Finished downloading");
            }
        }

        #endregion

        // Simple logging function
        private void LogLostFile(string report)
        {
            // Simple header to differentiate between runs
            if (!logYet)
            {
                File.AppendAllText(Application.StartupPath + "\\errors.log", "===========================================================" + Environment.NewLine);
                File.AppendAllText(Application.StartupPath + "\\errors.log", " ======== Download Free MS eBooks v" + Application.ProductVersion + " ========" + Environment.NewLine);
                File.AppendAllText(Application.StartupPath + "\\errors.log", " ======== " + DateTime.Now.ToString() + " ========" + Environment.NewLine);
                File.AppendAllText(Application.StartupPath + "\\errors.log", "===========================================================" + Environment.NewLine);
                logYet = true;
            }
            //
            File.AppendAllText(Application.StartupPath + "\\errors.log", report + Environment.NewLine);
        }
    }

    // Download object makes things easier to deal with
    public class Download
    {
        private Uri _uri;
        private bool _active = true;
        private string _name = "";

        public Download(Uri u, string n)
        {
            _uri = u;
            _name = n;
        }

        public Uri URL
        {
            get { return _uri; }
        }

        public bool Active
        {
            get { return _active; }
            set { _active = value; }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
