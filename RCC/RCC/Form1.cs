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


namespace RCC
{
    public partial class Form1 : Form
    {
        string message = "";    //ログ表示する1行

        BackgroundWorker works;
        
        public class anCommand{
            public string targetIP;    //IPアドレス
            public string portNum;     //ポート番号
            public string commandNumber;   //コマンド行
        }

        int progress = 0;



        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初期処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            bgWorker.WorkerReportsProgress = true;  //進ちょく報告を可能に
            bgWorker.WorkerSupportsCancellation = true; //キャンセルを可能に

            bgWorker.RunWorkerAsync(100);
        }



        /// <summary>
        ///  時間のかかる処理を行うメソッド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 別スレッドで実行されるため、このメソッドでは
            // UI（コントロール）を操作してはいけない

            // このメソッドへのパラメータ
            int bgWorkerArg = (int)e.Argument;

            // senderの値はbgWorkerの値と同じ
            BackgroundWorker worker = (BackgroundWorker)sender;
            works = worker;


            progress += 10;
            message = "起動しました";
            worker.ReportProgress(progress);   //進捗報告

            ArrayList coms = loadIni();  //ini読み込み

            progress += 10;
            message = "ini読み込み完了";
            worker.ReportProgress(progress);   //進捗報告

            sendCommandsToServer(coms); //コマンドを転送

            message = "すべて完了";
            worker.ReportProgress(100);   //進捗報告


            // このメソッドからの戻り値
            e.Result = "すべて完了";

            // この後、RunWorkerCompletedイベントが発生
        }


        /// <summary>
        /// 進ちょく報告された時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Text = e.ProgressPercentage + "％完了";
            progressBar.Value = e.ProgressPercentage;

            if (message.Length > 1)
            {
                txtLogs.Text += message + "\r\n";
            }
        }


        /// <summary>
        /// bgWorkerが完了したときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 処理結果の表示
            this.Text = e.Result.ToString();
            //MessageBox.Show("正常に完了");

            progressBar.Value = 100;

            Application.Exit();
        }






        /// <summary>
        /// iniファイルを読み込む
        /// </summary>
        private ArrayList loadIni()
        {
            ArrayList commandList = new ArrayList();

            // StreamReader の新しいインスタンスを生成する
            System.IO.StreamReader cReader = (
                new System.IO.StreamReader(@"settings.ini", System.Text.Encoding.Default)
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


                    string[] stArrayData = stBuffer.Split(',');

                    if (stArrayData.Length > 2)
                    {

                        //targetIPs.Add(stArrayData[0]);
                        //commandNumbers.Add(stArrayData[1]);

                        anCommand ancom = new anCommand();
                        ancom.targetIP = stArrayData[0];
                        ancom.portNum = stArrayData[1];
                        ancom.commandNumber = stArrayData[2];

                        //MessageBox.Show(stArrayData[0] + "/" + stArrayData[1] + "/" + stArrayData[2]);

                        commandList.Add(ancom);
                    }
                    else
                    {
                        MessageBox.Show("settings.iniの書式が変です.");
                    }
                }
            }

            // cReader を閉じる (正しくは オブジェクトの破棄を保証する を参照)
            cReader.Close();

            return commandList;
        }


        /// <summary>
        /// 複数のコマンド転送を実施
        /// </summary>
        /// <param name="coms"></param>
        private void sendCommandsToServer(ArrayList coms)
        {
            for (int i = 0; i < coms.Count; i++)
            {

                

                anCommand com = (anCommand)coms[i];

                progress += 10;
                message = "IP:" + com.targetIP + " PORT:" + com.portNum + " Command:" + com.commandNumber + " を接続開始";
                works.ReportProgress(progress);   //進捗報告


                connectToServer(com.targetIP, com.portNum, com.commandNumber);


            }
        }

        private void connectToServer(string address,string portNum,string command)
        {
            try
            {
                //サーバーのホスト名とポート番号
                string host = address;
                int port = int.Parse(portNum);

                //TcpClientを作成し、サーバーと接続する
                System.Net.Sockets.TcpClient tcp =
                    new System.Net.Sockets.TcpClient(host, port);
                Console.WriteLine("サーバー({0}:{1})と接続しました({2}:{3})。",
                    ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Address,
                    ((System.Net.IPEndPoint)tcp.Client.RemoteEndPoint).Port,
                    ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Address,
                    ((System.Net.IPEndPoint)tcp.Client.LocalEndPoint).Port);

                //NetworkStreamを取得する
                System.Net.Sockets.NetworkStream ns = tcp.GetStream();

                //サーバーにデータを送信する
                //送信するデータを入力
                //string sendMsg = Console.ReadLine();            
                string sendMsg = command;

                //何も入力されなかった時は切断する
                if (sendMsg == null || sendMsg.Length == 0)
                {
                    tcp.Close();
                    ns.Close();
                    return;
                }
                //文字列をByte型配列に変換
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                byte[] sendBytes = enc.GetBytes(sendMsg);
                //データを送信する
                ns.Write(sendBytes, 0, sendBytes.Length);
                Console.WriteLine(sendMsg);

                //サーバーから送られたデータを受信する
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                do
                {
                    //データの一部を受信する
                    int resSize = ns.Read(resBytes, 0, resBytes.Length);
                    //Readが0を返した時はサーバーが切断したと判断
                    if (resSize == 0)
                    {
                        Console.WriteLine("サーバーが切断しました。");
                        break;
                    }
                    //受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);
                } while (ns.DataAvailable);
                //受信したデータを文字列に変換
                string resMsg = enc.GetString(ms.ToArray());
                ms.Close();
                Console.WriteLine(resMsg);

                //閉じる
                ns.Close();
                tcp.Close();
                Console.WriteLine("切断しました。");

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                printLog(ex.Message);
            }
        }


        /// <summary>
        /// キャンセルボタンを押したとき
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            works.CancelAsync();
            progressBar.Value = 0;
            this.Text = "Remote Command Client";
        }


        /// <summary>
        /// txtLogsにログを追記表示する。
        /// </summary>
        /// <param name="stLog"></param>
        private void printLog(string stLog)
        {
            message = stLog;
            bgWorker.ReportProgress(0);
        }
    }
}
