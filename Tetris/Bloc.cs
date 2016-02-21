using System;
using System.Diagnostics;

namespace Tetris
{
    public class Shape
    {
        public int[,] shape { get; private set; } //might want to bool this instead of int

        public int size { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }

        public int originX { get; private set; }
        public int originY { get; private set; }

        public Shape(int[,] shape)
        {
            set(shape);
        }

        public void set(int[,] shape)
        {
            if (shape == null) {
                throw new TetrisException("Shape: Shape is null");
            }
            this.shape = shape;
            this.size = (int)Math.Sqrt((double)shape.Length);
            calcOrigin();
            calcDimensions();
        }

        private void calcDimensions()
        {
            int lastX = 0;
            int lastY = 0;
            bool found = false;

            for (int y = originY; y < size; y++) {
                for (int x = originX; x < size; x++) {
                    if (shape[y,x] == 1) {
                        if (x > lastX) {
                            lastX = x;
                        }
                        if (y > lastY) {
                            lastY = y;
                        }
                        found = true;
                    }
                }
            }

            if (!found) {
                throw new TetrisException("Shape: Shape is empty");
            }

            width = lastX - originX + 1;
            height = lastY - originY + 1;
        }

        private void calcOrigin()
        {
            originX = 2555555;
            originY = 2555555;

            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    if (shape[y,x] == 1) {
                        if (x < originX) {
                            originX = x;
                        }
                        if (y < originY) {
                            originY = y;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            string output = "";

            output += "(" + width + " x " + height + "): " + size + "\n";
            output += "origin: " + originX + "," + originY + "\n";

            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    output += shape[y, x] + " ";
                }
                output += "\n";
            }

            return output;
        }
    };

    public class Bloc
    {
        public int posX;
        public int posY;

        public const int NB_ROTATIONS = 4;
        public int angle { get; private set; }

        public float dropSpeed { get; private set; } // box/ticks

        public Shape[] shape { get; private set; }
        public int blocID { get; private set; } //between 1 and n (0 = not a bloc)

        public Bloc(Shape shape, int blocID)
        {
            if (shape == null) {
                throw new TetrisException("Bloc: Shape is null");
            }
            this.blocID = blocID;
            init();
            genShapes(shape);
        }

        public Bloc(Bloc bloc)
        {
            if (bloc == null) {
                throw new TetrisException("Bloc: Bloc is null");
            }
            posX = bloc.posX;
            posY = bloc.posY;

            angle = bloc.angle;

            dropSpeed = bloc.dropSpeed;

            this.shape = bloc.shape; //todo: probably gonna crash some times.. because its sharing references

            blocID = bloc.blocID;
        }

        public void init()
        {
            posX = 3;
            posY = 0;

            angle = 0;

            dropSpeed = 1f;
        }

        private void genShapes(Shape baseShape)
        {
            this.shape = new Shape[NB_ROTATIONS];
            this.shape[0] = new Shape(baseShape.shape);

            for (int i = 1; i < NB_ROTATIONS; i++) {
                shape[i] = rotateShape(shape[i-1]);
            }
        }

        private static Shape rotateShape(Shape shape)
        {
            int[,] newShape = new int[shape.size, shape.size];

            for (int y = 0; y < shape.size; y++) {
                for (int x = 0; x < shape.size; x++) {
                    //Rotation formula = (x,y) -> (nbRow-1-y,x)
                    newShape[x, shape.size - 1 - y] = shape.shape[y, x];
                }
            }

            return new Shape(newShape);
        }

        public void rotate()
        {
            if(angle < NB_ROTATIONS-1)
                angle++;
            else
                angle = 0;
        }

        public void setAngle(int angle)
        {
            checkAngle(angle);
        	this.angle = angle;
        }

        public Shape getShape()
        {
            return shape[angle];
        }

        public Shape getShape(int angle)
        {
            checkAngle(angle);
            return shape[angle];
        }

        public int getOriginX()
        {
            return shape[angle].originX;
        }

        public int getOriginY()
        {
            return shape[angle].originY;
        }

        public override string ToString()
        {
            string output = "";

            output += "(" + posX + "," + posY + ") a: " + angle + " s: " + dropSpeed + " t: " + blocID + "\n\n";
            for(int i = 0; i < NB_ROTATIONS; i++) {
                output += shape[i] + "\n";
            }

            return output;
        }

        private static void checkAngle(int angle)
        {
            Debug.Assert(angle >= 0 && angle <= NB_ROTATIONS);
        }
    }
}