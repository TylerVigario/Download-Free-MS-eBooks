using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;

namespace Download_Free_MS_eBooks
{
    public partial class MainForm : Form
    {
        private List<Download> urls;
        private List<Download> downloads;
        private WebClient wClient;
        private string downloadLocation;

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
                    try
                    {
                        string[] parts = line.Split(',');
                        urls.Add(new Download(new Uri(parts[0].Trim()), parts[1].Trim()));
                    }
                    catch(UriFormatException ex)
                    {
                        // Send error maybe
                        // Log it for sure
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
            //
            wClient.DownloadFileCompleted += WClient_DownloadFileCompleted;
            toolStripStatusLabel1.Text = "Downloading " + downloads[0].Name;
            wClient.DownloadFileAsync(downloads[0].URL, downloadLocation + "\\" + downloads[0].Name);
            downloads.Remove(downloads[0]);
        }

        private void WClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "Downloading " + downloads[0].Name;
            wClient.DownloadFileAsync(downloads[0].URL, downloadLocation + "\\" + downloads[0].Name);
            downloads.Remove(downloads[0]);
            toolStripProgressBar1.PerformStep();
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
