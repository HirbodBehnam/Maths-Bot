using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Maths_Bot
{
    class Program
    {
        private static UserDatabaseType[] UserDB; //Can I use SynchronizedCollection<T> Class? Each user is stored in one element
        private static TelegramBotClient bot;
        private static readonly Dictionary<string, string> AvailableFucntionsList = new Dictionary<string, string>()
        {
            { "factors","Find factors of a number; 6-> 1,2,3,6"},
            { "factorize","Factorize a number to prime factors" },
            { "gcd","Find greatest common divisor of two numbers" },
            { "lcm","Find least common multiple of two numbers" },
            { "mod","Find remainder of division of two numbers" },
            { "detectprime","Detects if a number is prime or not" },
            { "quadratic","Solve a quadratic equation" },
            //Non-Mathematical functions
            { "about","About this bot" },
            { "app","See the main Rare Math Calculations application on bazaar" },
            { "donate","Donate to me!" }
        };
        private static readonly string[] HelpInsideFunctions = new string[]
        {
            "*Factors*\nFind factors of a number.\nFor example 6 results in 1,2,3 and 6\nSend a number to bot to find it's factors.\n\nUse /menu to choose another function.",
            "*Factorize*\nFactorize a number to prime factors.\nSend a number to factorize the number.\n\nUse /menu to choose another function." ,
            "*GCD*\nFind greatest common divisor of two numbers.\nSend a numbers like `number1` `number2`.\nFor example send:\n43895 4343\n\nUse /menu to choose another function." ,
            "*LCM*\nFind least common multiple of two numbers.\nSend a numbers like `number1` `number2`.\nFor example send:\n94533 4453\n\nUse /menu to choose another function." ,
            "*Remainder*\nFind remainder of division of two numbers.\nSend a numbers like `dividend` `divisor`.\nFor example send:\n656757 535\n\nUse /menu to choose another function." ,
            "*Prime Detector*\nDetects if a number is prime or not.\n\nUse /menu to choose another function.",
            "*Quadratic Equation Solver*\nSolve a quadratic equation. Suppose the equation `ax^2+bx+c=0`, then enter `a`, `b` and `c` split by white space.\nFor example send bot \"3 -5 1.5\" where a = 3, b = -5 and c = 1.5\n\nUse /menu to choose another function."
        };
        private static string DBPath = AppContext.BaseDirectory + "/user_database.json";
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet \"Maths Bot.dll\" BOT_TOKEN [-l] [--db Location]");
                Console.WriteLine("Example: dotnet \"Maths Bot.dll\" MY_TOKEN -l --db \"/etc/Bot/myConf.db\"");
                Console.WriteLine("Pass \"-l\" if you are running bot as service.");
                Console.WriteLine("Database location by default is current working directory and the file \"user_database.json\"");
                return;
            }
            Console.WriteLine("Rare Math Calculations Bot By Hirbod Behnam");
            Console.WriteLine("Source at https://github.com/HirbodBehnam/Maths-Bot");
            #region Parse Token and setup bot
            try
            {
                bot = new TelegramBotClient(args[0]);
            }
            catch (ArgumentException e)
            {
                Extra.WriteError(e.Message);
                return;
            }
            #endregion
            #region Parse Arguments
            bool UseLoop = false;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-l")
                    UseLoop = true;
                else if (args[i] == "--db")
                    DBPath = args[++i];
                else
                    Extra.WriteWarning("Unrecognized argument \"" + args[i] + "\"");
            }
            #endregion
            #region Load database
            if (!System.IO.File.Exists(DBPath))
            {
                UserDB = new UserDatabaseType[0];
            }
            else
            {
                UserDB = UserDatabaseActions.Load(DBPath);
                if (UserDB == null)
                    return;
            }
            #endregion
            //Setup bot
            var me = bot.GetMeAsync().Result;
            Console.Title = me.Username;
            bot.OnMessage += BotOnMessageReceived;
            bot.StartReceiving();
            Console.WriteLine($"[ {DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss")} ]: Starting @{me.Username} bot.");
            //Run user db manager; It deletes each user that have not accessed database for more than 30 days
            new Task(() =>
            {
                Console.WriteLine($"[ { DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss")} ]: Started database cleaner daemon.");
                const int OneDay = 1000 * 60 * 60 * 24;
                DateTime now;
                List<UserDatabaseType> Users;
                List<int> RemovedID;
                while (true)
                {
                    now = DateTime.Now;
                    Users = new List<UserDatabaseType>(UserDB);
                    RemovedID = new List<int>();
                    for (int i = 0; i < Users.Count; i++)
                    {
                        if ((now - UserDB[i].LastUse).TotalDays >= 30)
                        {
                            RemovedID.Add(Users[i].UserID);
                            Users.RemoveAt(i);
                        }
                    }
                    if (RemovedID.Count != 0)
                    {
                        UserDB = Users.ToArray();
                        foreach(int i in RemovedID)
                            Console.WriteLine($"[ { DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss")} ]: A user has been removed from database. ID: " + i);
                    }
                    Thread.Sleep(OneDay);
                }
            }).Start();
            //Save database every 15 mins
            new Task(() =>
            {
                Console.WriteLine($"[ { DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss")} ]: Started database saver daemon.");
                const int FiftyMin = 1000 * 60 * 15;
                while (true)
                {
                    Thread.Sleep(FiftyMin);
                    if (UserDatabaseActions.Save(UserDB, DBPath))
                        Console.WriteLine($"[ { DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss")} ]: Saved database.");
                }
            }).Start();
            /*
             * If you are going to make a service out of this application you may have to pass -l to application
             */
            if (UseLoop)
            {
                Console.WriteLine("Press Ctrl+C to stop application");
                while (true)
                    Thread.Sleep(int.MaxValue);//Stop main thread and wait for Ctrl+C
            }
            else
            {
                Console.WriteLine("Press enter to stop bot.");
                Console.ReadLine();//Wait until user presses enter
            }
            bot.StopReceiving();
        }
        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            Message message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text || string.IsNullOrWhiteSpace(message.Text)) //Only accept text
                return;
            int UserID = message.From.Id;
            #region Check DB
            byte PageIn;
            {
                byte? PageInN = Extra.GetPageIn(UserDB, UserID);
                if (PageInN == null) //First time user is here
                {
                    PageInN = 0;
                    Array.Resize(ref UserDB, UserDB.Length + 1);
                    UserDB[UserDB.Length - 1] = new UserDatabaseType()
                    {
                        PageIn = 0,
                        LastUse = DateTime.Now,
                        UserID = UserID
                    };
                }
                PageIn = PageInN ?? 0;
                
            }
            #endregion
            switch(message.Text)
            {
                case "/start":
                    await bot.SendTextMessageAsync(message.Chat.Id, "Hello and welcome to Rare Math Calculations bot!\nUse /menu to get a list of commands.");
                    break;
                case "/menu":
                    {
                        StringBuilder sb = new StringBuilder("Click on one of the commands to use it:\n");
                        foreach (KeyValuePair<string, string> function in AvailableFucntionsList)
                        {
                            sb.Append("/");
                            sb.Append(function.Key);
                            sb.Append(" : ");
                            sb.AppendLine(function.Value);
                        }
                        await bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                    }
                    break;
                case "/help":
                    await bot.SendTextMessageAsync(message.Chat.Id, PageIn == 0 ? "Use /menu and choose a command from it." : HelpInsideFunctions[PageIn-1],ParseMode.Markdown);
                    break;
                case "/about":
                    await bot.SendTextMessageAsync(message.Chat.Id, "Created with \u2764 by Hirbod Behnam! Contact me here: @ThyCrow\n\nAlso you can view the project source at:\nhttps://github.com/HirbodBehnam/Maths-Bot");
                    break;
                case "/donate":
                    await bot.SendTextMessageAsync(message.Chat.Id, "Thanks for donating to me!\u2764\nI currently only accept *Bitcoin*, *Bitcoin Gold*, *Ethereum*, *Monero* and *ZCash*.\nFor me, even 1$ per month would be ok but more is welcome :) Here are my wallet addresses:\nBitcoin\n`1XDgEkpnkJ7hC8Kwv5adfaDC1Z3FrkwsK`\nBitcoin Gold:\n`GcNgxfyR3nnAsD3Nhuckvq14sXYuDFkK9P`\nEthereum:\n`0xbb527a28B76235E1C125206B7CcFF944459b4894`\nMonero:\n`43GGA2kcGwqBXpGKwt2zFBicz2wh41zX3JNSnj12dXDiWzJHn44tpVT1eiUt8UMym828A1BBgaboBTn1usnCNHZqMhhuDXz`\nZCash:\n`t1ZKYrYZCjxDYvo6mQaLZi3gNe2a6MydUo3`\n\nWant to buy some? Check here: https://www.coinbase.com and https://exchanging.ir/sell/", ParseMode.Markdown,true);
                    break;
                case "/app":
                    await bot.SendTextMessageAsync(message.Chat.Id, "If you are an Android user you can simply download my Rare Math Calculations app on CafeBazaar. It's free and no Internet connection is needed. It also have some other parts and calculations you can use!\nhttps://cafebazaar.ir/app/com.hirbod.maths/\n\niOS users can also use my website https://hirbodbehnam.github.io/ or they can install my app from sibApp https://new.sibapp.com/applications/maths");
                    break;
                case "/factors":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 1);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[0],ParseMode.Markdown);
                    break;
                case "/factorize":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 2);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[1], ParseMode.Markdown);
                    break;
                case "/gcd":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 3);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[2], ParseMode.Markdown);
                    break;
                case "/lcm":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 4);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[3], ParseMode.Markdown);
                    break;
                case "/mod":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 5);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[4], ParseMode.Markdown);
                    break;
                case "/detectprime":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 6);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[5], ParseMode.Markdown);
                    break;
                case "/quadratic":
                    UserDB = Extra.SetPageIn(UserDB, UserID, 7);
                    await bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[6], ParseMode.Markdown);
                    break;
                default:
                    if (PageIn == 0)
                        await bot.SendTextMessageAsync(message.Chat.Id, "Choose a function from /menu");
                    else
                    {
                        switch (PageIn)
                        {
                            case 1: //Factors
                                {
                                    uint Number;
                                    try
                                    {
                                        Number = uint.Parse(message.Text);
                                        if (Number == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => //Send process to background to avoid busy threads
                                    {
                                        uint[] facotrs = MathCore.Factors(Number);
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append(Number);
                                        sb.Append(" has ");
                                        sb.Append(facotrs.Length);
                                        sb.AppendLine(" factors.");
                                        sb.Append("The factors of ");
                                        sb.Append(Number);
                                        sb.AppendLine(" are:");
                                        foreach (uint i in facotrs)
                                        {
                                            if (sb.Length >= 4084)//Telegram Max Message Length - number of chars in 2^32 - chars in '\n'
                                            {
                                                bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                                                sb = new StringBuilder();
                                            }
                                            sb.Append(i);
                                            sb.Append("\n");
                                        }
                                        bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                                    }).Start();
                                }
                                break;
                            case 2: //Factorize
                                {
                                    uint Number;
                                    try
                                    {
                                        Number = uint.Parse(message.Text);
                                        if (Number < 2)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => //Send process to background to avoid busy threads
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append(Number);
                                        sb.Append(" =");
                                        foreach (uint i in MathCore.Factorize(Number)) //Show result
                                        {
                                            sb.Append(' ');
                                            sb.Append(i);
                                            sb.Append(" x");
                                        }
                                        sb.Length--;
                                        bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                                    }).Start();
                                }
                                break;
                            case 3: //GCD
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 2)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Please send two numbers split with whitespace.");
                                        break;
                                    }
                                    uint Num1, Num2;
                                    try
                                    {
                                        Num1 = uint.Parse(splitMessage[0]);
                                        Num2 = uint.Parse(splitMessage[1]);
                                        if (Num1 == 0 || Num2 == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => bot.SendTextMessageAsync(message.Chat.Id, "The GCD of " + Num1 + " and " + Num2 + " is `" + MathCore.GCD(Num1, Num2) + "`", ParseMode.Markdown))
                                        .Start();
                                }
                                break;
                            case 4: //LCM
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 2)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Please send two numbers split with whitespace.");
                                        break;
                                    }
                                    uint Num1, Num2;
                                    try
                                    {
                                        Num1 = uint.Parse(splitMessage[0]);
                                        Num2 = uint.Parse(splitMessage[1]);
                                        if (Num1 == 0 || Num2 == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => bot.SendTextMessageAsync(message.Chat.Id, "The LCM of " + Num1 + " and " + Num2 + " is `" + (Num1 * Num2 / MathCore.GCD(Num1, Num2)) + "`", ParseMode.Markdown))
                                        .Start();
                                }
                                break;
                            case 5: //Mod
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 2)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Please send two numbers split with whitespace.");
                                        break;
                                    }
                                    ulong Num1, Num2;
                                    try
                                    {
                                        Num1 = ulong.Parse(splitMessage[0]);
                                        Num2 = ulong.Parse(splitMessage[1]);
                                        if (Num2 == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 18446744073709551616.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 18446744073709551616.");
                                        break;
                                    }
                                    await bot.SendTextMessageAsync(message.Chat.Id, "The remainder of division of " + Num1 + " / " + Num2 + " is `" + Num1 % Num2 + "`", ParseMode.Markdown);
                                }
                                break;
                            case 6: //Detect prime
                                {
                                    uint Number;
                                    try
                                    {
                                        Number = uint.Parse(message.Text);
                                        if (Number < 2)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    new Task(() =>
                                    {
                                        uint res = MathCore.DetectPrime(Number);
                                        bot.SendTextMessageAsync(message.Chat.Id, Number + (res == 1 ? " IS PRIME." : " IS NOT prime. It can be divided by " + res));
                                    }).Start();
                                }
                                break;
                            case 7: //Quadratic equation
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 3)//Check for numbers
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "Please send bot three numbers.");
                                        break;
                                    }
                                    double a, b, c;
                                    try
                                    {
                                        a = double.Parse(splitMessage[0]);
                                        b = double.Parse(splitMessage[1]);
                                        c = double.Parse(splitMessage[2]);
                                    }
                                    catch (OverflowException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "I wonder how long you've been typing? You overflowed a double type!");
                                        break;
                                    }
                                    catch (FormatException)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "The value you entered is not valid.");
                                        break;
                                    }
                                    if (a == 0)
                                    {
                                        await bot.SendTextMessageAsync(message.Chat.Id, "`a` cannot be 0.", ParseMode.Markdown);
                                        break;
                                    }
                                    double delta = b * b - 4 * a * c;
                                    if (delta < 0)
                                        await bot.SendTextMessageAsync(message.Chat.Id, "`delta` is less than 0.", ParseMode.Markdown);
                                    else if (delta == 0)
                                        await bot.SendTextMessageAsync(message.Chat.Id, "`x` = `" + ((-b) / (2 * a)) + "`", ParseMode.Markdown);
                                    else
                                    {
                                        delta = Math.Sqrt(delta);
                                        await bot.SendTextMessageAsync(message.Chat.Id, "`x1` = `" + ((-b + delta) / (2 * a)) + "`\n`x2` = `" + ((-b - delta) / (2 * a)) + "`", ParseMode.Markdown);
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
