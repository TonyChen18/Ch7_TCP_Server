using System;
using System.Text;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace Ch7_TCP_Server
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        TcpListener Server;
        Socket Client;
        Thread Th_Svr;
        Thread Th_Clt;
        Hashtable HT = new Hashtable();

        //顯示本機IP
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this.Title += " " + MyIP();
            txtBoxIP.Text = MyIP();
            txtBoxIP.IsEnabled = false;
        }

        //找出本機IP
        private string MyIP()
        {
            string hn = Dns.GetHostName();
            IPAddress[] ip = Dns.GetHostEntry(hn).AddressList;
            string ipInfo = "";
            long start = IP2Long("192.168.1.1");
            long end = IP2Long("192.168.1.255");
            long ipAddr;
            foreach (IPAddress it in ip)
            {
                if (it.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipInfo = Convert.ToString(it);
                    ipAddr = IP2Long(ipInfo);
                    bool inRange = (ipAddr >= start && ipAddr <= end);
                    if (inRange)
                        return ipInfo;
                }
            }
            return ipInfo;
        }

        //將IP轉換成長整數long
        public static long IP2Long(string ip)
        {
            string[] ipBytes;
            double num = 0;
            if (!string.IsNullOrEmpty(ip))
            {
                ipBytes = ip.Split('.');
                for (int i = ipBytes.Length - 1; i >= 0; i--)
                {
                    num += ((int.Parse(ipBytes[i]) % 256) * Math.Pow(256, (3 - i)));
                }
            }
            return (long)num;
        }

        //啟動監聽連線要求
        private void btnServerStart_Click(object sender, RoutedEventArgs e)
        {
            Th_Svr = new Thread(ServerSub);
            Th_Svr.IsBackground = true;
            Th_Svr.Start();
            btnServerStart.IsEnabled = false;
        }

        //接受客戶連線要求的程式,針對每一個客戶會建立一個連線,以及獨立執行緒
        private void ServerSub()
        {
            string ip = "0.0.0.0";
            string port = "0";

            Dispatcher.Invoke(new Action(() =>
            {
                ip = txtBoxIP.Text;
                port = txtBoxPort.Text;
            })); //用Dispatcher.Invoke(同步)來確定拿到Port值才繼續後面程序

            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt16(port)); //Server IP和Port
            Server = new TcpListener(EP); //建立伺服器監聽器(總機)
            Server.Start(100); //啟動監聽設定,允許最多連線數100人
            while (true)
            {
                Client = Server.AcceptSocket(); //建立此客戶的連線物件Client
                Th_Clt = new Thread(Listen); //建立監聽這個客戶的獨立執行緒
                Th_Clt.IsBackground = true; //設定為背景執行緒
                Th_Clt.Start(); //開始執行緒的運作
            }
        }

        //監聽客戶訊息的程式
        private void Listen()
        {
            Socket sck = Client; //複製Client通訊物件到個別客戶專用物件sck
            Thread Th = Th_Clt; //複製執行緒Th_Clt到區域變數Th
            while (true) //持續監聽客戶傳來的訊息
            {
                try //用sck來接收此客戶訊息,inLen是接收訊息的byte數目
                {
                    byte[] B = new byte[1023]; //建立接收資料用的陣列,長度需大於可能的訊息
                    int inLen = sck.Receive(B); //接收網路資訊(Byte陣列)
                    //翻譯實際訊息(長度inLen)
                    string Msg = Encoding.Default.GetString(B, 0, inLen);
                    string Cmd = Msg.Substring(0, 1); //取出命令碼(第一個字)
                    string Str = Msg.Substring(1); //取出命令碼之後的訊息
                    switch (Cmd)
                    {
                        case "0":
                            HT.Add(Str, sck);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                listBox.Items.Add(Str);
                            }));
                            SendAll(OnlineList());
                            break;

                        case "9":
                            HT.Remove(Str);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                listBox.Items.Remove(Str);
                            }));
                            SendAll(OnlineList());
                            Th.Abort();
                            break;

                        case "1":
                            SendAll(Msg);
                            break;
                        default:
                            string[] c = Str.Split('|');
                            SendTo(Cmd + c[0], c[1]);
                            break;
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        //建立線上名單
        private string OnlineList()
        {
            string L = "L";
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                L += listBox.Items[i];
                if (i < listBox.Items.Count - 1)
                    L += ",";
            }
            return L;
        }

        //傳送訊息給指定客戶
        private void SendTo(string Str,string User)
        {
            byte[] B = Encoding.Default.GetBytes(Str);
            Socket Sck = (Socket)HT[User];
            Sck.Send(B, 0, B.Length, SocketFlags.None);
        }

        //傳送訊息給所有客戶
        private void SendAll(string Str)
        {
            byte[] B = Encoding.Default.GetBytes(Str);
            foreach(Socket s in HT.Values) s.Send(B, 0, B.Length, SocketFlags.None);
        }

        //關閉視窗時
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown(); //關閉所有執行緒
        }
    }
}
