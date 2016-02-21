using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;

namespace Tetris
{
    public partial class GamePanel : Control
    {
        public const int BOX_SIZE = 18;
        public const int GAME_WIDTH = 10;
        public const int GAME_HEIGHT = 20;
        public const int GAME_PIXEL_WIDTH = GAME_WIDTH * BOX_SIZE;
        public const int GAME_PIXEL_HEIGHT = GAME_HEIGHT * BOX_SIZE;

        private const float fps = 30;
        private static System.Windows.Forms.Timer frameTimer;

        private static Game game;
        private static Thread thread;
        private static bool quit;

        private Image screen;
        private static SolidBrush solidBrush = new SolidBrush(Color.Gray);
        private static Pen grayPen = new Pen(solidBrush);

        private Image[] tiles;
        private Color[] colors = new Color[8];
        private Image[, ,] blocsImage;
        private bool textureFound;
        private const string tilesFileName = "tiles.bmp";

        public GamePanel()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.KeyDown += new KeyEventHandler(GamePanel_KeyDown);
            this.PreviewKeyDown += new PreviewKeyDownEventHandler(GamePanel_PreviewKeyDown);
            this.MouseClick += new MouseEventHandler(GamePanel_MouseClick); //TODO: temp: debug

            game = new Game();

            screen = null;
            textureFound = true;

            loadTextures();
            //preDrawBlocs();

            Focus();
            start();
            initTimer();
        }

        private void loadTextures()
        {
            System.Console.WriteLine("Loading textures...");

            Image tileSet = null;
            try {
                tileSet = Image.FromFile(tilesFileName);
            }
            catch (System.IO.FileNotFoundException) {
                System.Console.WriteLine("Warning: Can't load image '" + tilesFileName + "'");
                textureFound = false;
                fillColors();
            }

            generateTiles(tileSet);
        }

        private void generateTiles(Image tileSet)
        {
            System.Console.WriteLine("Generating tiles...");
            if (textureFound) {
                System.Console.WriteLine("tileset: " + tileSet.Width + " x " + tileSet.Height + " (" + BOX_SIZE + " x " + BOX_SIZE + ")" +
                                         " #tiles: " + tileSet.Width / BOX_SIZE);
            }
            tiles = new Image[game.nbBlocs + 1];

            for (int i = 0; i < game.nbBlocs + 1; i++) {
                tiles[i] = new Bitmap(BOX_SIZE, BOX_SIZE);
                System.Console.Write("tile[" + i + "]\t");
                using (Graphics g = Graphics.FromImage(tiles[i])) {
                    if (textureFound) {
                        System.Console.WriteLine("tile: " + i * BOX_SIZE + " x 0");
                        g.DrawImage(tileSet, new Rectangle(0, 0, BOX_SIZE, BOX_SIZE),
                                             new Rectangle(i * BOX_SIZE, 0, BOX_SIZE, BOX_SIZE), GraphicsUnit.Pixel);
                    }
                    else {
                        System.Console.WriteLine("color: " + colors[i]);
                        Brush brush = new SolidBrush(colors[i]);
                        g.FillRectangle(brush, 0, 0, BOX_SIZE, BOX_SIZE);
                        brush.Dispose();
                    }
                }
            }
        }

        private void fillColors()
        {
            colors[0] = Color.Black;
            colors[1] = Color.Orange;
            colors[2] = Color.Blue;
            colors[3] = Color.Red;
            colors[4] = Color.LightGreen;
            colors[5] = Color.LightBlue;
            colors[6] = Color.Yellow;
            colors[7] = Color.Purple;
        }

        public void initTimer()
        {
            frameTimer = new System.Windows.Forms.Timer();
            frameTimer.Tick += new EventHandler(timerTick);
            frameTimer.Interval = (int)(1.0 / fps * 1000);
            frameTimer.Start();
        }

        private void timerTick(object sender, EventArgs e)
        {
            Refresh(); //refresh screen each X seconds
        }

        public void playReplay(string fileName)
        {
            game.loadReplay(fileName);
            game.playReplay();
            start();
        }

