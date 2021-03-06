﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        private static UserDatabaseType[] _userDb; //Can I use SynchronizedCollection<T> Class? Each user is stored in one element
        private static TelegramBotClient _bot;
        private static readonly Dictionary<string, string> AvailableFunctionsList = new Dictionary<string, string>()
        {
            { "factors","Find factors of a number; 6-> 1,2,3,6"},
            { "factorize","Factorize a number to prime factors" },
            { "gcd","Find greatest common divisor of two numbers" },
            { "lcm","Find least common multiple of two numbers" },
            { "mod","Find remainder of division of two numbers" },
            { "detectprime","Detects if a number is prime or not" },
            { "quadratic","Solve a quadratic equation" },
            { "2varequation","Solve two variable two equation system" },
            { "average","Get average of some numbers" },
            //Non-Mathematical functions
            { "about","About this bot" },
            { "app","See the main Rare Math Calculations application on bazaar" },
            { "donate","Donate to me!" }
        };
        private const string ShowMenuAtLast = "\n\nUse /menu to choose another function.";
        private static readonly string[] HelpInsideFunctions = {
            "*Factors*\nFind factors of a number.\nFor example 6 results in 1,2,3 and 6\nSend a number to bot to find it's factors.",
            "*Factorize*\nFactorize a number to prime factors.\nSend a number to factorize the number." ,
            "*GCD*\nFind greatest common divisor of two numbers.\nSend a numbers like `number1` `number2`.\nFor example send:\n43895 4343" ,
            "*LCM*\nFind least common multiple of two numbers.\nSend a numbers like `number1` `number2`.\nFor example send:\n94533 4453" ,
            "*Remainder*\nFind remainder of division of two numbers.\nSend a numbers like `dividend` `divisor`.\nFor example send:\n656757 535" ,
            "*Prime Detector*\nDetects if a number is prime or not.\nSend a number to bot to check.",
            "*Quadratic Equation Solver*\nSolve a quadratic equation. Suppose the equation `a𝑥²+b𝑥+c=0`, then enter `a`, `b` and `c` split by white space.\nFor example send bot \"`3 -5 1.5`\" where a = 3, b = -5 and c = 1.5",
            "*Two Variable Two Equation Solver*\nSuppose the system\n`ax+by=c`\n`dx+ey=f`\nThen enter `a`, `b`, `c`, `d`, `e` and `f` split by whitespace.\nFor example send bot \"`3 -5 1.5 64 -435 0`\" where a = 3, b = -5, c = 1.5, d = 64, e = -435 and f = 0",
            "*Average Calculator*\nEnter numbers split by whitespace to calculate their average.\nExample: 12 5.4 6.56 -43.4 -767 343 1 -54"
        };
        private static string _dbPath = AppContext.BaseDirectory + "/user_database.json";
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
                _bot = new TelegramBotClient(args[0]);
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
                    _dbPath = args[++i];
                else
                    Extra.WriteWarning("Unrecognized argument \"" + args[i] + "\"");
            }
            #endregion
            #region Load database
            if (!System.IO.File.Exists(_dbPath))
            {
                _userDb = new UserDatabaseType[0];
            }
            else
            {
                _userDb = UserDatabaseActions.Load(_dbPath);
                if (_userDb == null)
                    return;
            }
            #endregion
            //Setup bot
            var me = _bot.GetMeAsync().Result;
            Console.Title = me.Username;
            _bot.OnMessage += BotOnMessageReceived;
            _bot.StartReceiving();
            Console.WriteLine($"[ {DateTime.Now:dd MMMM yyyy HH:mm:ss} ]: Starting @{me.Username} bot.");
            //Run user db manager; It deletes each user that have not accessed database for more than 30 days
            new Task(() =>
            {
                Console.WriteLine($"[ { DateTime.Now:dd MMMM yyyy HH:mm:ss} ]: Started database cleaner daemon.");
                const int oneDay = 1000 * 60 * 60 * 24;
                DateTime now;
                List<UserDatabaseType> users;
                List<int> removedId;
                while (true)
                {
                    now = DateTime.Now;
                    users = new List<UserDatabaseType>(_userDb);
                    removedId = new List<int>();
                    for (int i = 0; i < users.Count; i++)
                    {
                        if ((now - _userDb[i].LastUse).TotalDays >= 30)
                        {
                            removedId.Add(users[i].UserId);
                            users.RemoveAt(i);
                        }
                    }
                    if (removedId.Count != 0)
                    {
                        _userDb = users.ToArray();
                        foreach(int i in removedId)
                            Console.WriteLine($"[ { DateTime.Now:dd MMMM yyyy HH:mm:ss} ]: A user has been removed from database. ID: " + i);
                    }
                    Thread.Sleep(oneDay);
                }
            }).Start();
            //Save database every 15 mins
            new Task(() =>
            {
                Console.WriteLine($"[ { DateTime.Now:dd MMMM yyyy HH:mm:ss} ]: Started database saver daemon.");
                const int fiftyMin = 1000 * 60 * 15;
                while (true)
                {
                    Thread.Sleep(fiftyMin);
                    if (UserDatabaseActions.Save(_userDb, _dbPath))
                        Console.WriteLine($"[ { DateTime.Now:dd MMMM yyyy HH:mm:ss} ]: Saved database at " + _dbPath);
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
            _bot.StopReceiving();
        }
        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            Message message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text || string.IsNullOrWhiteSpace(message.Text)) //Only accept text
                return;
            int userId = message.From.Id;
            #region Check DB
            byte pageIn;
            {
                byte? pageInN = Extra.GetPageIn(_userDb, userId);
                if (pageInN == null) //First time user is here
                {
                    pageInN = 0;
                    Array.Resize(ref _userDb, _userDb.Length + 1);
                    _userDb[_userDb.Length - 1] = new UserDatabaseType()
                    {
                        PageIn = 0,
                        LastUse = DateTime.Now,
                        UserId = userId
                    };
                }
                pageIn = pageInN ?? 0;
                
            }
            #endregion
            switch(message.Text)
            {
                case "/start":
                    await _bot.SendTextMessageAsync(message.Chat.Id, "Hello and welcome to Rare Math Calculations bot!\nUse /menu to get a list of commands.");
                    break;
                case "/menu":
                    {
                        StringBuilder sb = new StringBuilder("Click on one of the commands to use it:\n");
                        foreach (KeyValuePair<string, string> function in AvailableFunctionsList)
                        {
                            sb.Append("/");
                            sb.Append(function.Key);
                            sb.Append(" : ");
                            sb.AppendLine(function.Value);
                        }
                        await _bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                    }
                    break;
                case "/help":
                    await _bot.SendTextMessageAsync(message.Chat.Id, pageIn == 0 ? "Use /menu and choose a command from it." : HelpInsideFunctions[pageIn-1],ParseMode.Markdown);
                    break;
                case "/about":
                    await _bot.SendTextMessageAsync(message.Chat.Id, "Created with \u2764 by Hirbod Behnam! Contact me here: @ThyCrow\n\nAlso you can view the project source at:\nhttps://github.com/HirbodBehnam/Maths-Bot");
                    break;
                case "/donate":
                    await _bot.SendTextMessageAsync(message.Chat.Id, "Thanks for donating to me!\u2764\nI currently only accept *Bitcoin*, *Bitcoin Gold*, *Ethereum*, *Monero* and *ZCash*.\nFor me, even 1$ per month would be ok but more is welcome :) Here are my wallet addresses:\nBitcoin\n`1XDgEkpnkJ7hC8Kwv5adfaDC1Z3FrkwsK`\nBitcoin Gold:\n`GcNgxfyR3nnAsD3Nhuckvq14sXYuDFkK9P`\nEthereum:\n`0xbb527a28B76235E1C125206B7CcFF944459b4894`\nMonero:\n`43GGA2kcGwqBXpGKwt2zFBicz2wh41zX3JNSnj12dXDiWzJHn44tpVT1eiUt8UMym828A1BBgaboBTn1usnCNHZqMhhuDXz`\nZCash:\n`t1ZKYrYZCjxDYvo6mQaLZi3gNe2a6MydUo3`\n\nWant to buy some? Check here: https://www.coinbase.com and https://exchanging.ir/sell/", ParseMode.Markdown,true);
                    break;
                case "/app":
                    await _bot.SendTextMessageAsync(message.Chat.Id, "If you are an Android user you can simply download my Rare Math Calculations app on CafeBazaar. It's free and no Internet connection is needed. It also have some other parts and calculations you can use!\nhttps://cafebazaar.ir/app/com.hirbod.maths/\n\niOS users can also use my website https://hirbodbehnam.github.io/ or they can install my app from sibApp https://new.sibapp.com/applications/maths");
                    break;
                case "/factors":
                    _userDb = Extra.SetPageIn(_userDb, userId, 1);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[0] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/factorize":
                    _userDb = Extra.SetPageIn(_userDb, userId, 2);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[1] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/gcd":
                    _userDb = Extra.SetPageIn(_userDb, userId, 3);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[2] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/lcm":
                    _userDb = Extra.SetPageIn(_userDb, userId, 4);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[3] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/mod":
                    _userDb = Extra.SetPageIn(_userDb, userId, 5);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[4] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/detectprime":
                    _userDb = Extra.SetPageIn(_userDb, userId, 6);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[5] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/quadratic":
                    _userDb = Extra.SetPageIn(_userDb, userId, 7);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[6]+ ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/2varequation":
                    _userDb = Extra.SetPageIn(_userDb, userId, 8);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[7] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                case "/average":
                    _userDb = Extra.SetPageIn(_userDb, userId, 9);
                    await _bot.SendTextMessageAsync(message.Chat.Id, HelpInsideFunctions[8] + ShowMenuAtLast, ParseMode.Markdown);
                    break;
                default:
                    if (pageIn == 0)
                        await _bot.SendTextMessageAsync(message.Chat.Id, "Choose a function from /menu");
                    else
                    {
                        switch (pageIn)
                        {
                            case 1: //Factors
                                {
                                    uint number;
                                    try
                                    {
                                        number = uint.Parse(message.Text);
                                        if (number == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => //Send process to background to avoid busy threads
                                    {
                                        uint[] factors = MathCore.Factors(number);
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append(number);
                                        sb.Append(" has ");
                                        sb.Append(factors.Length);
                                        sb.AppendLine(" factors.");
                                        sb.Append("The factors of ");
                                        sb.Append(number);
                                        sb.AppendLine(" are:");
                                        foreach (uint i in factors)
                                        {
                                            if (sb.Length >= 4084)//Telegram Max Message Length - number of chars in 2^32 - chars in '\n'
                                            {
                                                _bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                                                sb = new StringBuilder();
                                            }
                                            sb.Append(i);
                                            sb.Append("\n");
                                        }
                                        _bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                                    }).Start();
                                }
                                break;
                            case 2: //Factorize
                                {
                                    uint number;
                                    try
                                    {
                                        number = uint.Parse(message.Text);
                                        if (number < 2)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => //Send process to background to avoid busy threads
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append(number);
                                        sb.Append(" =");
                                        foreach (uint i in MathCore.Factorize(number)) //Show result
                                        {
                                            sb.Append(' ');
                                            sb.Append(i);
                                            sb.Append(" x");
                                        }
                                        sb.Length--;
                                        _bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                                    }).Start();
                                }
                                break;
                            case 3: //GCD
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 2)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Please send two numbers split with whitespace.");
                                        break;
                                    }
                                    uint num1, num2;
                                    try
                                    {
                                        num1 = uint.Parse(splitMessage[0]);
                                        num2 = uint.Parse(splitMessage[1]);
                                        if (num1 == 0 || num2 == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => _bot.SendTextMessageAsync(message.Chat.Id, "The GCD of " + num1 + " and " + num2 + " is `" + MathCore.GCD(num1, num2) + "`", ParseMode.Markdown))
                                        .Start();
                                }
                                break;
                            case 4: //LCM
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 2)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Please send two numbers split with whitespace.");
                                        break;
                                    }
                                    uint num1, num2;
                                    try
                                    {
                                        num1 = uint.Parse(splitMessage[0]);
                                        num2 = uint.Parse(splitMessage[1]);
                                        if (num1 == 0 || num2 == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 4294967296.");
                                        break;
                                    }
                                    new Task(() => _bot.SendTextMessageAsync(message.Chat.Id, "The LCM of " + num1 + " and " + num2 + " is `" + (num1 * num2 / MathCore.GCD(num1, num2)) + "`", ParseMode.Markdown))
                                        .Start();
                                }
                                break;
                            case 5: //Mod
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 2)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Please send two numbers split with whitespace.");
                                        break;
                                    }
                                    ulong num1, num2;
                                    try
                                    {
                                        num1 = ulong.Parse(splitMessage[0]);
                                        num2 = ulong.Parse(splitMessage[1]);
                                        if (num2 == 0)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 1 and 18446744073709551616.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 1 and 18446744073709551616.");
                                        break;
                                    }
                                    await _bot.SendTextMessageAsync(message.Chat.Id, "The remainder of division of " + num1 + " / " + num2 + " is `" + num1 % num2 + "`", ParseMode.Markdown);
                                }
                                break;
                            case 6: //Detect prime
                                {
                                    uint number;
                                    try
                                    {
                                        number = uint.Parse(message.Text);
                                        if (number < 2)
                                            throw new FormatException();
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is not in valid format. Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Your number is too big! Enter a number between 2 and 4294967296.");
                                        break;
                                    }
                                    new Task(() =>
                                    {
                                        uint res = MathCore.DetectPrime(number);
                                        _bot.SendTextMessageAsync(message.Chat.Id, number + (res == 1 ? " IS PRIME." : " IS NOT prime. It can be divided by " + res));
                                    }).Start();
                                }
                                break;
                            case 7: //Quadratic equation
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 3)//Check for numbers
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Please send bot three numbers.");
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
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "I wonder how long you've been typing? You overflowed a double type!");
                                        break;
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "The value you entered is not valid.");
                                        break;
                                    }
                                    if (a == 0)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "`a` cannot be 0.", ParseMode.Markdown);
                                        break;
                                    }
                                    double delta = b * b - 4 * a * c;
                                    if (delta < 0)
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "`delta` is less than 0.", ParseMode.Markdown);
                                    else if (delta == 0)
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "`x` = `" + ((-b) / (2 * a)) + "`", ParseMode.Markdown);
                                    else
                                    {
                                        delta = Math.Sqrt(delta);
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "`x1` = `" + ((-b + delta) / (2 * a)) + "`\n`x2` = `" + ((-b - delta) / (2 * a)) + "`", ParseMode.Markdown);
                                    }
                                }
                                break;
                            case 8: //Two Variable Two Equation Solver
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    if (splitMessage.Length != 6)//Check for numbers
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Please send bot six numbers.");
                                        break;
                                    }
                                    double a, b, c, d, e, f;
                                    try
                                    {
                                        a = double.Parse(splitMessage[0]);
                                        b = double.Parse(splitMessage[1]);
                                        c = double.Parse(splitMessage[2]);
                                        d = double.Parse(splitMessage[3]);
                                        e = double.Parse(splitMessage[4]);
                                        f = double.Parse(splitMessage[5]);
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "I wonder how long you've been typing? You overflowed a double type!");
                                        break;
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "The value you entered is not valid.");
                                        break;
                                    }
                                    double det = a * e - b * d;
                                    if(det == 0)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Lines are parallel.");
                                        break;
                                    }
                                    double detX = c * e - f * b, detY = a * f - d * c;
                                    await _bot.SendTextMessageAsync(message.Chat.Id, "x = `" + (detX / det) + "`\ny = `" + (detY / det) + "`",ParseMode.Markdown);
                                }
                                break;
                            case 9: //Average
                                {
                                    string[] splitMessage = message.Text.Split(' ');
                                    double sum = 0;
                                    try
                                    {
                                        sum += splitMessage.Sum(s => Convert.ToDouble(s));
                                    }
                                    catch (FormatException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Invalid number.");
                                        break;
                                    }
                                    catch (OverflowException)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id, "Sum of numbers are two big.");
                                        break;
                                    }
                                    await _bot.SendTextMessageAsync(message.Chat.Id, "The average is `" + (sum / splitMessage.Length) + "`",ParseMode.Markdown);
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
