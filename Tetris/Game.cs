using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel; 

namespace Tetris
{
    public class Game
    {
        public int[,] grid { get; private set; }

        public int nbShapes { get; private set; }
        private Shape[] shapes;

        public int nbBlocs { get; private set; }
        private Bloc[] blocs;

        public Bloc bloc { get; private set; }

        public int shadowX { get; private set; }
        public int shadowY { get; private set; }

        private int oldX;
        private int oldY;

        public Actions actions;

        public bool gridChanged;
        public bool gameOver { get; private set; }
        public int score { get; private set; }

        private Random randGen;
        private System.Diagnostics.Stopwatch dropTimer;

        private System.Diagnostics.Stopwatch comboTimer;
        private int nbCombos;
        public const int COMBO_DELAY = 2; //seconds
        public const int COMBO_SCORE = 10;

        private bool replayMode;
        private bool saveReplay;
        public bool replayFinish;
        private ReplaySave replaySave;
        private long ticks;

        public Game()
        {
            System.Console.WriteLine("--- AlmostTetris v0.1 ---");
            grid = new int[GamePanel.GAME_HEIGHT, GamePanel.GAME_WIDTH];
            
            nbBlocs = 7;
            nbShapes = 7;

            fillShapes();
            buildBlocs();

            randGen = new Random();
            dropTimer = new System.Diagnostics.Stopwatch();
            comboTimer = new System.Diagnostics.Stopwatch();

            replayMode = false;
            saveReplay = true;
            replaySave = new ReplaySave("c:\\test.trs");

            restart();
        }

        public void restart()
        {
            System.Console.WriteLine("* new game");
            for (int y = 0; y < GamePanel.GAME_HEIGHT; y++) {
                for (int x = 0; x < GamePanel.GAME_WIDTH; x++) {
                    grid[y, x] = 0;
                }
            }

            gameOver = false;
            gridChanged = true;

            score = 0;
            nbCombos = 0;
            comboTimer.Stop(); //probably can remove
            comboTimer.Reset();

            shadowX = 0;
            shadowY = 0;

            oldX = 0;
            oldY = 0;

            dropTimer.Restart();

            initInput();

            ticks = 0;
            replaySave.init();
            replayFinish = false;

            if (bloc != null) {
                bloc.init();
            }
            genNewBloc();
        }

        private void initInput()
        {
            actions.moveX = 0;
            actions.moveY = 0;
            actions.rotate = false;
            actions.drop = false;
        }

        private void buildBlocs()
        {
            System.Console.WriteLine("* Generating blocs...");
            blocs = new Bloc[nbBlocs];

            blocs[0] = new Bloc(shapes[0], 1);
            blocs[1] = new Bloc(shapes[1], 2);
            blocs[2] = new Bloc(shapes[2], 3);
            blocs[3] = new Bloc(shapes[3], 4);
            blocs[4] = new Bloc(shapes[4], 5);
            blocs[5] = new Bloc(shapes[5], 6);
            blocs[6] = new Bloc(shapes[6], 7);
        }

        private void fillShapes()
        {
            System.Console.WriteLine("* Generating shapes...");
            shapes = new Shape[nbShapes];

            shapes[0] = new Shape(new int[3,3] { {0,0,1},
                                                 {1,1,1},
                                                 {0,0,0} });
            shapes[1] = new Shape(new int[3,3] { {1,1,0},
                                                 {0,1,1},
                                                 {0,0,0} });
            shapes[2] = new Shape(new int[3,3] { {0,1,1},
                                                 {1,1,0},
                                                 {0,0,0} });
            shapes[3] = new Shape(new int[4,4] { {1,1,1,1},
                                                 {0,0,0,0},
                                                 {0,0,0,0},
                                                 {0,0,0,0} });
            shapes[4] = new Shape(new int[2,2] { {1,1},
                                                 {1,1} });
            shapes[5] = new Shape(new int[3,3] { {0,1,0},
                                                 {1,1,1},
                                                 {0,0,0} });
            shapes[6] = new Shape(new int[3,3] { {1,0,0},
                                                 {1,1,1},
                                                 {0,0,0} });
        }

