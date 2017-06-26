using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharePoint_Opener_for_VSCode
{
    public partial class Main : Form
    {
        const string VSCodePath = @"C:\Program Files (x86)\Microsoft VS Code\code.exe";

        // Import DLL to set Cookies in IE
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetSetCookie(string lpszUrlName, string lpszCookieName, string lpszCookieData);

        public Main()
        {
            InitializeComponent();
        }

        private async void btnOpen_Click(object sender, EventArgs e)
        {
            var sharepointUri = new Uri(txtUrl.Text);

            lblStatus.Text = "Getting cookies...";

            await RunAsync(sharepointUri, "paul.brown@adepteq.com", "Prb*123.", false, false);

            if (SpoAuthUtility.Current != null)
            {
                //txtStatus.AppendText("Setting Cookies in OS...");
                try
                {
                    // Create the cookie collection object for sharepoint URI
                    CookieCollection cookies = SpoAuthUtility.Current.cookieContainer.GetCookies(sharepointUri);

                    // Extract the base URL in case the URL provided contains nested paths (e.g. https://contoso.sharepoint.com/abc/ddd/eed)
                    // The cookie has to be set for the domain (contoso.sharepoint.com), otherwise it will not work
                    String baseUrl = sharepointUri.Scheme + "://" + sharepointUri.Host;

                    if (InternetSetCookie(baseUrl, null, cookies["FedAuth"].ToString() + "; Expires = " + cookies["FedAuth"].Expires.AddMinutes(0).ToString("R")))
                    {
                        if (InternetSetCookie(baseUrl, null, cookies["rtFA"].ToString() + "; Expires = " + cookies["rtFA"].Expires.AddMinutes(0).ToString("R")))
                        {
                            //txtStatus.AppendText("[OK]. Expiry = " + cookies["FedAuth"].Expires.AddMinutes(0).ToString("R"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = ("[ERROR setting Cookies]:" + ex.Message);
                }
            }

            var webDAVPath = txtUrl.Text;

            // Change all back-slashes to forward-slashes
            webDAVPath = webDAVPath.Replace("/", "\\");

            // Remove protocol
            webDAVPath = webDAVPath.Replace("https:", string.Empty);

            // Insert @SSL\DavWWWRoot
            webDAVPath = webDAVPath.Replace(".sharepoint.com", ".sharepoint.com@SSL\\DavWWWRoot");

            lblStatus.Text = string.Format("Opening '{0}' in VS Code...", webDAVPath);

            // Start VS Code with the SharePoint Path
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = Path.GetFileName(VSCodePath);
            processInfo.WorkingDirectory = Path.GetDirectoryName(VSCodePath);
            processInfo.Arguments = string.Format("-n {0}", webDAVPath);

            Process.Start(processInfo);
        }

        static async Task RunAsync(Uri sharepointUri, string username, string password, bool useIntegratedAuth, bool verbose)
        {
            await SpoAuthUtility.Create(sharepointUri, username, password, useIntegratedAuth, verbose);
        }
    }
}
