using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Maths_Bot
{
    class Program
    {
        private static TelegramBotClient bot;
        private static readonly Dictionary<string, string> availableFucntionsList = new Dictionary<string, string>()
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
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Please pass your bot token as argument.");
                return;
            }
            //Parse Token and setup bot
            try
            {
                bot = new TelegramBotClient(args[0]);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Error: " + e.Message);
                return;
            }
            //Setup bot
            var me = bot.GetMeAsync().Result;
            Console.Title = me.Username;
            bot.OnMessage += BotOnMessageReceived;
            bot.StartReceiving();
            Console.WriteLine($"[ {DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss")} ]: Starting @{me.Username} bot. Press enter to stop bot.");
            Console.WriteLine("Press enter to stop bot.");
            Console.ReadLine(); //Wait until user presses enter
            bot.StopReceiving();
        }
        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            Message message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text || string.IsNullOrWhiteSpace(message.Text)) //Only accept text
                return;
            string[] splitMessage = message.Text.Split(' ');
            switch(splitMessage[0].ToLower())
            {
                case "/start":
                    await bot.SendTextMessageAsync(message.Chat.Id, "Hello and welcome to Rare Math Calculations bot!\nUse /help to get a list of commands.");
                    break;
                case "/help":
                    {
                        StringBuilder sb = new StringBuilder("Click on one of the commands to see it's usage:\n");
                        foreach (KeyValuePair<string, string> function in availableFucntionsList)
                        {
                            sb.Append("/");
                            sb.Append(function.Key);
                            sb.Append(" : ");
                            sb.AppendLine(function.Value);
                        }
                        await bot.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                    }
                    break;
                case "/about":
                    await bot.SendTextMessageAsync(message.Chat.Id, "Created with \u2764 by Hirbod Behnam! Contact me here: @ThyCrow\n\nAlso you can view the project source at:\nhttps://github.com/HirbodBehnam/Maths-Bot");
                    break;
                case "/donate":
                    await bot.SendTextMessageAsync(message.Chat.Id, "Thanks for donating to me!\u2764\nI currently only accept *Bitcoin*, *Bitcoin Gold*, *Ethereum*, *Monero* and *ZCash*.\nFor me, even 1$ per month would be ok but more is welcome :) Here are my wallet addresses:\nBitcoin\n`1XDgEkpnkJ7hC8Kwv5adfaDC1Z3FrkwsK`\nBitcoin Gold:\n`GcNgxfyR3nnAsD3Nhuckvq14sXYuDFkK9P`\nEthereum:\n`0xbb527a28B76235E1C125206B7CcFF944459b4894`\nMonero:\n`43GGA2kcGwqBXpGKwt2zFBicz2wh41zX3JNSnj12dXDiWzJHn44tpVT1eiUt8UMym828A1BBgaboBTn1usnCNHZqMhhuDXz`\nZCash:\n`t1ZKYrYZCjxDYvo6mQaLZi3gNe2a6MydUo3`\n\nWant to buy some? Check here: https://www.coinbase.com and https://exchanging.ir/sell/", ParseMode.Markdown,true);
                    break;
                case "/app":
                    await bot.SendTextMessageAsync(message.Chat.Id, "If you are an Android user you can simply download my Rare Math Calculations app on CafeBazaar. It's free and no Internet connection is needed. It also have some other parts and calculations you can use!\nhttps://cafebazaar.ir/app/com.hirbod.maths/");
                    break;
                case "/factors":
                    {
                        if (splitMessage.Length == 1)//Check for number
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Find factors of a number.\nFor example 6 results in 1,2,3 and 6\nUsage:\n  /factors `number`\nExample:\n   /factors 96", ParseMode.Markdown);
                            break;
                        }
                        uint Number;
                        try
                        {
                            Number = uint.Parse(splitMessage[1]);
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
                            StringBuilder sb = new StringBuilder("The factors of ");
                            sb.Append(Number);
                            sb.AppendLine(" are:");
                            foreach (uint i in MathCore.Factors(Number))
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
                case "/factorize":
                    {
                        if (splitMessage.Length == 1)//Check for number
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Factorize a number to prime numbers.\nUsage:\n  /factors `number`\nExample:\n   /factors 96", ParseMode.Markdown);
                            break;
                        }
                        uint Number;
                        try
                        {
                            Number = uint.Parse(splitMessage[1]);
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
                case "/gcd":
                    {
                        if (splitMessage.Length < 3)//Check for numbers
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Find greatest common divisor of two numbers.\nUsage:\n  /gcd `number1` `number2`\nExample:\n   /gcd 438,932", ParseMode.Markdown);
                            break;
                        }
                        uint Num1, Num2;
                        try
                        {
                            Num1 = uint.Parse(splitMessage[1]);
                            Num2 = uint.Parse(splitMessage[2]);
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
                        new Task(() => bot.SendTextMessageAsync(message.Chat.Id, "The GCD of " + Num1 + " and " + Num2 + " is `" + MathCore.GCD(Num1, Num2) + "`",ParseMode.Markdown))
                            .Start();
                    }
                    break;
                case "/lcm":
                    {
                        if (splitMessage.Length < 3)//Check for numbers
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Find least common multiple of two numbers.\nUsage:\n  /lcm `number1` `number2`\nExample:\n   /lcm 438,932", ParseMode.Markdown);
                            break;
                        }
                        uint Num1, Num2;
                        try
                        {
                            Num1 = uint.Parse(splitMessage[1]);
                            Num2 = uint.Parse(splitMessage[2]);
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
                        new Task(() => bot.SendTextMessageAsync(message.Chat.Id, "The LCM of " + Num1 + " and " + Num2 + " is `" + (Num1 * Num2 / MathCore.GCD(Num1, Num2)) + "`",ParseMode.Markdown))
                            .Start();
                    }
                    break;
                case "/mod":
                    {
                        if (splitMessage.Length < 3)//Check for numbers
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Find remainder of division of two numbers.\nUsage:\n  /mod `dividend` `divisor`\nExample:\n   /mod 43252,432", ParseMode.Markdown);
                            break;
                        }
                        ulong Num1, Num2;
                        try
                        {
                            Num1 = ulong.Parse(splitMessage[1]);
                            Num2 = ulong.Parse(splitMessage[2]);
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
                        await bot.SendTextMessageAsync(message.Chat.Id, "The remainder of division of " + Num1 + " / " + Num2 + " is `" + Num1 % Num2 + "`",ParseMode.Markdown);
                    }
                    break;
                case "/detectprime":
                    {
                        if (splitMessage.Length == 1)//Check for number
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Detect if a number is prime.\nUsage:\n  /detectprime `number`\nExample:\n   /detectprime 372", ParseMode.Markdown);
                            break;
                        }
                        uint Number;
                        try
                        {
                            Number = uint.Parse(splitMessage[1]);
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
                case "/quadratic":
                    {
                        if (splitMessage.Length < 4)//Check for numbers
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "Solve a quadratic equation.\nUsage:\nSuppose the equation `ax^2+bx+c=0` then use /quadratic `a` `b` `c` to solve the equation.\nExample:\n   /quadratic 4.3 -43 -5.54", ParseMode.Markdown);
                            break;
                        }
                        double a, b, c;
                        try
                        {
                            a = double.Parse(splitMessage[1]);
                            b = double.Parse(splitMessage[2]);
                            c = double.Parse(splitMessage[3]);
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
                        if(a == 0)
                        {
                            await bot.SendTextMessageAsync(message.Chat.Id, "`a` cannot be 0.",ParseMode.Markdown);
                            break;
                        }
                        double delta = b * b - 4 * a * c;
                        if(delta < 0)
                            await bot.SendTextMessageAsync(message.Chat.Id, "`delta` is less than 0.", ParseMode.Markdown);
                        else if(delta == 0)
                            await bot.SendTextMessageAsync(message.Chat.Id, "`x` = `" + ((-b) / (2*a)) + "`", ParseMode.Markdown);
                        else
                        {
                            delta = Math.Sqrt(delta);
                            await bot.SendTextMessageAsync(message.Chat.Id, "`x1` = `" + ((-b + delta)/ (2 * a)) + "`\n`x2` = `" + ((-b - delta) / (2 * a))+"`", ParseMode.Markdown);
                        }
                    }
                    break;
                default:
                    await bot.SendTextMessageAsync(message.Chat.Id, "Invalid command!\nType /help to see the list of commands.");
                    break;
            }
        }
    }
}
