using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nox.Libs;
using Nox.Libs.Data;
using Nox.Libs.Data.SqlServer;
using Nox.Libs.Buffer;

/// <summary>
/// 
/// </summary>
namespace Nox.Libs.Cli
{
    public enum SwitchArgType
    {
        _none = 0,
        _string = 1,
        _bool = 2,
        _int = 10,
        _list = 100,
    }

    public class CliCommand
    {
        #region Properties
        /// <summary>
        /// Liefert das Kommando zurück.
        /// </summary>
        public string CommandText { get; internal set; }

        /// <summary>
        /// Liefert eine Beschreibung für den Schalter zurück oder legt Sie fest.
        /// </summary>
        public string Description { get; internal set; }
        #endregion

        public CliCommand(string CommandText, string Description = "")
        {
            this.CommandText = CommandText;
            this.Description = Description;
        }
    }

    public class CliArg
    {
        #region Properties
        /// <summary>
        /// Gibt den Namen des Argumentes zurück
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gibt die Position zurück an der dieser Schalter stehen muss, -1 wenn die Reihenfolge ohne Bedeutung ist.
        /// </summary>
        public int Position { get; internal set; }

        /// <summary>
        /// Gibt zurück ob der Parameter erforderlich ist.
        /// </summary>
        public bool Required { get; internal set; }

        /// <summary>
        /// Liefert eine Beschreibung für den Schalter zurück oder legt Sie fest.
        /// </summary>
        public string Description { get; internal set; }
        #endregion

        public CliArg(string Name, int Position, bool Required, string Description = "")
        {
            this.Name = Name;
            this.Position = Position;
            this.Required = Required;

            this.Description = Description;
        }
    }

    public class CliSwitch
    {
        #region Properties
        /// <summary>
        /// Gibt den Schalter-Code zurück.
        /// </summary>
        public string Switch { get; internal set; }

        /// <summary>
        /// Gibt den Typ des Schalterargumentes fest.
        /// </summary>
        public SwitchArgType ArgsType { get; internal set; }

        /// <summary>
        /// Gibt eine Argumentenliste zurück oder legt sie fest. Gilt nur für ArgsType (_list)
        /// </summary>
        public string[] Args { get; internal set; }

        /// <summary>
        /// Gibt zurück ob der Schalter für das Kommando benötigt wird.
        /// </summary>
        public bool Required { get; internal set; }

        /// <summary>
        /// Beschreibung
        /// </summary>
        public string Description { get; internal set; }
        #endregion

        public CliSwitch(string Switch, string Description = "", bool Required = false, SwitchArgType ArgsType = SwitchArgType._none, params string[] Args)
        {
            this.Switch = Switch;
            this.Required = Required;
            this.ArgsType = ArgsType;
            if (this.ArgsType == SwitchArgType._list)
                this.Args = Args;

            this.Description = Description;
        }
    }

    public class CliSwitchValue
    {
        private CliSwitch   _Switch;
        private string      _Value = "";
        private bool        _HasValue = false;


        #region Properties
        /// <summary>
        /// Gibt den zugehörigen Schalter zurück
        /// </summary>
        public CliSwitch Switch { get; internal set; }

        public string Value
        {
            get
            {
                return _Value;
            }
            internal set
            {
                _HasValue = ((_Value = value) != "");
            }
        }

        public bool HasValue
        {
            get
            {
                return _HasValue;
            }
        }
        #endregion

        public CliSwitchValue(CliSwitch Switch)
        {
            this._Switch = Switch;
        }

        public CliSwitchValue(CliSwitch Switch, string Value)
            : this(Switch)
        {
            this.Value = Value;
        }
    }

    public class CliCommandSet
    {
        CliCommand              _CommandObject;
        EventList<CliArg>       _Args;
        EventList<CliSwitch>    _Switches = null;

        #region Properties
        public CliCommand Command
        {
            get
            {
                return _CommandObject;
            }
            internal set
            {
                _CommandObject = value;
            }
        }

        /// <summary>
        /// Liefert eine Liste der allgemeinen Parameter zurück oder legt Sie fest.
        /// </summary>
        public EventList<CliArg> Args
        {
            get
            {
                return _Args;
            }
            internal set
            {
                _Args = value;
            }
        }

        /// <summary>
        /// Liefert eine Liste mit den Flags zurück oder legt sie fest.
        /// </summary>
        public EventList<CliSwitch> Switches
        {
            get
            {
                return _Switches;
            }
            internal set
            {
                _Switches = value;
            }
        }
        #endregion

