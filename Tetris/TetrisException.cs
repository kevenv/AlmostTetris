using System;

namespace Tetris
{
    [Serializable]
    class TetrisException : Exception
    {
        public TetrisException(string message)
        :base(message)
        {
        }
    }
}
