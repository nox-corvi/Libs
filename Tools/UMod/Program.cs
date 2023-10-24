using System.DirectoryServices.AccountManagement;
using System.Text;
using Nox.IO.Miru;
using Nox;
using Nox.Cli;

namespace UserMod
{
    public class Programm
    {
        private static SwitchDesc Help = new("help", "show this help");

        //<check type="reg" key="HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall" value="DisplayName" match="*SAP Crystal Reports*" />
        private static MiruParseDescriptor CliDescription = new("USERMOD",
            @"tiny util to create, change and modify local and ad user, change group memberships and much more",
            CommandDesc.Create(
                new CommandDesc("query")
                {
                    SubCommands = CommandDesc.Create(
                        new CommandDesc("user", "get some user information")
                        {
                            Arguments = ArgDesc.Create(new ArgDesc("user", "user to search for")),
                            Switches = SwitchDesc.Create(new SwitchDesc("show-membership", "if set, the groups will print out using the given search pattern") { SwitchValueType = SwitchValueTypeEnum._string })
                        }),
                },
                new CommandDesc("alter")
                {
                    SubCommands = CommandDesc.Create(
                        new CommandDesc("user")
                        {
                            Switches = SwitchDesc.Create(
                                new SwitchDesc("set-pass", "change the pass of the user")
                                {
                                    SwitchArguments = SwitchArgDesc.Create(new SwitchArgDesc("user", "user to change"), new SwitchArgDesc("pass", "pass to set"))
                                })
                        }),
                },
                new CommandDesc("create")
                {
                    SubCommands = CommandDesc.Create(
                        new CommandDesc("user", "create a new user")
                        {
                            Arguments = ArgDesc.Create(new ArgDesc("name", "name of the user"), new ArgDesc("displayname", "displayname of the user")),
                            Switches = SwitchDesc.Create(
                                new SwitchDesc("ou", "if set, the user is created in the following ou (ignored without --domain)")
                                {
                                    SwitchArguments = SwitchArgDesc.Create(new SwitchArgDesc("fqdn", "fqdn of the users target"))
                                },
                                new SwitchDesc("set-pass", "password of the user")
                                {
                                    SwitchArguments = SwitchArgDesc.Create(new SwitchArgDesc("password", "if set, the pass of the user will changed")),
                                }
                            ),
                        }
                    )
                },
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

        private static string PassGen(int Length)
        {
            var r = new Random((int)(DateTime.Now.Ticks & 0xFFFF));

            var chars = "BCDFGHJKMPQRTVWXY2346789";
            var result = new StringBuilder();

            for (int i = 0; i < Length; i++)
            {
                int p = r.Next(0, chars.Length - 1),
                    q = r.Next(0, 99);

                // never start with a digit
                if (i == 0)
                    while (char.IsDigit(chars[p]))
                        p = r.Next(0, chars.Length - 1);

                if (char.IsDigit(chars[p]))
                    result.Append(chars[p]);
                else
                {
                    switch (q & 0x1)
                    {
                        case 0:
                            result.Append(chars[p].ToString().ToLower());
                            break;
                        case 1:
                            result.Append(chars[p].ToString().ToUpper());
                            break;
                    }
                }
            }

            return result.ToString();
        }


        static void Main(string[] args)
        {
            Console.WriteLine(Environment.CommandLine);


            var Con1 = new ConPrint();
            var parser = new Parser(CliDescription);
            var result = parser.Parse(args);

            Action Help = () => new Nox.IO.Miru.ManualCreator(CliDescription, Con: Con1).PrintHelp();
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

                string domain = Environment.MachineName;
                ContextType Context = ContextType.Machine;
                if (result.Switches.HasSwitch("domain"))
                {
                    // switch to domain
                    domain = result.Switches.GetSwitch("domain").Value;
                    Context = ContextType.Domain;
                }

                try
                {
                    switch (c.CommandText)
                    {
                        case "query":
                            c = c.SubCommand;
                            switch (c.CommandText)
                            {
                                case "user":
                                    using (var pc = new PrincipalContext(Context, domain))
                                    using (var user = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName,
                                        c.Arguments.GetArgument("user").Value))
                                        if (user != null)
                                        {
                                            Con1.PrintText($"SID: {user.Sid}\r\n");
                                            Con1.PrintText($"SAM: {user.SamAccountName}\r\n");
                                            Con1.PrintText($"Distinquished Name: {user.DistinguishedName}\r\n");

                                            if (c.Switches.HasSwitch("show-membership"))
                                            {
                                                Con1.PrintText("\r\n");
                                                var pattern = c.Switches.GetSwitch("show-membership").Value.Replace("*", "%");

                                                var users = user.GetGroups();
                                                foreach (var group in users)
                                                    if (group.Name.IsLike(pattern))
                                                        Con1.PrintText($"GROUP: {group.Name}, SID: {group.Sid}\r\n");
                                            }
                                            Environment.ExitCode = 0;
                                        }
                                        else
                                            Environment.ExitCode = -1;

                                    break;
                                case "group":
                                    //using (WindowsIdentity newId = new WindowsIdentity(safeTokenHandle.DangerousGetHandle()))
                                    //using (WindowsImpersonationContext impersonatedUser = newId.Impersonate())

                                    using (var pc = new PrincipalContext(Context, domain))
                                    using (var group = GroupPrincipal.FindByIdentity(pc, IdentityType.Name,
                                        c.Arguments.GetArgument("group").Value))
                                        if (group != null)
                                        {
                                            Con1.PrintText($"SAM: {group.Name}\r\n");
                                            Con1.PrintText($"SID: {group.Sid}\r\n");
                                            Con1.PrintText($"IsSecurityGroup: {group.IsSecurityGroup}\r\n");
                                            Con1.PrintText($"Distinquished Name: {group.DistinguishedName}\r\n");

                                            if (c.Switches.HasSwitch("show-users"))
                                            {
                                                Con1.PrintText("\r\n");
                                                var pattern = c.Switches.GetSwitch("show-users").Value.Replace("*", "%");

                                                if (result.Switches.HasSwitch("impersonate-domain"))
                                                {
                                                    Console.WriteLine("impersonate");

                                                    var id = result.Switches.GetSwitch("impersonate-domain");

                                                    var domainName = id.SwitchArguments.GetArgument("domain").Value;
                                                    var userName = id.SwitchArguments.GetArgument("user").Value;
                                                    var passWord = id.SwitchArguments.GetArgument("pass").Value;

                                                    PrincipalSearchResult<Principal> oPrincipalSearchResult = group.GetMembers();
                                                    foreach (Principal user in oPrincipalSearchResult)
                                                    {
                                                        Con1.PrintText($"USER: {user.SamAccountName}, SID: {user.Sid}\r\n");
                                                    }

                                                    using (var f = new PrincipalContext(ContextType.Domain, domainName, userName, passWord))

                                                    Console.WriteLine(domain);
                                                    var members = group.Members;

                                                    foreach (var user in members)
                                                        Con1.PrintText($"USER: {user.SamAccountName}, SID: {user.Sid}\r\n");
                                                }
                                                else
                                                {
                                                    var members = group.Members;
                                                    foreach (var user in members)
                                                        Con1.PrintText($"USER: {user.SamAccountName}, SID: {user.Sid}\r\n");
                                                }

                                                Environment.ExitCode = 0;
                                            }
                                            else
                                                Environment.ExitCode = -1;

                                        }
                                    break;

                            }
                            break;
                        case "alter":
                            c = c.SubCommand;
                            switch (c.CommandText)
                            {
                                case "user":
                                    if (c.Switches.HasSwitch("set-pass"))
                                    {
                                        var SetPassSwitch = c.Switches.GetSwitch("set-pass");
                                        var u = SetPassSwitch.SwitchArguments.GetArgument("user").Value;
                                        var p = SetPassSwitch.SwitchArguments.GetArgument("pass").Value;

                                        using (var pc = new PrincipalContext(Context, domain))
                                        using (var user = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, u))
                                            if (user != null)
                                            {
                                                user.SetPassword(p);
                                                user.Save();

                                                Con1.PrintText("set-pass successfull\r\n");
                                                Environment.ExitCode = 0;
                                            }
                                            else
                                            {
                                                Environment.ExitCode = -2;
                                            }

                                        // comming soon
                                    }

                                    break;
                                case "localgroup":
                                    if (c.Switches.HasSwitch("add-domain-user"))
                                    {
                                        var AddUserSwitch = c.Switches.GetSwitch("add-domain-user");
                                        var g1 = AddUserSwitch.SwitchArguments.GetArgument("group").Value;
                                        var u1 = AddUserSwitch.SwitchArguments.GetArgument("user").Value;

                                        using (var local = new PrincipalContext(ContextType.Machine))
                                        using (var dom = new PrincipalContext(ContextType.Domain))
                                        using (var group = GroupPrincipal.FindByIdentity(local, IdentityType.Name, g1))
                                        using (var user = UserPrincipal.FindByIdentity(dom, IdentityType.SamAccountName, u1))
                                            if ((group != null) & (user != null))
                                            {
                                                group?.Members.Add(user as Principal);
                                                group?.Save();

                                                Con1.PrintText("add-domainuser successfull\r\n");
                                                Environment.ExitCode = 0;
                                            }
                                            else
                                            {
                                                Environment.ExitCode = -2;
                                            }

                                    }
                                    break;
                            }
                            break;
                        case "create":
                            c = c.SubCommand;
                            switch (c.CommandText)
                            {
                                case "user":
                                    // name and display name
                                    var name = c.Arguments.GetArgument("name").Value;
                                    var display_name = c.Arguments.GetArgument("displayname").Value;

                                    string pass = PassGen(20);
                                    if (c.Switches.HasSwitch("set-pass"))
                                    {
                                        var PassSwitch = c.Switches.GetSwitch("set-pass");
                                        pass = PassSwitch.SwitchArguments.GetArgument("password").Value;
                                    }
                                    else
                                        Console.WriteLine("GEN: " + pass);

                                    string ou = null;
                                    if (Context != ContextType.Machine)
                                        if (c.Switches.HasSwitch("ou"))
                                        {
                                            var OUSwitch = c.Switches.GetSwitch("ou");
                                            ou = OUSwitch.SwitchArguments.GetArgument("fqdn").Value.Replace("\"", "");
                                            Console.WriteLine(ou);
                                        }

                                    using (var pc = new PrincipalContext(Context, domain, ou))
                                    using (var user = new UserPrincipal(pc, name, pass, true))
                                        if (user != null)
                                        {
                                            user.PasswordNeverExpires = true;
                                            user.Save();

                                            Con1.PrintText("create user successfull\r\n");

                                            Environment.ExitCode = 0;
                                        }
                                        else
                                            Environment.ExitCode = -1;

                                    break;
                            }
                            break;
                        case "validate":
                            var u2 = c.Arguments.GetArgument("user").Value;
                            var p2 = c.Arguments.GetArgument("password").Value;

                            using (var pc = new PrincipalContext(Context, domain))
                            {
                                bool isValid = pc.ValidateCredentials(u2, p2);

                                if (isValid)
                                    Environment.ExitCode = 0;
                                else
                                    Environment.ExitCode = -1000;
                            }
                            break;
                    }
                }
                catch (PasswordException e)
                {
                    Con1.PrintError(e.Message);
                    Environment.ExitCode = -1001;
                }
                catch (PrincipalExistsException e)
                {
                    Con1.PrintError(e.Message);
                    Environment.ExitCode = -1002;
                }
                catch (UnauthorizedAccessException e)
                {
                    Con1.PrintError("access denied");
                    Environment.ExitCode = -1000;
                }
                catch (MultipleMatchesException e)
                {
                    Con1.PrintError(e.Message);
                    Environment.ExitCode = -2;
                }
                catch (Exception e)
                {
                    Con1.PrintError(e.ToString());
                    Environment.ExitCode = 2;// exception
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