        public CliCommandSet(CliCommand CommandObject, CliArg[] Args,  CliSwitch[] Switches) 
        {
            _CommandObject = CommandObject;

            _Args = new EventList<CliArg>();
            if (Args != null)
                _Args.AddRange(Args);

            _Switches = new EventList<CliSwitch>();
            if (Switches != null)
                _Switches.AddRange(Switches);
        }
    }

    public class CliParserResult
    {
        private string              _Command;
        private string[]            _Args;
        private CliSwitchValue[]    _Switches;

        #region Properties
        public bool Success { get; internal set; }

        public string Command { get; internal set; }

        public string[] Args { get; internal set; }

        public CliSwitchValue[] Switches { get; internal set; }

        public List<string> FailResults { get; internal set; }
        #endregion

        /// <summary>
        /// Prüft ob das Parser-Ergebniss einen Schalter beinhaltet.
        /// </summary>
        /// <param name="Switch">Der zu suchenden Schalter</param>
        /// <returns>Wahr wenn der Schalter gefunden wurde, sonst Falsch</returns>
        public bool HasFlag(string Switch)
        {
            for (int k = 0; k < Switches.Length; k++)
                if (Switches[k].Switch.Switch.Equals(Switch, StringComparison.InvariantCultureIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>
        /// Liefert ein Argument für den Schalter zurück sofern vorhanden
        /// </summary>
        /// <param name="Switch">Der Name des Schalter nach dem gesucht werden soll</param>
        /// <returns>Das Argument sofern vorhanden, sonst einen Leerstring</returns>
        public string SwitchArg(string Switch)
        {
            for (int k = 0; k < Switches.Length; k++)
                if (Switches[k].Switch.Switch.ToLower() == Switch.ToLower())
                    return Switches[k].Value;

            return "";
        }

        public CliParserResult()
        {
            FailResults = new EventList<string>();
        }
    }

    public class CliParser
    {
        #region Properties
        public string[] SwitchSeperator { get; private set; }
        public string[] SwitchArgSeperator { get; private set; }

        public EventList<CliCommandSet> CommandSets { get; private set; }
        #endregion

        private CliParserResult ShrinkParams(ref List<string> Params)
        {
            int ParamsCount = Params.Count;
            for (int i = 0; i < ParamsCount; i++)
                if (Params[i].StartsWith("\""))
                    while (!Params[i].EndsWith("\""))
                    {
                        if ((i + 1) > (ParamsCount - 1))
                            return WithResult(new string[] { "\" expected" });

                        Params[i] += ' ' + Params[i + 1];
                        for (int j = i + 1; j < (ParamsCount - 1); j++)
                            Params[j] = Params[j + 1];

                        ParamsCount--;
                        Params.RemoveAt(ParamsCount);
                    }

            return new CliParserResult()
            {
                Success = true
            };
        }

        public CliParserResult Parse(string Input)
        {
            //const char SPC = ' ';
            var Result = new CliParserResult();

            //if (Input.Trim().Equals(String.Empty, StringComparison.InvariantCultureIgnoreCase))
            //    return WithResult(new string[] { "cli empty, nothing to do" });

            //// extract command from cli
            //string CommandString = Helpers.GetBefore(Input.TrimStart(), " ", Input).Trim();

            //// check if command exists in definition
            //CliCommandSet CommandSetObject = CommandSets.Find(value => value.Command.CommandText.Equals(CommandString, StringComparison.InvariantCultureIgnoreCase));
            //if (CommandSetObject == null)
            //    return WithResult(new string[] { "command not found" });

            //// extract and merge params
            //var Params = new List<string>(Helpers.GetAfter(Input, SPC.ToString()).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            //CliParserResult ShrinkResult = ShrinkParams(ref Params);
            //if (!ShrinkResult.Success)
            //    return WithResult(Result, ShrinkResult);
          

            //List<CliSwitchValue> FoundSwitches = new List<CliSwitchValue>();
            //List<string> FoundCommonParams = new List<string>();

            //List<CliSwitch> RequiredSwitches = CommandSetObject.Switches.FindAll(value => value.Required);
            //List<CliArg> RequiredCommonParams = CommandSetObject.CommonParams.FindAll(value => value.Required);
            //int RequiredCommonParamsCount = RequiredCommonParams.Count;

            //for (int i = 0; i < Params.Count; i++)
            //{
            //    int MatchSeperator = -1;
            //    for (int k = 0; k < SwitchSeperator.Length; k++)
            //        if (Params[i].StartsWith(SwitchSeperator[k]))
            //        {
            //            MatchSeperator = k;
            //            break;
            //        }

            //    if (MatchSeperator == -1)    // common argument
            //    {
            //        if (RequiredCommonParamsCount == 0)
            //            return WithResult(String.Format("Unexpected Common Parameter found '{0}'", Params[i]));

            //        RequiredCommonParamsCount--;
            //        FoundCommonParams.Add(Params[i].Replace("\"", ""));
            //    }
            //    else
            //    {
            //        string Param = Helpers.GetAfter(Params[i], SwitchSeperator[MatchSeperator]);
            //        string Switch = Helpers.GetBefore(Param, ":", Param);
            //        string SwitchArg = Helpers.GetAfter(Params[i], ":");

            //        CliSwitch CurrentSwitch;
            //        if ((CurrentSwitch = CommandSetObject.Switches.Find(value => value.Switch.ToLower() == Switch.ToLower())) != null)
            //        {
            //            // validate
            //            if ((CurrentSwitch.ArgsType == SwitchArgType._none) && (SwitchArg.Length > 0))
            //                return WithResult(String.Format("Switch '{0}' must not have an Argument", Switch));

            //            // check if args can parsed
            //            string ArgumentParsed = "";
            //            switch (CurrentSwitch.ArgsType)
            //            {
            //                case SwitchArgType._string:
            //                    ArgumentParsed = SwitchArg;
            //                    break;
            //                case SwitchArgType._bool:
            //                    bool ResultBoolParseTest = false;
            //                    if (bool.TryParse(SwitchArg, out ResultBoolParseTest))
            //                        ArgumentParsed = ResultBoolParseTest.ToString();
            //                    else
            //                        return WithResult(string.Format("parse-error, arg of flag '{0}' should be bool.", SwitchArg));
            //                    break;
            //                case SwitchArgType._int:
            //                    int ResultIntParseTest = 0;
            //                    if (int.TryParse(SwitchArg, out ResultIntParseTest))
            //                        ArgumentParsed = ResultIntParseTest.ToString();
            //                    else
            //                        return WithResult(string.Format("parse-error, arg of flag '{0}' should be int.", SwitchArg));
            //                    break;
            //                case SwitchArgType._list:
            //                    foreach (var Item in CurrentSwitch.Args)
            //                        if (Item.ToLower() == SwitchArg.ToLower())
            //                        {
            //                            ArgumentParsed = SwitchArg;
            //                            break;
            //                        }

            //                    return WithResult(string.Format("parse-error, arg of flag '{0}' not in possible values", SwitchArg));
            //                default:
            //                    ArgumentParsed = SwitchArg;
            //                    break;
            //            }
            //            FoundSwitches.Add(new CliSwitchValue(Switch, SwitchArg));

            //            // Remove from Required
            //            RequiredSwitches.Remove(CurrentSwitch);
            //        }
            //    }
            //}

            //if (RequiredSwitches.Count > 0)
            //    return WithResult("flags is missing");
            //if (RequiredCommonParamsCount > 0)
            //    return WithResult("commonarg is missing");

            //Result.Command = Command.Command;
            //Result.Args = FoundCommonParams.ToArray();
            //Result.Switches = FoundSwitches.ToArray();

            //Result.Success = true;

            return Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Results"></param>
        /// <returns></returns>
        private CliParserResult WithResult(params string[] Results)
        {
            var Result = new CliParserResult()
            {
                Success = false
            };

            Result.FailResults.Clear();
            Result.FailResults.AddRange(Results);

            return Result;
        }

        private CliParserResult WithResult(params CliParserResult[] Results)
        {
            var Result = new CliParserResult();
            
            foreach (var Item in Results)
            {
                Result.Success |= Item.Success;
                Result.FailResults.AddRange(Item.FailResults);
            }

            return Result;
        }

        public CliParser(CliCommand[] Commands = null)
        {
            SwitchArgSeperator = new string[] { ":" };
            SwitchSeperator = new string[] { "--", "-", "/" };

            //this.Commands = new EventList<CliCommand>();
            //if (Commands != null)
            //    this.Commands.AddRange(Commands);
        }
    }
}