        public void tick()
        {
            if (replayMode) {
                loadNextInputs();
            }

            oldX = bloc.posX;
            oldY = bloc.posY;

            //drop
            genShadow();

            if (actions.drop) {
                dropBloc();
                genNewBloc(bloc.posX, bloc.posY);
                actions.drop = false;
            }
            else {
                //rotate
                rotateBloc();

                //x collide
                if (actions.moveX > 0 && bloc.posX + bloc.getOriginX() < (GamePanel.GAME_WIDTH - bloc.getShape().width)) {
                    bloc.posX++;
                }
                else if (actions.moveX < 0 && bloc.posX + bloc.getOriginX() > 0) {
                    bloc.posX--;
                }

                //gravity
                //bloc.dropSpeed //box/1 tick
                //box/tick -> box/seconds
                //1 second ~ 1 tick

                if (dropTimer.ElapsedMilliseconds > (1 / bloc.dropSpeed * 1000)) {
                    actions.moveY++;
                    //score += 1;
                    dropTimer.Restart();
                }

                bloc.posY += actions.moveY;

                //y collide
                if (bloc.posY + bloc.getOriginY() <= (GamePanel.GAME_HEIGHT - bloc.getShape().height)) {
                    if (isCollide(bloc.posX, bloc.posY)) {
                        if (bloc.posY == 0) {
                            System.Console.WriteLine("* game over");
                            gameOver = true;
                            dropTimer.Stop();
                        }
                        else if (actions.moveY > 0) {
                            genNewBloc(oldX, oldY);
                        }
                        else {
                            bloc.posY = oldY;
                            bloc.posX = oldX;
                            System.Console.Write("[INVALIDE Y MOVE]");
                        }
                    }
                }
                else { //if last line
                    genNewBloc(oldX, oldY);
                }
            }

            if (comboTimer.IsRunning && comboTimer.Elapsed.Seconds > COMBO_DELAY) {
                System.Console.WriteLine("* Combo: C-C-C-COMBO BREAKER!");
                nbCombos = 0;
                comboTimer.Stop();
                comboTimer.Reset();
            }

            initInput();
            ticks++;
        }

        private void rotateBloc()
        {
            if (actions.rotate) {
            	System.Console.WriteLine("* Rotating bloc... [" + bloc.angle + "]");

                int oldAngle = bloc.angle;
                actions.rotate = false;
                bloc.rotate();

                //wall-kick
                if (bloc.posX + bloc.getOriginX() > (GamePanel.GAME_WIDTH - bloc.getShape().width)) {
                    bloc.posX -= (bloc.getShape().size - (bloc.getOriginY() + (bloc.getShape().height - 1))) - 1;
                }
                else if (bloc.posX + bloc.getOriginX() < 0) {
                    bloc.posX += bloc.getOriginY();
                }

                if (bloc.posY + bloc.getOriginY() > (GamePanel.GAME_HEIGHT - bloc.getShape().height)) {
                    bloc.posY -= bloc.getOriginX();
                }

                //rotate collide todo: block kick
                if (isCollide(bloc.posX, bloc.posY)) {
                    System.Console.WriteLine("[INVALID MOVE]");
                    bloc.posX = oldX;
                    bloc.posY = oldY;
                    bloc.setAngle(oldAngle);
                }
            }
        }

        private void genNewBloc(int posX, int posY)
        {
            System.Console.WriteLine("* Choosing bloc...");

            lockBloc(posX, posY);
            destroyFullLines();
            score += 2;
            bloc.init();
            genNewBloc();
        }

        private void genNewBloc()
        {
            int id;
            if (replayMode && (replaySave.currentBlocID != replaySave.blocsIDCount - 1)) {
                id = replaySave.getNextBlocID();
            }
            else {
                id = randGen.Next(0, nbBlocs);
            }
            bloc = blocs[id];

            if (saveReplay) {
                replaySave.addBlocID(id);
            }
        }

        private void destroyFullLines()
        {
            System.Console.WriteLine("* Clearing line...");
       
            for(int y = bloc.posY; y < GamePanel.GAME_HEIGHT; y++) {
                if (isLineFull(y)) {
                    System.Console.WriteLine("[Line " + y + " is FULL]");
                    dropLines(y);
                    doCombo();
                    gridChanged = true;
                }
            }
        }

