using System;
using System.Windows.Forms;

namespace Tetris
{
    public partial class SpeedChangerWindow : Form
    {
        private MainWindow mainWindow;

        public SpeedChangerWindow(MainWindow parent)
        {
            InitializeComponent();
            speedSelector.Items.Add(8);
            speedSelector.Items.Add(6);
            speedSelector.Items.Add(4);
            speedSelector.Items.Add(2);
            speedSelector.Items.Add(1);
            speedSelector.Items.Add(-2);
            speedSelector.Items.Add(-4);
            speedSelector.Items.Add(-6);
            speedSelector.Items.Add(-8);

            speedSelector.SelectedIndex = 4;

            mainWindow = parent;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            int value = (int)speedSelector.Items[speedSelector.SelectedIndex];
            mainWindow.setReplaySpeed(value);
            Close();
        }
    }
}
