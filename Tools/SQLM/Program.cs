using Nox.Cli;
using Nox.IO.Miru;
using System.Text;

namespace SQLM
{
    internal class Programm
    {
        private const string Title = @"tiny util to diag and run maintenance jobs on microsoft sql server database";

        /// <summary>
        /// Descriptor for connection, control and execution
        /// </summary>
        private static MiruParseDescriptor CliDescriptor1 = new MiruParseDescriptor("SQLM", Title, 
            CommandDesc.Create(
                null), 
                
            SwitchDesc.Create(
                new SwitchDesc("trust", "t", "use trusted connection to connect to sql server"), 
                new SwitchDesc("auth", "a", "use sql server authentication")
                {
                    SwitchArguments = SwitchArgDesc.Create(
                        new SwitchArgDesc("user", "sql user for authentication."),
                        new SwitchArgDesc("pass", "sql pass for authentication. if pass is not specified, you will be prompted for")
                        )
                },
                new SwitchDesc("database", "db", "database to connect to"),
                new SwitchDesc("execute", "e", "command for execution. if execute is not specified, app will start in interactive mode")
                ), null);

        /// <summary>
        /// Descriptor for 
        /// </summary>
        private static MiruParseDescriptor CliDescriptor2 = new MiruParseDescriptor("SQLM", Title, 
            CommandDesc.Create(
                new CommandDesc("validate")
                {
                    Arguments = ArgDesc.Create(new RequiredArgDesc("user", "user to validate"), new RequiredArgDesc("password", "password to validate")),
                }))
        {
            Switches = SwitchDesc.Create(
                new SwitchDesc("domain", "performs the process for domain controllers")
                {
                    SwitchValueType = SwitchValueTypeEnum._string,
                }),
            ExitCodes = new()
            {
                new ExitCodeDesc(-1, "if there is no result"),
                new ExitCodeDesc(-2, "if the search pattern gives an ambiguous result"),
                new ExitCodeDesc(-1000, "access denied"),
                new ExitCodeDesc(-1001, "password exception"),
                new ExitCodeDesc(-1002, "principal already exists"),
                new ExitCodeDesc(-1003, "impersonation failed"),
                new ExitCodeDesc(0, "if ok"),
                new ExitCodeDesc(1, "if arguments are invalid"),
                new ExitCodeDesc(2, "if an error occured"),
                new ExitCodeDesc(3, "unknown"),
            }
        };

        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CommandLine);

            var Con1 = new ConPrint();
            var parser = new Parser(CliDescriptor1);
            var result = parser.Parse(args);

            Action Help = () => new ManualCreator(CliDescriptor1, Con: Con1).PrintHelp();
            Action<int> Quit = (int ExitCode) => { Environment.Exit(ExitCode); };
            Action<int, string> QuitWith = (int ExitCode, string Message) => { if (!string.IsNullOrWhiteSpace(Message)) Con1.PrintError(Message); Quit(ExitCode); };

            if (args.Where(f => f.Equals("--help")).Count() > 0)
            {
                Help();
                Quit(0);
            }

            Environment.ExitCode = 3;
            if (result.Success)
            {
                var c = result.Command;

                try
                {
                    switch (c.CommandText)
                    {
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Con1.PrintError(e.Message);
                    Environment.ExitCode = -1001;
                }
                finally
                {

                }
            }
            else
            {
                foreach (var item in result.FailResults)
                    Con1.PrintError(item);

                Con1.PrintError("use --help");
                Environment.ExitCode = 1;
            }
        }
    }
}