        private void dropLines(int lineID)
        {
            System.Console.WriteLine("[Lines < " + lineID + " dropped]");

            for (int y = lineID; y > 0; y--) {
                for (int x = 0; x < GamePanel.GAME_WIDTH; x++) {
                    grid[y, x] = grid[y - 1, x];
                }
            }
        }

        private bool isLineFull(int y)
        {
            for (int x = 0; x < GamePanel.GAME_WIDTH; x++) {
                if (grid[y, x] == 0) {
                    return false;
                }
            }
            return true;
        }

        private void lockBloc(int posX, int posY) //optimize : shape.originXY -> if optimizaation work, might be able to remove isInGameArea in isBlocHere
        {
            System.Console.WriteLine("* Locking bloc...");

            for (int y = 0; y < bloc.getShape().height; y++) {
                for (int x = 0; x < bloc.getShape().width; x++) {
                    int absPosX = x + bloc.getShape().originX;
                    int absPosY = y + bloc.getShape().originY;
                    if (bloc.getShape().shape[absPosY, absPosX] == 1) {
                        grid[posY + absPosY, posX + absPosX] = bloc.blocID;
                    }
                }
            }
            gridChanged = true;
        }

        private bool isCollide(int posX, int posY)
        {
            for (int y = 0; y < bloc.getShape().height; y++) {
                for (int x = 0; x < bloc.getShape().width; x++) {
                    int absPosX = x + bloc.getShape().originX;
                    int absPosY = y + bloc.getShape().originY;
                	if (isBlocHere(posX + absPosX, posY + absPosY) && bloc.getShape().shape[absPosY,absPosX] == 1) {
                		return true;
                    }
                }
            }

            return false;
        }

        private bool isBlocHere(int x, int y)
        {
        	return isInGameArea(x,y) && grid[y,x] != 0;
        }

        private static bool isInGameArea(int x, int y)
        {
            return x >= 0 && x < GamePanel.GAME_WIDTH &&
                   y >= 0 && y < GamePanel.GAME_HEIGHT;
        }

        private bool isAtLastLine(int y)
        {
        	return y > (GamePanel.GAME_HEIGHT - bloc.getShape().height);
        }

        private void genShadow()
        {
            shadowX = bloc.posX;
            shadowY = bloc.posY;

            while (!isAtLastLine(shadowY + bloc.getOriginY()) && !isCollide(shadowX, shadowY)) {
                shadowY++;
            }
            shadowY--;
        }

        private void dropBloc()
        {
            System.Console.WriteLine("* Droping bloc...");
            bloc.posY = shadowY;
        }

        public void setBloc(int x, int y, int textureID)
        {
            grid[y,x] = textureID;
        }

        public void delBloc(int x, int y)
        {
            grid[y,x] = 0;
        }

        public Bloc getBloc(int id)
        {
            return blocs[id];
        }

        private void doCombo()
        {
            if (comboTimer.IsRunning) {
                System.Console.WriteLine("* Combo: !!! (x" + nbCombos + ")");
                comboTimer.Restart();
            }
            else {
                comboTimer.Start();
            }

            nbCombos++;
            score += nbCombos * COMBO_SCORE;
        }

        public void playReplay()
        {
            restart();
            replayMode = true;
        }

        public void loadReplay(string fileNamePath)
        {
            replayMode = true;
            replaySave.loadReplayBin(fileNamePath);
        }

        public void saveReplayFile()
        {
            if (saveReplay) {
                replaySave.saveReplayFileBin();
            }
        }

        private void loadNextInputs()
        {
            replaySave.loadNextInputs(ticks, ref actions);
            if (replaySave.replayFinish) {
                replayFinish = true;
                replayMode = false;
            }
        }

        public void addReplay(string action)
        {
            if (saveReplay) {
                replaySave.addEntry(ticks, action);
            }
        }

        public void setReplaySpeed(int value)
        {
            replaySave.setReplaySpeed(value);
        }
    }
}