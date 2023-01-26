using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Cli
{
    public class Layout
    {
        public static readonly Layout Default;

        #region Properties
        public ConsoleColor Text;
        public ConsoleColor HighLight1;
        public ConsoleColor HighLight2;

        public ConsoleColor Prompt;
        public ConsoleColor Layer;

        public ConsoleColor StateInfo;
        public ConsoleColor StateWarn;
        public ConsoleColor StateFail;
        public ConsoleColor StateDone;
        #endregion

        static Layout()
        {
            Default = new Layout()
            {
                Text = ConsoleColor.Gray,
                HighLight1 = ConsoleColor.White,
                HighLight2 = ConsoleColor.White,


                Prompt = ConsoleColor.Cyan,
                Layer = ConsoleColor.Green,

                StateInfo = ConsoleColor.DarkMagenta,
                StateWarn = ConsoleColor.DarkYellow,
                StateFail = ConsoleColor.DarkRed,
                StateDone = ConsoleColor.DarkGreen,
            };
        }
    }
}
