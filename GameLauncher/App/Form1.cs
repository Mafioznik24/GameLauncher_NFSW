﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using System.Xml;
using GameLauncher.Resources;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.Threading;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using SoapBox.JsonScheme;
using GameLauncher.App.Classes;

namespace GameLauncher {
    public partial class mainScreen : Form {
        Point mouseDownPoint = Point.Empty;
        bool loginEnabled;
        bool serverEnabled;
        bool builtinserver = false;
        bool useSavedPassword;
        bool skipServerTrigger = false;
        IniFile SettingFile = new IniFile("Settings.ini");
        string UserSettings = Environment.ExpandEnvironmentVariables("%AppData%\\Need for Speed World\\Settings\\UserSettings.xml");

        private void moveWindow_MouseDown(object sender, MouseEventArgs e) {
            mouseDownPoint = new Point(e.X, e.Y);
        }

        private void moveWindow_MouseUp(object sender, MouseEventArgs e) {
            mouseDownPoint = Point.Empty;
        }

        private void moveWindow_MouseMove(object sender, MouseEventArgs e) {
            if (mouseDownPoint.IsEmpty) { return; }
            Form f = this as Form;
            f.Location = new Point(f.Location.X + (e.X - mouseDownPoint.X), f.Location.Y + (e.Y - mouseDownPoint.Y));
        }

        public void ConsoleLog(string e, string type) {
            consoleLog.SelectionStart = consoleLog.TextLength;
            consoleLog.SelectionLength = 0;
            consoleLog.SelectionFont = new Font(consoleLog.Font, FontStyle.Bold);

            consoleLog.SelectionColor = Color.Gray;
            consoleLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] ");

            if (type == "warning") {
                consoleLog.SelectionColor = Color.Yellow;
                consoleLog.AppendText("[WARN] ");
            } else if (type == "info") {
                consoleLog.SelectionColor = Color.Cyan;
                consoleLog.AppendText("[INFO] ");
            } else if (type == "error") {
                consoleLog.SelectionColor = Color.Red;
                consoleLog.AppendText("[ERROR] ");
            } else if (type == "success") {
                consoleLog.SelectionColor = Color.Lime;
                consoleLog.AppendText("[SUCCESS] ");
            } else if (type == "ping") {
                consoleLog.SelectionColor = Color.DarkOrange;
                consoleLog.AppendText("[PING] ");
            }

            consoleLog.SelectionColor = consoleLog.ForeColor;
            consoleLog.SelectionFont = new Font(consoleLog.Font, FontStyle.Regular);
            consoleLog.AppendText(e);
            consoleLog.AppendText("\r\n");
            consoleLog.ScrollToCaret();
        }

        public mainScreen() {
            InitializeComponent();
            ApplyEmbeddedFonts();

            MaximizeBox = false;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);

            closebtn.MouseEnter += new EventHandler(closebtn_MouseEnter);
            closebtn.MouseLeave += new EventHandler(closebtn_MouseLeave);
            closebtn.Click += new EventHandler(closebtn_Click);

            settingsButton.MouseEnter += new EventHandler(settingsButton_MouseEnter);
            settingsButton.MouseLeave += new EventHandler(settingsButton_MouseLeave);
            settingsButton.Click += new EventHandler(settingsButton_Click);

            minimizebtn.MouseEnter += new EventHandler(minimizebtn_MouseEnter);
            minimizebtn.MouseLeave += new EventHandler(minimizebtn_MouseLeave);
            minimizebtn.Click += new EventHandler(minimizebtn_Click);

            loginButton.MouseEnter += new EventHandler(loginButton_MouseEnter);
            loginButton.MouseLeave += new EventHandler(loginButton_MouseLeave);
            loginButton.Click += new EventHandler(loginButton_Click);
            loginButton.MouseUp += new MouseEventHandler(loginButton_MouseUp);
            loginButton.MouseDown += new MouseEventHandler(loginButton_MouseDown);

            registerButton.MouseEnter += new EventHandler(registerButton_MouseEnter);
            registerButton.MouseLeave += new EventHandler(registerButton_MouseLeave);
            registerButton.MouseUp += new MouseEventHandler(registerButton_MouseUp);
            registerButton.MouseDown += new MouseEventHandler(registerButton_MouseDown);
            registerButton.Click += new EventHandler(registerButton_Click);

            settingsSave.MouseEnter += new EventHandler(settingsSave_MouseEnter);
            settingsSave.MouseLeave += new EventHandler(settingsSave_MouseLeave);
            settingsSave.MouseUp += new MouseEventHandler(settingsSave_MouseUp);
            settingsSave.MouseDown += new MouseEventHandler(settingsSave_MouseDown);
            settingsSave.Click += new EventHandler(settingsSave_Click);

