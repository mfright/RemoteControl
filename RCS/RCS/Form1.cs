using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Collections; 

namespace RCS
{
    public partial class Form1 : Form
    {
        string message = "";    //ログ表示する1行

        public ArrayList commandsArray;    //commands.iniをロードしたもの。

        public class anCommand
        {
            public string commandString;    //コマンド
        }




        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bgWorker.WorkerReportsProgress = true;

            bgWorker.RunWorkerAsync(100);
        }


        /// <summary>
        /// バックグラウンド処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // iniをロード
            commandsArray = loadIni();
            printLog("iniをロードしました");

            //サーバ機能
            server();
        }

        /// <summary>
        /// 進ちょく報告された時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (txtLogs.Text.Length > 1000)
            {
                int begin = 1000;
                int leng = txtLogs.Text.Length - begin;
                if (txtLogs.Text.IndexOf(Environment.NewLine) != -1)
                {
                    txtLogs.Text = txtLogs.Text.Substring(begin, leng);
                }
            }

            txtLogs.Text += message + "\r\n";

            //カレット位置を末尾に移動
            txtLogs.SelectionStart = txtLogs.Text.Length;
            //テキストボックスにフォーカスを移動
            txtLogs.Focus();
            //カレット位置までスクロール
            txtLogs.ScrollToCaret();
        }


        /// <summary>
        /// 動作完了したとき
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }


        private void server()
        {
            for (; true; )
            {

                //IPv4とIPv6の全てのIPアドレスをListenする
                System.Net.Sockets.TcpListener listener =
                    new System.Net.Sockets.TcpListener(System.Net.IPAddress.IPv6Any, 2005);
                //IPv6Onlyを0にする
                listener.Server.SetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.IPv6,
                    System.Net.Sockets.SocketOptionName.IPv6Only,
                    0);

                //Listenを開始する
                listener.Start();

                printLog("サーバ待ち受け中");

                //接続要求があったら受け入れる
                System.Net.Sockets.TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("IPアドレス:{0} ポート番号:{1})。",
                    ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Address,
                    ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Port);


                printLog("接続を受けました。IP:" + ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Address
                    + " PORT:" + ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Port);

                
                //NetworkStreamを取得
                System.Net.Sockets.NetworkStream ns = client.GetStream();

                //クライアントから送られたデータを受信する
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                bool disconnected = false;
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                do
                {
                    //データの一部を受信する
                    int resSize = ns.Read(resBytes, 0, resBytes.Length);
                    //Readが0を返した時はクライアントが切断したと判断
                    if (resSize == 0)
                    {
                        disconnected = true;
                        Console.WriteLine("クライアントが切断しました。");
                        printLog("クライアントが切断しました。");
                        break;
                    }
                    //受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);
                } while (ns.DataAvailable);
                //受信したデータを文字列に変換
                string resMsg = enc.GetString(ms.ToArray());
                ms.Close();
                Console.WriteLine("Received Message:" + resMsg);
                printLog("コマンドを受信しました:"+resMsg);

                /*
                if (!disconnected)
                {
                    //クライアントにデータを送信する
                    //クライアントに送信する文字列を作成
                    string sendMsg = resMsg.Length.ToString();
                    //文字列をByte型配列に変換
                    byte[] sendBytes = enc.GetBytes(sendMsg);
                    //データを送信する
                    ns.Write(sendBytes, 0, sendBytes.Length);
                    Console.WriteLine("Send Message:" + sendMsg);
                } */

                //閉じる
                ns.Close();
                client.Close();
                Console.WriteLine("クライアントとの接続を閉じました。");

                //リスナを閉じる
                listener.Stop();
                Console.WriteLine("Listenerを閉じました。");


                runCommands(resMsg);    //受け取ったコマンドを実行する。
            }
        }


        /// <summary>
        /// txtLogsにログを追記表示する。
        /// </summary>
        /// <param name="stLog"></param>
        private void printLog(string stLog)
        {
            DateTime dtNow = DateTime.Now;

            message = dtNow.ToString() + " - " + stLog;
            bgWorker.ReportProgress(0);
        }

        /// <summary>
        /// 指定された行目のコマンドを実行する。
        /// </summary>
        /// <param name="commandNumber"></param>
        private void runCommands(string commandNumber)
        {
            int line = -1;
            if (int.TryParse(commandNumber, out line))
            {
                try
                {
                    
                    string cmdstr = ((anCommand)commandsArray[line - 1]).commandString;

                    //Processオブジェクトを作成
                    System.Diagnostics.Process p = new System.Diagnostics.Process();

                    //出力をストリームに書き込むようにする
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    //OutputDataReceivedイベントハンドラを追加
                    p.OutputDataReceived += p_OutputDataReceived;

                    p.StartInfo.FileName =
                        System.Environment.GetEnvironmentVariable("ComSpec");
                    p.StartInfo.RedirectStandardInput = false;
                    p.StartInfo.CreateNoWindow = true;
                    //p.StartInfo.Arguments = @"/c dir c:\ /w";
                    p.StartInfo.Arguments = @"/c "+cmdstr;

                    //起動
                    p.Start();

                    //非同期で出力の読み取りを開始
                    p.BeginOutputReadLine();

                    p.WaitForExit();
                    p.Close();

                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("指定の行目のコマンドを読むところでエラー");
                    Console.WriteLine(ex.Message);
                    
                }
            }
            else
            {
                printLog("数値でないコマンド行数を検知");
                Console.WriteLine("数値でないコマンド行数を検知");
            }
        }

        //OutputDataReceivedイベントハンドラ
        //行が出力されるたびに呼び出される
        static void p_OutputDataReceived(object sender,
            System.Diagnostics.DataReceivedEventArgs e)
        {
            //出力された文字列を表示する
            Console.WriteLine(e.Data);
        }




        /// <summary>
        /// iniをロードする
        /// </summary>
        /// <returns></returns>
        private ArrayList loadIni()
        {
            try
            {
                ArrayList commandList = new ArrayList();

                // StreamReader の新しいインスタンスを生成する
                System.IO.StreamReader cReader = (
                    new System.IO.StreamReader(@"commands.ini", System.Text.Encoding.Default)
                );

                // 読み込んだ結果をすべて格納するための変数を宣言する
                //string stResult = string.Empty;

                // 読み込みできる文字がなくなるまで繰り返す
                while (cReader.Peek() >= 0)
                {
                    // ファイルを 1 行ずつ読み込む
                    string stBuffer = cReader.ReadLine();
                    // 読み込んだものを追加で格納する
                    //stResult += stBuffer + System.Environment.NewLine;

                    if (stBuffer.Length > 2 && stBuffer.Substring(0, 2) != "//")
                    {




                        anCommand ancom = new anCommand();
                        ancom.commandString = stBuffer;

                        commandList.Add(ancom);

                    }
                }

                // cReader を閉じる (正しくは オブジェクトの破棄を保証する を参照)
                cReader.Close();

                return commandList;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Exit();
                return null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("終了すると遠隔操作を受け付けなくなります。終了しますか？", "確認", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
