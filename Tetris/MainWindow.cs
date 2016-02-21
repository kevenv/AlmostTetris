using System;
using System.Windows.Forms;
using System.Drawing;

namespace Tetris
{
    public partial class MainWindow : Form
    {
        private GamePanel gamePanel;
        private SpeedChangerWindow speedChangerW;

        public MainWindow()
        {
            InitializeComponent();

            gamePanel = new GamePanel();
            gamePanel.Location = new System.Drawing.Point(0, menu.Location.Y+menu.Height);
            gamePanel.Size = new System.Drawing.Size(GamePanel.GAME_PIXEL_WIDTH, GamePanel.GAME_PIXEL_HEIGHT);
            gamePanel.BackColor = Color.Black;
            gamePanel.Name = "GamePanel";
            gamePanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            Controls.Add(gamePanel);

            speedChangerW = new SpeedChangerWindow(this);

            openFileDialog.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            openFileDialog.Filter = "Tetris Replay Save|*.trs|Text files|*.txt";
            openFileDialog.DefaultExt = "trs";
            openFileDialog.AddExtension = true;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FileName = "";
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            gamePanel.stop();
            gamePanel.Dispose();
            Dispose();
        }

        public void setScore(int score)
        {
            this.scoreText.Text = "" + score;
        }

        public void setReplaySpeed(int value)
        {
            gamePanel.setReplaySpeed(value);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();

            if (openFileDialog.FileName != "") {
                gamePanel.playReplay(openFileDialog.FileName);
            }

            openFileDialog.FileName = "";
        }

        private void changeSpeedMenu_Click(object sender, EventArgs e)
        {
            speedChangerW.ShowDialog();
        }
    }
}
