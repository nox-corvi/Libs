using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Nox.Cli
{
    public class CliBufferItem
    {
        public char Char { get; set; } = ' ';
        public ConsoleColor ForeColor { get; set; } = Console.ForegroundColor;
        public ConsoleColor BackColor { get; set; } = Console.BackgroundColor;

        public CliBufferItem() { }

        public bool IsEqualTo(char Char, ConsoleColor ForeColor, ConsoleColor BackColor)
            => (this.Char == Char) & (this.ForeColor == ForeColor) & (this.BackColor == BackColor);


        public CliBufferItem(char c)
            : this() => this.Char = c;

        public CliBufferItem(char c, ConsoleColor foreColor, ConsoleColor backColor)
            : this(c)
        {
            ForeColor = foreColor;
            BackColor = backColor;
        }
    }

    public class CliBuffer : ICloneable
    {
        public CliBufferItem[,] _Buffer;

        #region Properties
        public CliBufferItem this[int x, int y]
        {
            get
            {
                try
                {
                    return _Buffer[x, y];
                }
                catch (Exception e)
                {
                    Helpers.Nope();
                    return null;
                }
            }
            set => _Buffer[x, y] = value;
        }

        public int Width { get => _Buffer.GetLength(0); }

        public int Height { get => _Buffer.GetLength(1); }
        #endregion

        public CliBufferItem GetItem(int x, int y)
            => _Buffer[x, y];

        public void SetItem(int x, int y, CliBufferItem item)
            => _Buffer[x, y] = item;

        public void Move(int x, int y)
        {
            int fX, fY;

            for (int iY = 0; iY < Height; iY++)
                for (int iX = 0; iX < Width; iX++)
                {
                    fX = iX + x;
                    fY = iY + y;

                    CliBufferItem item;
                    if (Bounds(iX, iY))
                    {
                        item = GetItem(iX, iY);
                        if (Bounds(fX, fY))
                            SetItem(fX, fY, item);
                    }
                }

            if (x < 0)
                // clear right cols
                for (int i = 0; i < Math.Abs(x); i++)
                    ClearCol(Console.WindowWidth - i - 1);
            else
                // clear left cols
                for (int i = 0; i < x; i++)
                    ClearCol(i);


            if (y < 0)
                // clear right cols
                for (int i = 0; i < Math.Abs(y); i++)
                    ClearRow(Console.WindowHeight - i - 1);
            else
                // clear left cols
                for (int i = 0; i < y; i++)
                    ClearRow(i);
        }

        public void ClearRow(int y)
        {
            for (int i = 0; i < Width; i++)
                SetItem(i, y, new CliBufferItem(' ', Console.ForegroundColor, Console.BackgroundColor));
        }

        public void ClearCol(int x)
        {
            for (int i = 0; i < Height; i++)
                SetItem(x, i, new CliBufferItem(' ', Console.ForegroundColor, Console.BackgroundColor));
        }

        private bool Bounds(int x, int y)
            => ((x >= 0) & (x < Width) & (y >= 0) & (y < Height));

        private void Restore(Action<char> Draw)
        {
            for (int y = 0; y < _Buffer.GetLength(1); y++)
            {
                Console.CursorTop = y;
                for (int x = 0; x < _Buffer.GetLength(0); x++)
                {
                    Console.CursorLeft = x;
                    Console.ForegroundColor = _Buffer[x, y].ForeColor;
                    Console.BackgroundColor = _Buffer[x, y].BackColor;

                    Draw.Invoke(_Buffer[x, y].Char);
                }
            }
        }

        public void RestoreToConsole()
            => Restore((c) => Console.Write(c));

        public void RestoreTo(ConWindow Con1)
            // write in buffer 
            => Restore((c) => Con1.Print(c.ToString()));

        public object Clone()
            => new CliBuffer(_Buffer);

        protected CliBuffer(CliBufferItem[,] buffer)
        {
            _Buffer = new CliBufferItem[buffer.GetLength(0), buffer.GetLength(1)];
            for (int y = 0; y < buffer.GetLength(1); y++)
                for (int x = 0; x < buffer.GetLength(0); x++)
                    _Buffer[x, y] = new CliBufferItem(buffer[x, y].Char,
                        buffer[x, y].ForeColor,
                        buffer[x, y].BackColor);
        }

        public CliBuffer(int Width, int Height)
        {
            _Buffer = new CliBufferItem[Width, Height];
            for (int y = 0; y <= _Buffer.GetUpperBound(1); y++)
                for (int x = 0; x <= _Buffer.GetUpperBound(0); x++)
                    _Buffer[x, y] = new(' ', Console.ForegroundColor, Console.BackgroundColor);
        }
    }

    public abstract class ConBase
        : IDisposable
    {
        public bool SetCursor(int X, int Y)
        {
            int fX = Console.CursorLeft, fY = Console.CursorTop;
            try
            {
                if ((X != Console.CursorLeft) | (Y != Console.CursorTop))
                    // set cursor
                    Console.SetCursorPosition(X, Y);

                return true;
            }
            catch (Exception)
            {
                // if failed, back to originyou
                Console.SetCursorPosition(fX, fY);
                return false;
            }
        }

        public bool SetCursorWrap(int X, int Y)
        {
            int fX = X, fY = Y;
            while (fX > (Console.WindowWidth - 1))
            {
                fX -= Console.WindowWidth;
                fY++;
            }
            if (fY > (Console.WindowHeight - 1))
                return false;

            return SetCursor(fX, fY);
        }

        public virtual void Dispose()
        { }
    }

    public class ConPrint
        : ConBase
    {
        public enum StateEnum
        {
            done,
            fail,
            warn,

            info,
            none
        }

        public Layout __layout = Layout.Default;


        public void Print(string Message)
            => Console.Write(Message);

        public void PrintWith(string Message, ConsoleColor ForeColor, ConsoleColor BackColor)
        {
            Console.ForegroundColor = ForeColor;
            Console.BackgroundColor = BackColor;

            Print(Message);

            Console.ResetColor();
        }

        public void PrintWith(string Message, ConsoleColor ForeColor)
            => PrintWith(Message, ForeColor, Console.BackgroundColor);

        public void PrintTo(string Message, int X, int Y)
        {
            SetCursor(X, Y);
            Print(Message);
        }

        public void PrintToWith(string Message, int X, int Y, ConsoleColor ForeColor)
        {
            Console.ForegroundColor = ForeColor;
            PrintTo(Message, X, Y);

            Console.ResetColor();
        }

        public void PrintToWith(string Message, int X, int Y, ConsoleColor ForeColor, ConsoleColor BackColor)
        {
            Console.BackgroundColor = BackColor;
            PrintToWith(Message, X, Y, ForeColor);
        }

        public void Prompt(string Layer = "")
        {
            PrintWith($"\r# ", __layout.Prompt);
            PrintWith(Layer, __layout.Layer);
            PrintWith($"> ", __layout.Prompt);
        }

        public void PrintWithPrompt(string Message, bool HighLight = false)
        {
            Prompt();

            if (HighLight)
                Console.ForegroundColor = __layout.HighLight1;
            else
                Console.ForegroundColor = __layout.Text;

            Print(Message);

            Console.ResetColor();
        }

        public void PrintLine(string Message) =>
            Print(Message + "\r\n");

        public void PrintError(string Message) =>
            Console.Error.WriteLine(Message);

        public void PrintText(string Text, bool HighLight = false)
        {
            if (HighLight)
                Console.ForegroundColor = __layout.HighLight1;
            else
                Console.ForegroundColor = __layout.Text;

            Print(Text);

            Console.ResetColor();
        }

        public void LF() 
            => Console.WriteLine();

        public void PrintWithState(string Message, StateEnum StateValue)
        {
            Prompt();

            Console.ForegroundColor = __layout.Text;
            Print(Message);

            this.PrintState(StateValue);
        }

        public string GetStateText(StateEnum StateValue)
        {
            switch (StateValue)
            {
                case StateEnum.done:
                    return " OK ";
                case StateEnum.fail:
                    return "FAIL";
                case StateEnum.warn:
                    return "WARN";
                case StateEnum.info:
                    return "INFO";
                default:
                    return " -- ";
            }
        }

        private void SetStateColor(StateEnum StateValue)
        {
            switch (StateValue)
            {
                case StateEnum.done:
                    Console.ForegroundColor = __layout.StateDone;
                    break;
                case StateEnum.fail:
                    Console.ForegroundColor = __layout.StateFail;
                    break;
                case StateEnum.warn:
                    Console.ForegroundColor = __layout.StateWarn;
                    break;
                case StateEnum.info:
                    Console.ForegroundColor = __layout.StateInfo;
                    break;
                default:
                    Console.ForegroundColor = __layout.Text;
                    break;
            }
        }

        public void PrintState(StateEnum StateValue, bool LineFeed = true)
        {
            string StateText = $"[ {GetStateText(StateValue)} ]";

            Console.CursorLeft = Console.WindowWidth - StateText.Length - 1;

            Console.ForegroundColor = __layout.Text;
            Print("[ ");

            SetStateColor(StateValue);
            Print(GetStateText(StateValue));

            Console.ForegroundColor = __layout.Text;
            Print("] ");

            Console.ResetColor();

            if (LineFeed)
                LF();
        }

        public void AddMessage(string Group, string Message) =>
            AddMessage(Group, -1, Message, null, null);

        public void AddMessage(string Group, int ProcessId, string Message) =>
            AddMessage(Group, ProcessId, Message, null, null);

        public void AddMessage(string Group, int ProcessId, string Message, StateEnum State) =>
            AddMessage(Group, ProcessId, Message, null, (State) => GetStateText(State));

        public void AddMessage(string Group, string Message, Func<ConPrint.StateEnum> Callback) =>
            AddMessage(Group, -1, Message, Callback);

        public void AddMessage(string Group, int ProcessId, string Message, Func<ConPrint.StateEnum> Callback) =>
            AddMessage(Group, ProcessId, Message, Callback, null);

        public void AddMessage(string Group, int ProcessId, string Message, Func<ConPrint.StateEnum> Callback, Func<ConPrint.StateEnum, string> MessageResult)
        {
            BeginMessage(Group, ProcessId, Message);

            if (Callback != null)
            {
                var Result = Helpers.OnXTry<ConPrint.StateEnum>(Callback, (Exception e) => StateEnum.fail);

                AppendMessage(MessageResult?.Invoke(Result) ?? "");
                EndMessage(Result);
            }
            else
                LF();

            Prompt();
        }

        public void BeginMessage(string Group, int ProcessId, string Message)
        {
            Print("\r");
            Console.ForegroundColor = __layout.Text;

            if (Group != "")
            {
                Print("[ ");

                string Group2 = Group.TrimEnd().CenterText(8);
                Console.ForegroundColor = __layout.Layer;
                Print(Group2);

                Console.ForegroundColor = __layout.Text;
                Print("] ");
            }

            if (ProcessId != -1)
            {
                Print("[ ");

                string ProcessId2 = ProcessId.ToString().CenterText(4);
                Console.ForegroundColor = __layout.HighLight1;
                Print(ProcessId2);

                Console.ForegroundColor = __layout.Text;
                Print("] ");
            }

            //int MaxLength = Console.WindowWidth - Console.CursorLeft - 12 - (ProcessId != -1 ? 8 : 0) - 1;
            //string Message2 = Message.LimitLength(MaxLength);

            Print(Message);

            Console.ResetColor();
        }

        public void UpdateState(ConPrint.StateEnum Result) =>
            PrintState(Result, false);

        public void AppendMessage(string Message)
        {
            //int MaxLength = Console.BufferWidth - Console.CursorLeft - 8 - 1;
            //string Message2 = Message.LimitLength(MaxLength);

            Print(Message);
        }

        public void EndMessage(ConPrint.StateEnum Result) =>
            PrintState(Result, true);

        //string Chars = " ─ │ ┌ ┐ └ ┘ ├ ┤ ┬ ┴ ┼ ═ ║ ╒ ╓ ╔ ╕ ╖ ╗ ╘ ╙ ╚ ╛ ╜ ╝ ╞ ╟ ╠ ╡ ╢ ╣ ╤ ╥ ╦ ╧ ╨ ╩ ╪ ╫ ╬ ▀ ▄ █ ▌ ▐ ░ ▒ ▓ ■ □ ▪ ▫ ";

        public ConPrint() { }
    }
    public class ConWindow
        : ConBase
    {
        public Layout __layout = Layout.Default;
        private CliBuffer _buffer;

        public void Invalidate()
            => _buffer = new CliBuffer(Console.WindowWidth, Console.WindowHeight);

        public void PrintRaw(int X, int Y, string Message, ConsoleColor ForeColor, ConsoleColor BackColor, bool Wrap = true)
            => PrintRaw(X, Y, Message, ForeColor, BackColor, Wrap, _buffer);

        public void PrintRaw(int X, int Y, string Message, ConsoleColor ForeColor, ConsoleColor BackColor, bool Wrap = true, CliBuffer buffer = null)
        {
            // invalidate buffer?
            if ((buffer.Width != Console.WindowWidth) | (_buffer.Height != Console.WindowHeight))
                Invalidate();

            for (int i = 0; i < Message.Length; i++)
            {
                // get cursor position .. if text is wrapped, cursor position always fits the array boundaries
                int x = Console.CursorLeft, y = Console.CursorTop;

                Func<bool> CanWrite = () =>
                    // if next char will wrap, ignore write if wrap is disabled
                    Wrap || (x < buffer.Width);

                // write 
                if (buffer == null)
                {
                    if (CanWrite())
                        Console.Write(Message[i]);
                }
                else
                    if (CanWrite())
                    if (!_buffer[x, y].IsEqualTo(Message[i], Console.ForegroundColor, Console.BackgroundColor))
                    {
                        _buffer[x, y].ForeColor = Console.ForegroundColor;
                        _buffer[x, y].BackColor = Console.BackgroundColor;
                        _buffer[x, y].Char = Message[i];

                        Console.Write(Message[i]);
                    }
            }
        }      

        public bool Print(string Message)
        {
            // invalidate buffer?
            if ((_buffer.Width != Console.WindowWidth) | (_buffer.Height != Console.WindowHeight))
                Invalidate();

            int fX = Console.CursorLeft, fY = Console.CursorTop;
            for (int i = 0; i < Message.Length; i++)
            {
                // wrap if cursor position wont fit the array boundaries
                if (Console.CursorLeft > (Console.WindowWidth - 1))
                    if (!SetCursor(Console.CursorLeft - Console.WindowWidth, Console.CursorTop + 1))
                        return false;

                //if (delta > 0)
                //{

                //    while (delta > 0)
                //    { 
                //        Console.SetCursorPosition(0, Console.WindowHeight);
                //        Console.WriteLine();
                //        delta--;
                //        fY--;
                //    }
                //}

                // write 
                int x = Console.CursorLeft, y = Console.CursorTop;
                if (!_buffer[x, y].IsEqualTo(Message[i], Console.ForegroundColor, Console.BackgroundColor))
                {
                    _buffer[x, y].ForeColor = Console.ForegroundColor;
                    _buffer[x, y].BackColor = Console.BackgroundColor;
                    _buffer[x, y].Char = Message[i];

                    Console.Write(Message[i]);
                }
                else
                    if (!SetCursor(x + 1, y))
                    return false;
            }

            return true;
        }

        public void PrintWith(string Message, ConsoleColor ForeColor, ConsoleColor BackColor)
        {
            Console.ForegroundColor = ForeColor;
            Console.BackgroundColor = BackColor;

            Print(Message);

            Console.ResetColor();
        }

        public void PrintWith(string Message, ConsoleColor ForeColor)
            => PrintWith(Message, ForeColor, Console.BackgroundColor);

        public void PrintTo(string Message, int X, int Y)
        {
            Console.CursorLeft = X;
            Console.CursorTop = Y;
            Print(Message);
        }

        public void PrintToWith(string Message, int X, int Y, ConsoleColor ForeColor)
        {
            Console.ForegroundColor = ForeColor;
            PrintTo(Message, X, Y);

            Console.ResetColor();
        }

        public void PrintToWith(string Message, int X, int Y, ConsoleColor ForeColor, ConsoleColor BackColor)
        {
            Console.BackgroundColor = BackColor;
            PrintToWith(Message, X, Y, ForeColor);
        }

        public void Prompt(string Layer = "")
        {
            PrintWith($"\r# ", __layout.Prompt);
            PrintWith(Layer, __layout.Layer);
            PrintWith($"> ", __layout.Prompt);
        }

        public void Print(string Message, bool HighLight = false)
        {
            Prompt();

            if (HighLight)
                Console.ForegroundColor = __layout.HighLight1;
            else
                Console.ForegroundColor = __layout.Text;

            Print(Message);

            Console.ResetColor();
        }

        public void PrintLine(string Message) =>
            Print(Message + "\r\n");

        public void PrintError(string Message) =>
            Console.Error.WriteLine(Message);

        public void PrintText(string Text, bool HighLight = false)
        {
            if (HighLight)
                Console.ForegroundColor = __layout.HighLight1;
            else
                Console.ForegroundColor = __layout.Text;

            Print(Text);

            Console.ResetColor();
        }

        public void LF()
        {
            Console.CursorLeft = 0;
            Console.CursorTop += 1;
        }

        //string Chars = " ─ │ ┌ ┐ └ ┘ ├ ┤ ┬ ┴ ┼ ═ ║ ╒ ╓ ╔ ╕ ╖ ╗ ╘ ╙ ╚ ╛ ╜ ╝ ╞ ╟ ╠ ╡ ╢ ╣ ╤ ╥ ╦ ╧ ╨ ╩ ╪ ╫ ╬ ▀ ▄ █ ▌ ▐ ░ ▒ ▓ ■ □ ▪ ▫ ";

        public void Redraw()
            => _buffer.RestoreTo(this);

        public CliBuffer GetBuffer()
            => (CliBuffer)_buffer.Clone();

        public void PrintWindowTitle(string Title, int X, int Y, int Width, ConsoleColor ForeColor, ConsoleColor BackColor)
            => PrintToWith($" {Title}{new string(' ', Width - Title.Length - 1)}", X, Y, ForeColor, BackColor);

        public void PrintWindowTitle(string Title, int X, int Y, int Width)
            => PrintWindowTitle(Title, X, Y, Width, __layout.WindowTitleText, __layout.WindowTitleBack);

        public void PrintFrame(int X, int Y, int Width, int Height, bool Double, int Fill, ConsoleColor ForeColor, ConsoleColor BackColor, ConsoleColor FillColor)
        {
            Console.ForegroundColor = ForeColor;
            Console.BackgroundColor = BackColor;
            // top left
            PrintTo(Double ? "╔" : "┌", X, Y);

            // top
            for (int i = 1; i < Width - 1; i++)
                PrintTo(Double ? "═" : "─", X + i, Y);

            // top right
            PrintTo(Double ? "╗" : "┐", X + Width - 1, Y);

            // sidebars
            for (int i = Y + 1; i < Y + (Height - 1); i++)
            {
                PrintTo(Double ? "║" : "│", X, i);
                if (Fill > 0)
                {
                    Console.BackgroundColor = FillColor;

                    for (int k = X + 1; k < X + (Width - 1); k++)
                        switch (Fill)
                        {
                            case 1:
                                PrintTo(" ", k, i);
                                break;
                            case 2:
                                PrintTo("░", k, i);
                                break;
                            case 3:
                                PrintTo("▒", k, i);
                                break;
                            case 4:
                                PrintTo("▓", k, i);
                                break;
                        }

                    Console.BackgroundColor = BackColor;

                }

                PrintTo(Double ? "║" : "│", X + Width - 1, i);
            }

            // bottom left
            PrintTo(Double ? "╚" : "└", X, Y + Height - 1);

            // bottom
            for (int i = 1; i < Width - 1; i++)
                PrintTo(Double ? "═" : "─", X + i, Y + (Height - 1));

            // bottom right
            PrintTo(Double ? "╝" : "┘", X + (Width - 1), Y + (Height - 1));
            Console.ResetColor();
        }

        public void PrintBar(int X, int Y, int Width, int Progress, ConsoleColor ForeColor, ConsoleColor BackColor)
        {
            Console.ForegroundColor = ForeColor;
            Console.BackgroundColor = BackColor;


            //'■ □'ad
            PrintTo($"[{new string(' ', Width - 2)}]", X, Y);

            int t = (int)((Width - 2) / 100d * Progress);

            for (int i = 0; i < t; i++)
                PrintTo("■", X + i + 1, Y);

            Console.ResetColor();
        }
        public void PrintBar(int X, int Y, int Width, int Progress) =>
            PrintBar(X, Y, Width, Progress, __layout.HighLight1, __layout.WindowBack2);

        public ConWindow()
            => Invalidate();
    }
}
