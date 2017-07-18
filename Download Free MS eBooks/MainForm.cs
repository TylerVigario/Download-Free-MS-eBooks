using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;
using System.IO;

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
            //
            this.olvColumn1.AspectToStringConverter = delegate(object x) {
                Uri url = (Uri)x;
                return url.ToString();
            };
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            wClient = new WebClient();
            //
            urls = downloadBookList();
            if (urls == null)
            {
                return;
            }
            //
            objectListView1.SetObjects(urls);
        }

        private List<Download> downloadBookList()
        {
            string temp;
            //
            try
            {
                temp = wClient.DownloadString("https://drive.google.com/uc?export=download&id=0B-ULAF28Y63jY00wZEtWSmRTUWM");
            }
            catch (WebException)
            {
                MessageBox.Show("File list URL is down. Look for update from developer of this software.");
                return null;
            }
            //
            string[] lines = temp.Split(new string[] { "\r\n", "\t\t" }, StringSplitOptions.None);
            List<Download> urls = new List<Download>();
            //
            foreach (string line in lines)
            {
                if (!String.IsNullOrEmpty(line) && line.Trim() != "MSFT eBooks")
                {
                    string[] parts = line.Split(',');
                    //
                    try
                    {
                        urls.Add(new Download(new Uri(parts[0].Trim()), parts[1].Trim()));
                    }
                    catch (UriFormatException)
                    {
                        LogLostFile(parts[0]);
                    }
                }
            }
            //
            return urls;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) { return; }
            downloadLocation = folderBrowserDialog1.SelectedPath;
            downloads = new List<Download>();
            //
            foreach (Download details in objectListView1.Objects)
            {
                if (details.Active)
                {
                    downloads.Add(details);
                }
            }
            //
            toolStripProgressBar1.Maximum = downloads.Count;
            wClient.DownloadFileCompleted += WClient_DownloadFileCompleted;
            toolStripStatusLabel1.Text = "Downloading " + downloads[0].Name;
            //
            try
            {
                wClient.DownloadFileAsync(downloads[0].URL, downloadLocation + "\\" + downloads[0].Name);
            }
            catch(WebException)
            {
                LogLostFile(downloads[0].URL.ToString());
            }
            //
            downloads.Remove(downloads[0]);
        }

        private void WClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            toolStripProgressBar1.PerformStep();
            //
            if (downloads.Count > 0)
            {
                toolStripStatusLabel1.Text = "Downloading " + downloads[0].Name;
                //
                try
                {
                    wClient.DownloadFileAsync(downloads[0].URL, downloadLocation + "\\" + downloads[0].Name);
                }
                catch (WebException)
                {
                    LogLostFile(downloads[0].URL.ToString());
                }
                //
                downloads.Remove(downloads[0]);
            }
            else
            {
                MessageBox.Show("Finished downloading");
            }
        }

        private void LogLostFile(string report)
        {
            if (!logYet)
            {
                LogLostFile(" ======== Download Free MS eBooks v1.1 ========");
                LogLostFile(" == " + DateTime.Now.ToString());
                logYet = true;
            }
            //
            File.AppendAllText(Application.StartupPath + "\\errors.log", report + Environment.NewLine);
        }
    }

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