        public void setReplaySpeed(int value)
        {
            game.setReplaySpeed(value);
        }

        public static void run()
        {
            //System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            //int ticks = 0;

            //timer.Start();

            while (!GamePanel.quit && !game.gameOver) {
                //if (timer.Elapsed.Seconds >= 1) {
                //    timer.Stop();
                //    System.Console.WriteLine(ticks/timer.Elapsed.Seconds);
                //    ticks = 0;
                //    timer.Restart();
                //}

                game.tick();
                //ticks++;
                Thread.Sleep(10);
            }

            thread = null;
            System.Console.WriteLine("finish");
        }

        public static void start()
        {
            if (thread == null) {
                thread = new Thread(new ThreadStart(run));
                thread.Start();
                System.Console.WriteLine("start");
            }
        }

        public void stop()
        {
            System.Console.WriteLine("STOP");

            GamePanel.quit = true;
            grayPen.Dispose();
            solidBrush.Dispose();
            //foreach(Image img in blocsImage) {
            //    img.Dispose();
            //}
            game.saveReplayFile();
        }

        void GamePanel_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Keys key = e.KeyCode;
            if (key == Keys.Left ||
                key == Keys.Right ||
                key == Keys.Up ||
                key == Keys.Down ||
                key == Keys.Space ||
                key == Keys.R)
                e.IsInputKey = true;  //for some weird reason keyEvent is bugged...
        }

        void GamePanel_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode) {
            case Keys.Left:
                game.actions.moveX--;
                game.addReplay("left");
                System.Console.WriteLine("left");
                break;
            case Keys.Right:
                game.actions.moveX++;
                game.addReplay("right");
                System.Console.WriteLine("right");
                break;
            case Keys.Up:
                game.actions.rotate = true;
                game.addReplay("rotate");
                System.Console.WriteLine("rotate");
                break;
            case Keys.Down:
                game.actions.moveY++;
                game.addReplay("down");
                System.Console.WriteLine("down");
                break;
            case Keys.Space:
                game.actions.drop = true;
                game.addReplay("drop");
                System.Console.WriteLine("drop");
                break;
            case Keys.R:
                game.restart();
                start();
                System.Console.WriteLine("restart");
                break;
            }
        }

        void GamePanel_MouseClick(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Left)
            //    game.setBloc(e.X / BOX_SIZE, e.Y / BOX_SIZE, 3);//Color.LightGreen);
            //else if (e.Button == MouseButtons.Right)
            //    game.delBloc(e.X / BOX_SIZE, e.Y / BOX_SIZE);
            //game.gridChanged = true;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            //base.OnPaint(pe);
            Graphics g = pe.Graphics;

            drawGrid(g);
            drawBloc(g, game.bloc, false);

            //draw shadow bloc
            Bloc shadowBloc = new Bloc(game.bloc);
            shadowBloc.posX = game.shadowX;
            shadowBloc.posY = game.shadowY;
            drawBloc(g, shadowBloc, true);

            drawGameOver(g);
            drawReplayFinish(g);

            //draw score
            MainWindow parent = (MainWindow)this.Parent;
            parent.setScore(game.score);
        }

        private void drawGameOver(Graphics g)
        {
            if (game.gameOver && !game.replayFinish) {
                Font font = new Font("Arial", 20);
                SolidBrush brush = new SolidBrush(Color.White);
                StringFormat format = new StringFormat();

                g.DrawString("GAME OVER", font, brush, 3, GAME_PIXEL_HEIGHT / 2 - 20, format);

                font.Dispose();
                brush.Dispose();
                format.Dispose();
            }
        }

        private void drawReplayFinish(Graphics g)
        {
            if (game.replayFinish) {
                int policeSize = 20;
                Font font = new Font("Arial", policeSize);
                SolidBrush brush = new SolidBrush(Color.White);
                StringFormat format = new StringFormat();

                string s1 = "REPLAY";
                int x1 = GAME_PIXEL_WIDTH/2 - (s1.Length*20)/2 + 2;
                int y1 = GAME_PIXEL_HEIGHT / 2 - policeSize;
                string s2 = "FINISH";
                int x2 = GAME_PIXEL_WIDTH/2 - (s2.Length*20)/2 + 10;
                int y2 = y1 + (2*policeSize + 5);
                g.DrawString(s1, font, brush, x1, y1-20, format);
                g.DrawString(s2, font, brush, x2, y2-20, format);
                font.Dispose();
                brush.Dispose();
                format.Dispose();
            }
        }

        private void drawGrid(Graphics g)
        {
            if (game.gridChanged) {
                screen = new Bitmap(GAME_PIXEL_WIDTH, GAME_PIXEL_HEIGHT);
                using (Graphics gfx = Graphics.FromImage(screen)) {
                    for (int y = 0; y < GAME_HEIGHT; y++) {
                        for (int x = 0; x < GAME_WIDTH; x++) {
                            gfx.DrawImage(tiles[game.grid[y, x]], x * BOX_SIZE, y * BOX_SIZE);
                        }
                    }
                }

                game.gridChanged = false;
            }

            g.DrawImage(screen, 0, 0);
        }

        private void drawBloc(Graphics g, Bloc bloc, bool shadow)
        {
            //System.Console.WriteLine(bloc.posX + "," + bloc.posY);
            //if (bloc.posX < 0 || bloc.posY < 0) return;
            //g.DrawImage(blocsImage[bloc.textureID-1, bloc.angle, shadow?1:0], bloc.posX*BOX_SIZE, bloc.posY*BOX_SIZE);

            for (int y = 0; y < bloc.getShape().size; y++) { //draw each bloc forming the shape
                for (int x = 0; x < bloc.getShape().size; x++) {
                    if (bloc.getShape().shape[y, x] == 1) { //if bloc -> draw it
                        if (shadow) {
                            g.DrawRectangle(grayPen, (bloc.posX + x) * BOX_SIZE, (bloc.posY + y) * BOX_SIZE, BOX_SIZE, BOX_SIZE);
                        }
                        else {
                            g.DrawImage(tiles[bloc.blocID], (bloc.posX + x) * BOX_SIZE, (bloc.posY + y) * BOX_SIZE);
                        }
                    }
                }
            }
        }

        //TODO: bugged
        private void preDrawBlocs()
        {
            blocsImage = new Image[game.nbBlocs - 1, Bloc.NB_ROTATIONS, 2];

            for (int i = 0; i < game.nbBlocs - 1; i++) {
                for (int angle = 0; angle < Bloc.NB_ROTATIONS; angle++) {
                    blocsImage[i, angle, 0] = preDrawBloc(i, angle, false);
                    blocsImage[i, angle, 1] = preDrawBloc(i, angle, true);
                }
            }
        }

        private Image preDrawBloc(int textureID, int angle, bool shadow)
        {
            Bloc bloc = game.getBloc(textureID);
            Shape shape = bloc.getShape(angle);
            Image image = new Bitmap(shape.width * BOX_SIZE, shape.height * BOX_SIZE);

            using (Graphics gfx = Graphics.FromImage(image)) {
                for (int y = 0; y < shape.height; y++) { //draw each bloc forming the shape
                    for (int x = 0; x < shape.width; x++) {
                        int absPosX = x + shape.originX;
                        int absPosY = y + shape.originY;
                        if (shape.shape[absPosY, absPosX] == 1) { //if bloc -> draw it
                            if (shadow) {
                                gfx.DrawRectangle(grayPen, x * BOX_SIZE, y * BOX_SIZE, BOX_SIZE, BOX_SIZE);
                            }
                            else {
                                gfx.DrawImage(tiles[bloc.blocID], x * BOX_SIZE, y * BOX_SIZE);
                            }
                        }
                    }
                }
                gfx.DrawImage(image, 0, 0);
                //System.Console.WriteLine(image.Size.Width + "x" + image.Size.Height);
                //image.Save("test" + textureID + "_" + angle + "_" + shadow + ".bmp");
            }

            return image;
        }
    }
}
