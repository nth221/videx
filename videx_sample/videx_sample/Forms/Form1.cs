using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;

namespace videx_sample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
     
        string selectedFilePath;


        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }


        private void button_open_Click(object sender, EventArgs e)
        {
             OpenFileDialog openFileDialog = new OpenFileDialog();

            // 파일 필터를 설정하여 특정 유형의 파일만 선택할 수 있도록
            openFileDialog.Filter = "Video Files|*.mp4;*.avi|All Files|*.*";
            openFileDialog.Title = "Select a Video File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 사용자가 선택한 파일의 경로
                selectedFilePath = openFileDialog.FileName;

                // 여기에 파일을 로드하는 코드 추가
                axWindowsMediaPlayer1.URL = selectedFilePath;
                // selectedFilePath를 사용하여 선택한 파일에 대한 작업을 수행
                MessageBox.Show("Selected File: " + selectedFilePath);
            }
        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void axWindowsMediaPlayer1_Enter(object sender, EventArgs e)
        {

           /* VideoCapture video = new VideoCapture(selectedFilePath);
            Mat frame = new Mat();
            while (video.PosFrames != video.FrameCount)
            {
                video.Read(frame);
                Cv2.ImShow("frame", frame);
                Cv2.WaitKey(26);
            }

            frame.Dispose();
            video.Release();
            Cv2.DestroyAllWindows();*/ // parser video 용
        }
    }
}
