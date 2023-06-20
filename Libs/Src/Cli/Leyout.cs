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

        public ConsoleColor WindowTitleText;
        public ConsoleColor WindowTitleBack;
        public ConsoleColor WindowBack1;
        public ConsoleColor WindowBack2;

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

                WindowTitleText = ConsoleColor.Yellow,
                WindowTitleBack = ConsoleColor.Blue,
                WindowBack1 = ConsoleColor.Blue,
                WindowBack2 = ConsoleColor.DarkCyan,

                StateInfo = ConsoleColor.DarkMagenta,
                StateWarn = ConsoleColor.DarkYellow,
                StateFail = ConsoleColor.DarkRed,
                StateDone = ConsoleColor.DarkGreen,
            };
        }
    }
}
