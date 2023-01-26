using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Cli
{
    public class ConPrint
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

        public void Prompt(string Layer = "")
        {
            Console.ForegroundColor = __layout.Prompt;
            Console.Write($"\r# ");

            Console.ForegroundColor = __layout.Layer;
            Console.Write(Layer);

            Console.ForegroundColor = __layout.Prompt;
            Console.Write($"> ");

            Console.ResetColor();
        }

        public void Print(string Message, bool HighLight = false)
        {
            Prompt();

            if (HighLight)
                Console.ForegroundColor = __layout.HighLight1;
            else
                Console.ForegroundColor = __layout.Text;

            Console.Write(Message);

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

            Console.Write(Text);

            Console.ResetColor();
        }

        public void LF() => Console.WriteLine();

        public void PrintWithState(string Message, StateEnum StateValue)
        {
            Prompt();

            Console.ForegroundColor = __layout.Text;
            Console.Write(Message);

            this.PrintState(StateValue);
        }

        private string GetStateText(StateEnum StateValue)
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
            Console.Write("[ ");

            SetStateColor(StateValue);
            Console.Write(GetStateText(StateValue));

            Console.ForegroundColor = __layout.Text;
            Console.Write("] ");

            Console.ResetColor();

            if (LineFeed)
                LF();
        }

        public void AddMessage(string Group, string Message) =>
            AddMessage(Group, -1, Message, null, null);

        public void AddMessage(string Group, int ProcessId, string Message) =>
            AddMessage(Group, ProcessId, Message, null, null);

        public void AddMessage(string Group,int ProcessId, string Message, StateEnum State) =>
            AddMessage(Group, ProcessId, Message, null, (State) => GetStateText(State));

        public void AddMessage(string Group, string Message, Func<ConPrint.StateEnum> Callback) =>
            AddMessage(Group, -1, Message, Callback);

        public void AddMessage(string Group, int ProcessId, string Message, Func<ConPrint.StateEnum> Callback) =>
            AddMessage(Group, ProcessId, Message, Callback);

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
        }

        public void BeginMessage(string Group, int ProcessId, string Message)
        {
            Console.ForegroundColor = __layout.Text;

            if (Group != "")
            {
                Console.Write("[ ");

                string Group2 = Group.TrimEnd().CenterText(8);
                Console.ForegroundColor = __layout.Layer;
                Console.Write(Group2);

                Console.ForegroundColor = __layout.Text;
                Console.Write("] ");
            }

            if (ProcessId != -1)
            {
                Console.Write("[ ");

                string ProcessId2 = ProcessId.ToString().CenterText(4);
                Console.ForegroundColor = __layout.HighLight1;
                Console.Write(ProcessId2);

                Console.ForegroundColor = __layout.Text;
                Console.Write("] ");
            }

            int MaxLength = Console.BufferWidth - Console.CursorLeft - 8 - 1;
            string Message2 = Message.LimitLength(MaxLength);

            Console.Write(Message);

            Console.ResetColor();
        }

        public void UpdateState(ConPrint.StateEnum Result) =>
            PrintState(Result, false);

        public void AppendMessage(string Message)
        {
            int MaxLength = Console.BufferWidth - Console.CursorLeft - 8 - 1;
            string Message2 = Message.LimitLength(MaxLength);

            Console.Write(Message);
        }

        public void EndMessage(ConPrint.StateEnum Result) =>
            PrintState(Result, true);
    }
}