            email.KeyUp += new KeyEventHandler(loginbuttonenabler);
            email.PreviewKeyDown += new PreviewKeyDownEventHandler(loginEnter);
            password.KeyUp += new KeyEventHandler(loginbuttonenabler);
            password.PreviewKeyDown += new PreviewKeyDownEventHandler(loginEnter);

            serverPick.TextChanged += new EventHandler(serverPick_TextChanged);

            forgotPassword.LinkClicked += new LinkLabelLinkClickedEventHandler(forgotPassword_LinkClicked);
            githubLink.LinkClicked += new LinkLabelLinkClickedEventHandler(githubLink_LinkClicked);

            moveWindow.MouseDown += new MouseEventHandler(moveWindow_MouseDown);
            moveWindow.MouseMove += new MouseEventHandler(moveWindow_MouseMove);
            moveWindow.MouseUp += new MouseEventHandler(moveWindow_MouseUp);

            //Command-line Arguments
            string[] args = Environment.GetCommandLineArgs();

            //Somewhere here we will setup the game installation directory
            directoryInstallation();

            registerText.Text = "DON'T HAVE AN ACCOUNT?\nCLICK HERE TO CREATE ONE NOW...";
        }

        public void directoryInstallation(bool bypass = false) {
            if (!SettingFile.KeyExists("InstallationDirectory") || bypass == true) {
                var openFolder = new CommonOpenFileDialog();
                openFolder.InitialDirectory = "";
                openFolder.IsFolderPicker = true;
                openFolder.Title = "GameLauncher: Please pick up a directory with NFSW.";
                var result = openFolder.ShowDialog();

                if (result == CommonFileDialogResult.Ok) {
                    SettingFile.Write("InstallationDirectory", openFolder.FileName);
                } else if (result == CommonFileDialogResult.Cancel) {
                    Environment.Exit(Environment.ExitCode);
                }
            }

            if(!File.Exists(SettingFile.Read("InstallationDirectory") + "/nfsw.exe")) {
                DialogResult InstallerAsk = MessageBox.Show(null, "There's no 'Need For Speed: World' installation over there. Do you wanna select new installation directory?", "GameLauncher", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if(InstallerAsk == DialogResult.Yes) {
                    directoryInstallation(true);
                } else {
                    Environment.Exit(Environment.ExitCode);
                }
            }

            /*if (!Directory.Exists(SettingFile.Read("InstallationDirectory"))) {
                Directory.CreateDirectory(SettingFile.Read("InstallationDirectory")  + "/nfsw");
                Directory.CreateDirectory(SettingFile.Read("InstallationDirectory")  + "/nfsw/Cache");
                Directory.CreateDirectory(SettingFile.Read("InstallationDirectory")  + "/nfsw/Data");
                Directory.CreateDirectory(SettingFile.Read("InstallationDirectory")  + "/nfsw/Data/Modules");
                File.Create(SettingFile.Read("InstallationDirectory")  + "/nfsw/Cache/keep.this");
                File.Create(SettingFile.Read("InstallationDirectory")  + "/nfsw/Data/put.your.nfsw.exe.here");
                Process.Start(@"" + SettingFile.Read("InstallationDirectory")  + "/nfsw/Data/");
            }*/
        }

        private void mainScreen_Load(object sender, EventArgs e) {
            //Console output to textbox
            ConsoleLog("Log initialized", "info");
            ConsoleLog("GameLauncher initialized", "info");
            ConsoleLog("Installation directory: " + SettingFile.Read("InstallationDirectory"), "info");

            //Silly way to detect mono
            int SysVersion = (int)Environment.OSVersion.Platform;
            bool mono = (SysVersion == 4 || SysVersion == 6 || SysVersion == 128);

            //Silly way to detect wine
            bool wine;
            try {
                RegistryKey regKey = Registry.CurrentUser;
                RegistryKey rkTest = regKey.OpenSubKey(@"Software\Wine");

                if(String.IsNullOrEmpty(rkTest.ToString())) {
                    wine = false;
                } else {
                    wine = true;
                }
            } catch {
                wine = false;
            }

            //Console log with warning
            if (mono == true) {
                ConsoleLog("Detected OS: Linux using Mono - Note that game might not launch.", "warning");
            } else if (wine == true) {
                ConsoleLog("Detected OS: Linux using Wine - Note that game might not launch.", "warning");
            }

            email.Text = SettingFile.Read("AccountEmail");
            if (!String.IsNullOrEmpty(SettingFile.Read("AccountEmail")) && !String.IsNullOrEmpty(SettingFile.Read("Password"))) {
                rememberMe.Checked = true;
            }

            //Fetch serverlist, and disable if failed to fetch.
            var response = "";
            try {
                WebClient wc = new WebClientWithTimeout();
                wc.Headers.Add("user-agent", "GameLauncher (+https://github.com/metonator/GameLauncher_NFSW)");

                string serverurl = "http://nfsw.metonator.ct8.pl/serverlist.txt";
                response = wc.DownloadString(serverurl);
                ConsoleLog("Fetching " + serverurl, "info");
            } catch (Exception ex) {
                ConsoleLog("Failed to fetch serverlist. " + ex.Message, "error");
            }

            //Time to add servers
            serverPick.DisplayMember = "Text";
            serverPick.ValueMember = "Value";

            List<Object> items = new List<Object>();
            response += "Offline Built-In Server;http://localhost:7331/nfsw/Engine.svc";

            String[] substrings = response.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var substring in substrings) {
                if (!String.IsNullOrEmpty(substring)) {
                    String[] substrings2 = substring.Split(new string[] { ";" }, StringSplitOptions.None);
                    items.Add(new { Text = substrings2[0], Value = substrings2[1] });
                }
            }

            serverPick.DataSource = items;
            serverPick.SelectedIndex = 0;

            //Silliest way to prevent doublecall of TextChanged event...
            if(!SettingFile.KeyExists("Server")) {
                SettingFile.Write("Server", serverPick.SelectedValue.ToString());
            }

            if(SettingFile.KeyExists("Server")) {
                skipServerTrigger = true;
                serverPick.SelectedValue = SettingFile.Read("Server");

                //I don't know other way to fix this call...
                if(serverPick.SelectedIndex == 0) {
                    serverPick_TextChanged(sender, e);
                }
            }

            serverStatusImg.Location = new Point(-16, -16);

            if (SettingFile.KeyExists("Password")) {
                loginEnabled = true;
                serverEnabled = true;
                useSavedPassword = true;
                this.loginButton.Image = Properties.Resources.button_enable;
                this.loginButton.ForeColor = Color.White;
                ConsoleLog("Password recovered from Settings.ini file.", "success");
            } else {
                loginEnabled = false;
                serverEnabled = false;
                useSavedPassword = false;
                this.loginButton.Image = Properties.Resources.button_disable;
                this.loginButton.ForeColor = Color.Gray;
            }

            //Add downloadable languages to settingLanguage
            settingsLanguage.DisplayMember = "Text";
            settingsLanguage.ValueMember = "Value";

            var languages = new[] { 
                new { Text = "English", Value = "EN" },
                new { Text = "Deutsch", Value = "DE" },
                new { Text = "Español", Value = "ES" },
                new { Text = "Français", Value = "FR" },
                new { Text = "Polski", Value = "PL" },
                new { Text = "Русский", Value = "RU" },
                new { Text = "Português (Brasil)", Value = "PT" },
                new { Text = "繁體中文", Value = "TC" },
                new { Text = "简体中文", Value = "SC" },
                new { Text = "ภาษาไทย", Value = "TH" },
                new { Text = "Türkçe", Value = "TR" },
            };

            settingsLanguage.DataSource = languages;

            if(SettingFile.KeyExists("Language", "Downloader")) {
                settingsLanguage.SelectedValue = SettingFile.Read("Language", "Downloader");
            }

            //Add downloadable quality to settingLanguage
            settingsQuality.DisplayMember = "Text";
            settingsQuality.ValueMember = "Value";

            var quality = new[] { 
                new { Text = "Standard", Value = "0" },
                new { Text = "Maximum", Value = "1" },
            };

            settingsQuality.DataSource = quality;

            if(SettingFile.KeyExists("TracksHigh", "Downloader")) {
                settingsQuality.SelectedValue = SettingFile.Read("TracksHigh", "Downloader");
            }

            //Detect UserSettings
            if(File.Exists(UserSettings)) {
                ConsoleLog("Found Game Config under UserSettings.xml file.", "success");
            }

            //Hide other windows
            RegisterFormElements(false);
            SettingsFormElements(false);
        }

        private void closebtn_Click(object sender, EventArgs e) {
            this.closebtn.BackgroundImage = Properties.Resources.close_click;
            if(!Directory.Exists("logs")) {
                Directory.CreateDirectory("logs");
            }

            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000;
            string timestamp = ticks.ToString();
            consoleLog.SaveFile("logs/" + timestamp + ".log", RichTextBoxStreamType.PlainText);

            SettingFile.Write("Server", serverPick.SelectedValue.ToString());

            Application.ExitThread();
            Application.Exit();
        }

        private void closebtn_MouseEnter(object sender, EventArgs e) {
            this.closebtn.BackgroundImage = Properties.Resources.close_hover;
        }

        private void closebtn_MouseLeave(object sender, EventArgs e) {
            this.closebtn.BackgroundImage = Properties.Resources.close;
        }

        private void minimizebtn_Click(object sender, EventArgs e) {
            this.minimizebtn.BackgroundImage = Properties.Resources.minimize_click;
            this.WindowState = FormWindowState.Minimized;
        }

        private void minimizebtn_MouseEnter(object sender, EventArgs e) {
            this.minimizebtn.BackgroundImage = Properties.Resources.minimize_hover;
        }

        private void minimizebtn_MouseLeave(object sender, EventArgs e) {
            this.minimizebtn.BackgroundImage = Properties.Resources.minimize;
        }

        private void loginEnter(object sender, PreviewKeyDownEventArgs e) {
            if (e.KeyCode == Keys.Return && loginEnabled == true) {
                loginButton_Click(sender, e);
            }
        }

        private void loginbuttonenabler(object sender, EventArgs e) {
            if (String.IsNullOrEmpty(email.Text) || String.IsNullOrEmpty(password.Text)) {
                loginEnabled = false;
                this.loginButton.Image = Properties.Resources.button_disable;
                this.loginButton.ForeColor = Color.Gray;
            }
            else {
                loginEnabled = true;
                this.loginButton.Image = Properties.Resources.button_enable;
                this.loginButton.ForeColor = Color.White;
            }

            useSavedPassword = false;
        }

        private void loginButton_MouseUp(object sender, EventArgs e) {
            if (loginEnabled == true || builtinserver == true) {
                this.loginButton.Image = Properties.Resources.button_hover;
            } else {
                this.loginButton.Image = Properties.Resources.button_disable;
            }
        }

        private void loginButton_MouseDown(object sender, EventArgs e) {
            if (loginEnabled == true || builtinserver == true) {
                this.loginButton.Image = Properties.Resources.button_click;
            } else {
                this.loginButton.Image = Properties.Resources.button_disable;
            }
        }

        private void loginButton_Click(object sender, EventArgs e) {
            if((loginEnabled == false || serverEnabled == false) && builtinserver == false) {
                return;
            }

            string serverIP = serverPick.SelectedValue.ToString();
            string serverName = serverPick.GetItemText(serverPick.SelectedItem);
            string username = email.Text.ToString();
            string encryptedpassword = "";
            string serverLoginResponse = "";

            HashAlgorithm algorithm = SHA1.Create();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in algorithm.ComputeHash(Encoding.UTF8.GetBytes(password.Text.ToString()))) {
                sb.Append(b.ToString("X2"));
            }

            if (useSavedPassword) {
                encryptedpassword = SettingFile.Read("Password");
            } else {
                encryptedpassword = sb.ToString();
            }

            if (rememberMe.Checked) {
                SettingFile.Write("AccountEmail", username);
                SettingFile.Write("Password", encryptedpassword.ToLower());
            } else {
                SettingFile.DeleteKey("AccountEmail");
                SettingFile.DeleteKey("Password");
            }



            ConsoleLog("Trying to login into " + serverPick.GetItemText(serverPick.SelectedItem) + " (" + serverIP + ")", "info");

            if(builtinserver == true) {
                MessageBox.Show(null, "Careful: This built-in server is in alpha! Use it at your own risk.", "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            try {
                WebClient wc = new WebClientWithTimeout();
                wc.Headers.Add("user-agent", "GameLauncher (+https://github.com/metonator/GameLauncher_NFSW)");

                string BuildURL = serverIP + "/User/authenticateUser?email=" + username + "&password=" + encryptedpassword.ToLower();
                ConsoleLog(BuildURL, "info");

                serverLoginResponse = wc.DownloadString(BuildURL);
            } catch (WebException ex) {
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    HttpWebResponse serverReply = (HttpWebResponse)ex.Response;
                    if ((int)serverReply.StatusCode == 500) {
                        using (StreamReader sr = new StreamReader(serverReply.GetResponseStream())) {
                            serverLoginResponse = sr.ReadToEnd();
                        }
                    } else {
                        serverLoginResponse = ex.Message;
                    }
                } else {
                    serverLoginResponse = ex.Message;
                }
            }

            XmlDocument SBRW_XML = new XmlDocument();
            SBRW_XML.LoadXml(serverLoginResponse);
            XmlNodeList nodes = null;

            String Description = "";
            String LoginToken = "";
            String UserId = "";

            if (serverName == "World Revival") {
                try {
                    if(SBRW_XML.SelectSingleNode("EngineExceptionTrans") != null) {
                        nodes = SBRW_XML.SelectNodes("EngineExceptionTrans");
                    } else {
                        nodes = SBRW_XML.SelectNodes("LoginData");
                    }
                } catch(Exception) {
                    nodes = SBRW_XML.SelectNodes("LoginData");
                }
            }
            else {
                nodes = SBRW_XML.SelectNodes("LoginStatusVO");
            }

            foreach (XmlNode childrenNode in nodes) {
                if (serverName == "World Revival") {
                    try {
                        UserId = childrenNode["UserId"].InnerText;
                        LoginToken = childrenNode["LoginToken"].InnerText;
                    } catch {
                        Description = "LOGIN ERROR";
                    }
                } else {
                    UserId = childrenNode["UserId"].InnerText;
                    LoginToken = childrenNode["LoginToken"].InnerText;
                    Description = childrenNode["Description"].InnerText;
                }

                if (String.IsNullOrEmpty(LoginToken)) {
                    ConsoleLog("Invalid username or password.", "error");
                } else {
                    try {
                        /*this.BackgroundImage = Properties.Resources.playbg;
                        this.currentWindowInfo.Hide();
                        LoginFormHideElements();
                        DownloadFormShowElements();*/

                        string filename = SettingFile.Read("InstallationDirectory") + "\\nfsw.exe";
                        ConsoleLog("Logged in. Starting game (" + filename + ").", "success");
                        String cParams = "US " + serverIP + " " + LoginToken + " " + UserId;
                        var proc = Process.Start(filename, cParams);
                        proc.EnableRaisingEvents = true;

                        proc.Exited += (sender2, e2) => {
                            closebtn_Click(sender2, e2);
                        };

                        if (builtinserver == true) {
                            ConsoleLog("SoapBox Built-In Initialized, waiting for queries", "success");
                        } else {
                            ConsoleLog("Closing myself in 5 seconds.", "warning");
                            Thread.Sleep(5000);
                            closebtn_Click(sender, e);
                        }
                    } catch (Exception) {
                        ConsoleLog("Logged in. But i cannot find NFSW executable file. Are you sure you've copied all files?", "error");
                    }
                }
            }
        }

        private void loginButton_MouseEnter(object sender, EventArgs e) {
            if (loginEnabled == true || builtinserver == true) {
                this.loginButton.Image = Properties.Resources.button_hover;
                this.loginButton.ForeColor = Color.White;
            } else {
                this.loginButton.Image = Properties.Resources.button_disable;
                this.loginButton.ForeColor = Color.Gray;
            }
        }

        private void loginButton_MouseLeave(object sender, EventArgs e) {
            if (loginEnabled == true || builtinserver == true) {
                this.loginButton.Image = Properties.Resources.button_enable;
                this.loginButton.ForeColor = Color.White;
            } else {
                this.loginButton.Image = Properties.Resources.button_disable;
                this.loginButton.ForeColor = Color.Gray;
            }
        }

        private void clearConsole_Click(object sender, EventArgs e) {
            consoleLog.SelectionColor = Color.Gray;
            consoleLog.Text = "Console cleaned.\n";
        }

        private void serverPick_TextChanged(object sender, EventArgs e) {
            if (!skipServerTrigger) { return; }

            loginEnabled = false;
            this.loginButton.Image = Properties.Resources.button_disable;
            this.loginButton.ForeColor = Color.Gray;
            this.password.Text = "";
            string verticalImageUrl = "";
            verticalBanner.Image = null;
            verticalBanner.BackColor = Color.Transparent;

            string serverIP = serverPick.SelectedValue.ToString();
            string numPlayers;
            string serverName = serverPick.GetItemText(serverPick.SelectedItem);

            serverStatusImg.Location = new Point(-16, -16);
            serverStatus.ForeColor = Color.White;
            serverStatus.Text = "Retrieving server status...";
            serverStatus.Location = new Point(44, 329);
            onlineCount.Text = "";

            if (serverPick.GetItemText(serverPick.SelectedItem) == "Offline Built-In Server") {
                builtinserver = true;
                this.loginButton.Image = Properties.Resources.button_enable;
                this.loginButton.Text = "LAUNCH";
                this.loginButton.ForeColor = Color.White;
            } else {
                builtinserver = false;
                this.loginButton.Image = Properties.Resources.button_disable;
                this.loginButton.Text = "LOG IN";
                this.loginButton.ForeColor = Color.Gray;
            }

            var client = new WebClientWithTimeout();
            client.Headers.Add("user-agent", "GameLauncher (+https://github.com/metonator/GameLauncher_NFSW)");

            Uri StringToUri = new Uri(serverIP + "/GetServerInformation");
            client.CancelAsync();
            client.DownloadStringAsync(StringToUri);
            client.DownloadStringCompleted += (sender2, e2) => {
                if (e2.Cancelled) {
                    client.CancelAsync();
                    return;
                } else if (e2.Error != null) {
                    serverStatusImg.Location = new Point(20, 335);
                    serverStatusImg.BackgroundImage = Properties.Resources.server_offline;
                    serverStatus.ForeColor = Color.FromArgb(227, 88, 50);
                    serverStatus.Text = "This server is currently down. Thanks for your patience.";
                    serverStatus.Location = new Point(44, 329);
                    onlineCount.Text = "";
                    serverEnabled = false;
                } else {
                    serverStatusImg.Location = new Point(20, 323);
                    serverStatusImg.BackgroundImage = Properties.Resources.server_online;
                    serverStatus.ForeColor = Color.FromArgb(181, 255, 33);
                    serverStatus.Text = "This server is currenly up and running.";
                    serverStatus.Location = new Point(44, 322);

                    if (serverName == "Offline Built-In Server") {
                        numPlayers = "∞";
                    } else if(serverName == "World Revival") {
                        //JSON... and Dedicated API... c'mon WorldRevival...
                        Uri StringToUri2 = new Uri("http://world-revival.fr/api/GetStatus.php");
                        var reply = client.DownloadString(StringToUri2);
                        GetStatus json = JsonConvert.DeserializeObject<GetStatus>(reply);
                        numPlayers = json.server.slots + " out of " + json.server.maxslots;
                    } else {
                        GetServerInformation json = JsonConvert.DeserializeObject<GetServerInformation>(e2.Result);
                        
                        if (!String.IsNullOrEmpty(json.bannerUrl)) {
                            Uri uriResult;
                            bool result = Uri.TryCreate(json.bannerUrl, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                            if (result) {
                                verticalImageUrl = json.bannerUrl;
                            } else {
                                verticalImageUrl = null;
                            }
                        } else {
                            verticalImageUrl = null;
                        }

                        numPlayers = json.onlineNumber + " out of " + json.numberOfRegistered;
                    }

                    onlineCount.Text = "Players on server: " + numPlayers;
                    serverEnabled = true;

                    if (!String.IsNullOrEmpty(verticalImageUrl)) {
                        var client2 = new WebClientWithTimeout();
                        client.Headers.Add("user-agent", "GameLauncher (+https://github.com/metonator/GameLauncher_NFSW)");
                        Uri StringToUri3 = new Uri(verticalImageUrl);
                        client2.DownloadDataAsync(StringToUri3);
                        client2.DownloadDataCompleted += (sender4, e4) => {
                            if (e4.Cancelled) {
                                client2.CancelAsync();
                                return;
                            } else if (e4.Error != null) {
                                //What? 
                            } else {
                                Image image;
                                MemoryStream memoryStream = new MemoryStream(e4.Result);
                                image = Image.FromStream(memoryStream);
                                verticalBanner.Image = image;
                                verticalBanner.BackColor = Color.Black;
                            }
                        };
                    }

                    Ping pingSender = new Ping();
                    pingSender.SendAsync(StringToUri.Host, 1000, new byte[1], new PingOptions(64, true), new AutoResetEvent(false));
                    pingSender.PingCompleted += (sender3, e3) => {
                        PingReply reply = e3.Reply;

                        if (reply.Status == IPStatus.Success && serverName != "Offline Built-In Server") {
                            ConsoleLog("This PC <---> " + serverName + ": " + reply.RoundtripTime + "ms", "ping");
                        } else {
                            ConsoleLog(serverName + " doesn't allow pinging.", "ping");
                        }
                    };
                }
            };
        }

        private void ApplyEmbeddedFonts() {
            FontFamily fontFamily = FontWrapper.Instance.GetFontFamily("Font_MyriadProSemiCondBold.ttf");
            FontFamily fontFamily2 = FontWrapper.Instance.GetFontFamily("Font_Register.ttf");
            FontFamily fontFamily3 = FontWrapper.Instance.GetFontFamily("Font_RegisterBoldItalic.ttf");
            FontFamily fontFamily4 = FontWrapper.Instance.GetFontFamily("Font_RegisterDemiBold.ttf");
            FontFamily fontFamily5 = FontWrapper.Instance.GetFontFamily("Font_RegisterBold.ttf");
            FontFamily fontFamily6 = FontWrapper.Instance.GetFontFamily("Font_MyriadProSemiCond.ttf");

            currentWindowInfo.Font = new Font(fontFamily3, 12.75f, FontStyle.Italic);
            rememberMe.Font = new Font(fontFamily, 9f, FontStyle.Bold);
            loginButton.Font = new Font(fontFamily2, 15f, FontStyle.Bold | FontStyle.Italic);
            registerButton.Font = new Font(fontFamily2, 15f, FontStyle.Bold | FontStyle.Italic);
            settingsSave.Font = new Font(fontFamily2, 15f, FontStyle.Bold | FontStyle.Italic);
            serverStatus.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            onlineCount.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            registerText.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            emailLabel.Font = new Font(fontFamily4, 11f);
            passwordLabel.Font = new Font(fontFamily4, 11f);
            troubleLabel.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            githubLink.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            forgotPassword.Font = new Font(fontFamily, 9f);
            selectServerLabel.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            settingsLanguageText.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            settingsLanguageDesc.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            settingsQualityText.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
            settingsQualityDesc.Font = new Font(fontFamily, 9.749999f, FontStyle.Bold);
        }

        private void registerText_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            if(serverPick.GetItemText(serverPick.SelectedItem) == "World Revival") {
                ConsoleLog("Redirecting into Registration page", "info");
                Process.Start("http://world-revival.fr/user/register");
            } else {
                this.BackgroundImage = Properties.Resources.settingsbg;
                this.currentWindowInfo.Text = "REGISTER ON " + serverPick.GetItemText(serverPick.SelectedItem).ToUpper() + ":";
                LoginFormElements(false);
                RegisterFormElements(true);
            }
        }

        private void forgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            if (serverPick.GetItemText(serverPick.SelectedItem) == "World Revival") {
                ConsoleLog("Redirecting into Reset Password page", "info");
                Process.Start("http://world-revival.fr/user/reset");
            } else {
                Process.Start(serverPick.SelectedValue.ToString().Replace("Engine.svc", "") + "forgotPasswd.jsp");
            }
        }

        private void githubLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            ConsoleLog("Redirecting into GitHub Issue page", "info");
            Process.Start("https://github.com/metonator/GameLauncher_NFSW/issues");
        }

        private void LoginFormElements(bool hideElements = false) {
            this.rememberMe.Visible = hideElements;
            this.loginButton.Visible = hideElements;
            this.serverStatus.Visible = hideElements;
            this.onlineCount.Visible = hideElements;
            this.registerText.Visible = hideElements;
            this.serverPick.Visible = hideElements;
            this.serverStatusImg.Visible = hideElements;
            this.consoleLog.Visible = hideElements;
            this.clearConsole.Visible = hideElements;
            this.email.Visible = hideElements;
            this.password.Visible = hideElements;
            this.emailLabel.Visible = hideElements;
            this.passwordLabel.Visible = hideElements;
            this.troubleLabel.Visible = hideElements;
            this.githubLink.Visible = hideElements;
            this.forgotPassword.Visible = hideElements;
            this.selectServerLabel.Visible = hideElements;
            this.settingsButton.Visible = hideElements;
            this.verticalBanner.Visible = hideElements;
        }

        /* 
         * REGISTER PAGE LAYOUT 
         * Because why should i close Form1 and create/open Form2 if it will look a bit more responsive...
         */

        private void RegisterFormElements(bool hideElements = true) {
            this.registerButton.Visible = hideElements;
        }

        private void registerButton_MouseEnter(object sender, EventArgs e) {
            this.registerButton.Image = Properties.Resources.button_hover;
        }

        private void registerButton_MouseLeave(object sender, EventArgs e) {
            this.registerButton.Image = Properties.Resources.button_enable;
        }

        private void registerButton_MouseUp(object sender, EventArgs e) {
            this.registerButton.Image = Properties.Resources.button_hover;
        }

        private void registerButton_MouseDown(object sender, EventArgs e) {
            this.registerButton.Image = Properties.Resources.button_click;
        }

        private void registerButton_Click(object sender, EventArgs e) {
            this.BackgroundImage = Properties.Resources.loginbg;
            this.currentWindowInfo.Text = "ENTER YOUR ACCOUNT INFORMATION TO LOG IN:";
            RegisterFormElements(false);
            LoginFormElements(true);
        }

        /*
         * SETTINGS PAGE LAYOUT
         */

        private void settingsButton_Click(object sender, EventArgs e) {
            this.settingsButton.BackgroundImage = Properties.Resources.settingsbtn_click;
            this.BackgroundImage = Properties.Resources.settingsbg;
            this.currentWindowInfo.Text = "PLEASE SELECT YOUR GAME SETTINGS:";
            SettingsFormElements(true);
            LoginFormElements(false);
        }

        private void settingsButton_MouseEnter(object sender, EventArgs e) {
            this.settingsButton.BackgroundImage = Properties.Resources.settingsbtn_hover;
        }

        private void settingsButton_MouseLeave(object sender, EventArgs e) {
            this.settingsButton.BackgroundImage = Properties.Resources.settingsbtn;
        }

        private void settingsSave_MouseEnter(object sender, EventArgs e) {
            this.settingsSave.Image = Properties.Resources.button_hover;
        }

        private void settingsSave_MouseLeave(object sender, EventArgs e) {
            this.settingsSave.Image = Properties.Resources.button_enable;
        }

        private void settingsSave_MouseUp(object sender, EventArgs e) {
            this.settingsSave.Image = Properties.Resources.button_hover;
        }

        private void settingsSave_MouseDown(object sender, EventArgs e) {
            this.settingsSave.Image = Properties.Resources.button_click;
        }

        private void settingsSave_Click(object sender, EventArgs e) {
            SettingFile.Write("Language", settingsLanguage.SelectedValue.ToString(), "Downloader");
            SettingFile.Write("TracksHigh", settingsQuality.SelectedValue.ToString(), "Downloader");

            XmlDocument UserSettingsXML = new XmlDocument();
            if(File.Exists(UserSettings)) {
                try {
                    //File has been found, lets change Language setting
                    UserSettingsXML.Load(UserSettings);
                    XmlNode Language = UserSettingsXML.SelectSingleNode("Settings/UI/Language");
                    Language.InnerText = settingsLanguage.SelectedValue.ToString();
                } catch {
                    //XML is Corrupted... let's delete it and create new one
                    File.Delete(UserSettings);

                    XmlNode Setting = UserSettingsXML.AppendChild(UserSettingsXML.CreateElement("Settings"));
                    XmlNode PersistentValue = Setting.AppendChild(UserSettingsXML.CreateElement("PersistentValue"));
                    XmlNode Chat = PersistentValue.AppendChild(UserSettingsXML.CreateElement("Chat"));
                    XmlNode UI = Setting.AppendChild(UserSettingsXML.CreateElement("UI"));

                    Chat.InnerXml   = "<DefaultChatGroup Type=\"string\">" + settingsLanguage.SelectedValue.ToString() +"</DefaultChatGroup>";
                    UI.InnerXml     = "<Language Type=\"string\">" + settingsLanguage.SelectedValue.ToString() + "</Language>";

			        DirectoryInfo directoryInfo = Directory.CreateDirectory(Path.GetDirectoryName(UserSettings));
                }
            } else {
                //There's no file like that, let's create it
                XmlNode Setting = UserSettingsXML.AppendChild(UserSettingsXML.CreateElement("Settings"));
                XmlNode PersistentValue = Setting.AppendChild(UserSettingsXML.CreateElement("PersistentValue"));
                XmlNode Chat = PersistentValue.AppendChild(UserSettingsXML.CreateElement("Chat"));
                XmlNode UI = Setting.AppendChild(UserSettingsXML.CreateElement("UI"));

                Chat.InnerXml   = "<DefaultChatGroup Type=\"string\">" + settingsLanguage.SelectedValue.ToString() +"</DefaultChatGroup>";
                UI.InnerXml     = "<Language Type=\"string\">" + settingsLanguage.SelectedValue.ToString() + "</Language>";

			    DirectoryInfo directoryInfo = Directory.CreateDirectory(Path.GetDirectoryName(UserSettings));
            }

            UserSettingsXML.Save(UserSettings);

            this.BackgroundImage = Properties.Resources.loginbg;
            this.currentWindowInfo.Text = "ENTER YOUR ACCOUNT INFORMATION TO LOG IN:";
            SettingsFormElements(false);
            LoginFormElements(true);
        }

        private void SettingsFormElements(bool hideElements = true) {
            this.settingsSave.Visible = hideElements;
            this.settingsLanguage.Visible = hideElements;
            this.settingsLanguageText.Visible = hideElements;
            this.settingsLanguageDesc.Visible = hideElements;
            this.settingsQuality.Visible = hideElements;
            this.settingsQualityText.Visible = hideElements;
            this.settingsQualityDesc.Visible = hideElements;
        }

        /*
         * DOWNLOAD PAGE LAYOUT
         */

        private void DownloadFormShowElements() {

        }
    }
}