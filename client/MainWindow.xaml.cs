﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using ClassLibrary;
using System.Runtime.InteropServices;

namespace client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Строковые константы

        public const string strConnect = "Соединение установлено";
        public const string strDisconnect = "Соединение прекращено";

        // 

        public const int ReconectSecond = 10;

        // Данные для ини файла

        string iniFile = "param.ini";
        string iniSection = "main";
        string iniIP = "ip";
        string iniPort = "port";
        string iniName = "name";

        // Данные класса

        private TClientSocket _client;
        private WinForms.NotifyIcon _notifier = new WinForms.NotifyIcon();
        private DispatcherTimer _Timer;
        private bool Reconnect = true;
        private MiniWindow _miniWindow;
        private TFlowDocument _fd;
        private bool _IsClosing = false;

        // Методы

        public MainWindow()
        {
            InitializeComponent();

            _client = new TClientSocket();
            _client.OnConnect = OnConnect;
            _client.OnDisconnect = OnDisconnect;
            _client.OnReceive = OnReceive;
            _client.OnError = OnError;
            _client.OnLog = Log;

            this._notifier.MouseDown += new WinForms.MouseEventHandler(notifier_MouseDown);
            this._notifier.Icon = Properties.Resources.icoBallRed;
            this._notifier.Visible = true;

            _fd = new TFlowDocument(lbChat.Document);
        }

        public void ShowBalloon(string text, int second = 5, string title = "", WinForms.ToolTipIcon icon = WinForms.ToolTipIcon.Info)
        {
            _miniWindow.Show(text, second);
        }

        void notifier_MouseDown(object sender, WinForms.MouseEventArgs e)
        {
            this.Activate();
        }

        private void OnConnect(TSocket client)
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    ChatWriteString(strConnect);
                    SetStatusControl();
                    Log(strConnect);
                    ShowBalloon(strConnect);
                    _client.SendString(edtName.Text);
                }
            );
        }

        private void OnDisconnect(TSocket client)
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    if (!_IsClosing)
                    {
                        ChatWriteString(strDisconnect);
                        Log(strDisconnect);
                        SetStatusControl();
                        ShowBalloon(strDisconnect);
                    }
                }
            );
        }

        private void OnReceive(TSocket client, string str)
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    if (str.Trim().Length > 0)
                    {
                        ChatWriteString(str);
                        ShowBalloon(str);
                    }
                }
            );
        }

        private void OnError(TSocket client, Exception e)
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    Log(e.Message);
                    if (!client.Connected && Reconnect)
                        // Запуск коннекта через N секунд
                        StartReconnect();
                }
            );
        }

        private void StartReconnect()
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    Log("Попытка подключения через " + ReconectSecond.ToString() + " секунд.");
                    if (_Timer == null)
                    {
                        _Timer = new DispatcherTimer();
                        _Timer.Tick += StartReconnectCallback;
                        _Timer.Interval = new TimeSpan(0, 0, ReconectSecond);
                    }
                    if (!_Timer.IsEnabled)
                    {
                        _Timer.Start();
                    }
                }
            );
        }

        private void StartReconnectCallback(object sender, EventArgs e)
        {
            _Timer.Stop();
            Connect();
        }

        private void Log(string str)
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    lbLog.Items.Insert(0, DateTime.Now.ToString() + "\t" + str);
                }
            );
        }

        private void ChatWriteStrings(string[] mStr)
        {
            _fd.Write(_fd.CreateParagraph(mStr));
        }

        private void ChatWriteString(string str)
        {
            ChatWriteStrings(SplitStringBR(str));
        }

        private string[] SplitStringBR(string str)
        {
            return str.Split(new string[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void sendTextAndClear()
        {
            _client.SendString(edtText.Text);
            ChatWriteString(edtText.Text);
            edtText.Text = "";
        }

        public void Connect()
        {
            if (!_client.Connected)
            {
                Log("Попытка подключение к " + edtIP.Text + ":" + edtPort.Text);
                int port = 5050;
                int.TryParse(edtPort.Text, out port);
                _client.Connect(edtIP.Text, port);
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Reconnect = true;
            Connect();
        }

        private void SetStatusControl()
        {
            btnConnect.IsEnabled = !_client.Connected;
            btnDisconnect.IsEnabled = !btnConnect.IsEnabled;
            btnSend.IsEnabled = btnDisconnect.IsEnabled;

            string imgBall = "Resources/BallRed.png";
            System.Drawing.Icon icon = Properties.Resources.icoBallRed;
            if (_client.Connected)
            {
                imgBall = "Resources/BallGreen.png";
                icon = Properties.Resources.icoBallGreen;
            }
            imgConnect.Source = new BitmapImage(new Uri(imgBall, UriKind.Relative));
            _notifier.Icon = icon;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _IsClosing = true;

            if (_client.Connected)
            {
                _client.Disconnect();
            }

            _notifier.Visible = false;
            _notifier.Dispose();

            PropertySave();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            sendTextAndClear();
        }

        private void edtText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                sendTextAndClear();
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Reconnect = false;
            _client.Disconnect();
        }

        public void setVisibleLayer(RadioButton btn)
        {
            if (btn == btnChat)
            {
                layChat.Visibility = Visibility.Visible;
                layLogs.Visibility = Visibility.Hidden;
                layProperty.Visibility = Visibility.Hidden;
            }
            else if (btn == btnLogs)
            {
                layChat.Visibility = Visibility.Hidden;
                layLogs.Visibility = Visibility.Visible;
                layProperty.Visibility = Visibility.Hidden;
            }
            else if (btn == btnProperties)
            {
                layChat.Visibility = Visibility.Hidden;
                layLogs.Visibility = Visibility.Hidden;
                layProperty.Visibility = Visibility.Visible;
            }
        }

        private void btn_Checked(object sender, RoutedEventArgs e)
        {
            setVisibleLayer((RadioButton)sender);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _miniWindow = new MiniWindow(this);

            PropertyLoad();

            btnChat.IsChecked = true;
            if (edtIP.Text != "")
            {
                Connect();
            }
        }

        private void PropertyLoad()
        {
            IniFiles ini = new IniFiles(iniFile);
            edtIP.Text = ini.Read(iniSection, iniIP);
            edtPort.Text = ini.Read(iniSection, iniPort);
            edtName.Text = ini.Read(iniSection, iniName);
        }

        private void PropertySave()
        {
            IniFiles ini = new IniFiles(iniFile);
            ini.Write(iniSection, iniIP, edtIP.Text);
            ini.Write(iniSection, iniPort, edtPort.Text);
            ini.Write(iniSection, iniName, edtName.Text);
        }
    }
}
