using System;
using System.Linq;
using System.Threading;

namespace Genealogy
{
    internal static class Utility
    {
        private const int Sleep = 800;

        /// <summary>
        /// Prints a title based on the <paramref name="text"/> provided.
        /// </summary>
        /// <param name="text"></param>
        public static void PrintTitle(string text)
        {
            Console.Clear();
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            switch (text)
            {
                case "Title":
                    Console.WriteLine("\n\t   ____           _ __       ______");
                    Console.WriteLine("\t  / __/__ ___ _  (_) /_ __  /_  __/______ ___");
                    Console.WriteLine("\t / _// _ `/  ' \\/ / / // /   / / / __/ -_) -_)");
                    Console.WriteLine("\t/_/  \\_,_/_/_/_/_/_/\\_, /   /_/ /_/  \\__/\\__/");
                    Console.WriteLine("\t                   /___/");
                    break;
                case "Main Menu":
                    Console.WriteLine("\n\t   __  ___     _        __  ___");
                    Console.WriteLine("\t  /  |/  /__ _(_)__    /  |/  /__ ___  __ __");
                    Console.WriteLine("\t / /|_/ / _ `/ / _ \\  / /|_/ / -_) _ \\/ // /");
                    Console.WriteLine("\t/_/  /_/\\_,_/_/_//_/ /_/  /_/\\__/_//_/\\_,_/\n");
                    break;
                case "Add Member":
                    Console.WriteLine("\n\t   ___     __   __  __  ___          __");
                    Console.WriteLine("\t  / _ |___/ /__/ / /  |/  /__ __ _  / /  ___ ____");
                    Console.WriteLine("\t / __ / _  / _  / / /|_/ / -_)  ' \\/ _ \\/ -_) __/");
                    Console.WriteLine("\t/_/ |_\\_,_/\\_,_/ /_/  /_/\\__/_/_/_/_.__/\\__/_/\n");
                    break;
                case "List Members":
                    Console.WriteLine("\n\t   ____                 __     __  ___          __");
                    Console.WriteLine("\t  / __/__ ___ _________/ /    /  |/  /__ __ _  / /  ___ _______");
                    Console.WriteLine("\t _\\ \\/ -_) _ `/ __/ __/ _ \\  / /|_/ / -_)  ' \\/ _ \\/ -_) __(_-<");
                    Console.WriteLine("\t/___/\\__/\\_,_/_/  \\__/_//_/ /_/  /_/\\__/_/_/_/_.__/\\__/_/ /___/\n");
                    break;
                case "Selected Member":
                    Console.WriteLine("\n\t   ____    __        __         __  __  ___          __");
                    Console.WriteLine("\t  / __/__ / /__ ____/ /____ ___/ / /  |/  /__ __ _  / /  ___ ____");
                    Console.WriteLine("\t _\\ \\/ -_) / -_) __/ __/ -_) _  / / /|_/ / -_)  ' \\/ _ \\/ -_) __/");
                    Console.WriteLine("\t/___/\\__/_/\\__/\\__/\\__/\\__/\\_,_/ /_/  /_/\\__/_/_/_/_.__/\\__/_/\n");
                    break;
                case "Update Member":
                    Console.WriteLine("\n\t  __  __        __     __        __  ___          __");
                    Console.WriteLine("\t / / / /__  ___/ /__ _/ /____   /  |/  /__ __ _  / /  ___ ____");
                    Console.WriteLine("\t/ /_/ / _ \\/ _  / _ `/ __/ -_) / /|_/ / -_)  ' \\/ _ \\/ -_) __/");
                    Console.WriteLine("\t\\____/ .__/\\_,_/\\_,_/\\__/\\__/ /_/  /_/\\__/_/_/_/_.__/\\__/_/");
                    Console.WriteLine("\t    /_/\n");
                    break;
                case "Relatives":
                    Console.WriteLine("\n\t   ___      __     __  _");
                    Console.WriteLine("\t  / _ \\___ / /__ _/ /_(_)  _____ ___");
                    Console.WriteLine("\t / , _/ -_) / _ `/ __/ / |/ / -_|_-<");
                    Console.WriteLine("\t/_/|_|\\__/_/\\_,_/\\__/_/|___/\\__/___/\n");
                    break;
                default:
                    break;
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Colors a <paramref name="text"/> and writes it to the screen.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void WriteInColor(string text, ConsoleColor color = ConsoleColor.Yellow)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// When the user types in an answer it gets
        /// displayed to the sceen in DarkCyan.
        /// </summary>
        /// <returns></returns>
        public static string ReadLine()
        {
            Console.CursorVisible = true;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            var input = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorVisible = false;
            return input;
        }

        /// <summary>
        /// Writes a <paramref name="text"/> delayed to the screen.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="delay"></param>
        public static void WriteDelayed(string text, ConsoleColor color = ConsoleColor.Yellow, int delay = 20)
        {
            Console.ForegroundColor = color;
            Thread.Sleep(delay);
            foreach (var letter in text)
            {
                Console.Write(letter);
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(delay);
                }
            }
            Thread.Sleep(Sleep);
            Console.ForegroundColor = ConsoleColor.White;
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Writes a error message with the provided <paramref name="text"/>.
        /// The message will be displayed in DarkRed and
        /// then will be removed after one and a half second.
        /// </summary>
        /// <param name="text"></param>
        internal static void ErrorMessage(string text)
        {
            WriteDelayed(text, ConsoleColor.DarkRed);
            Thread.Sleep(400);
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine(new string(' ', 80));
            Console.SetCursorPosition(0, Console.CursorTop - 2);
            Console.WriteLine(new string(' ', 80));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        /// <summary>
        /// Forces the user to enter a valid name.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A valid name.</returns>
        public static string GetName(string type)
        {
            var name = "";
            while (true)
            {
                Console.Write($"\tEnter {type} name: ");
                name = ReadLine();
                if (EarlyExit(name))
                {
                    FamilyTree.MainMenu();
                }
                else if (name == "")
                {
                    ErrorMessage($"\tYou have to enter a {type} name.");
                }
                else if (name.Any(char.IsDigit))
                {
                    ErrorMessage("\tYou can't have digits in the name.");
                }
                else
                {
                    break;
                }
            }
            return char.ToUpper(name[0]) + name[1..];
        }

        /// <summary>
        /// Forces the user to enter a valid age.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A valkid age.</returns>
        public static int GetAge(string type)
        {
            int age;
            while (true)
            {
                WriteInColor($"\tEnter a {type} age: ");
                if (int.TryParse(ReadLine(), out age))
                {
                    break;
                }
                else
                {
                    ErrorMessage("\tInvalid choice. try again");
                }
            }
            return age;
        }

        /// <summary>
        /// Forces the user to enter a valid date but lets
        /// the user skip if the user enters an empty string.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A <see cref="string"/> that can be converted into a
        /// <see cref="DateTime"/> or <see langword="null"/>.</returns>
        public static DateTime? GetDateTime(string type)
        {
            while (true)
            {
                var date = GetInput($"\tEnter date of {type}: ");
                if (EarlyExit(date))
                {
                    FamilyTree.MainMenu();
                }

                if (SkipEntry(date))
                {
                    return null;
                }

                if (IsDateTimeConvertable(date))
                {
                    return Convert.ToDateTime(date);
                }
                else
                {
                    ErrorMessage("\tPlease use the format (yyyy-mm-dd)!");
                }
            }
        }

        /// <summary>
        /// Tells the user that the member already exists and asks if
        /// the user wants to create the member anyway.
        /// </summary>
        /// <param name="member"></param>
        /// <returns>The user's answer.</returns>
        public static string DoesAlreadyExist(string member)
        {
            WriteDelayed($"\tA member named {member} does already exist.\n", ConsoleColor.DarkRed);
            WriteInColor($"\tDo you want to create {member} anyway?(y/n)");
            return ReadLine();
        }

        /// <summary>
        /// Types an error message saying that the <paramref name="text"/> doesn't
        /// exist and asks the user to create the <paramref name="text"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>User input.</returns>
        public static string BigFail(string text)
        {
            WriteDelayed($"\t{text} doesn't seem to exist in the database.\n", ConsoleColor.DarkRed);
            WriteInColor($"\tDo you want to create {text}(y/n)? ");
            return ReadLine();
        }

        /// <summary>
        /// Lets the user know that the action was a success!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="action"></param>
        public static void Success(string item, string action = "created")
        {
            WriteDelayed($"\t{item} has successfully been {action}!\n", ConsoleColor.DarkGreen);
            Thread.Sleep(Sleep);
        }

        /// <summary>
        /// Asks the user a question followd by (y/n).
        /// </summary>
        /// <param name="question"></param>
        /// <returns>The user's answer.</returns>
        public static string GetInput(string question, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.Write(question);
            return ReadLine();
        }

        /// <summary>
        /// Promts the user for an answer.
        /// </summary>
        /// <param name="text"></param>
        /// <returns><see langword="true"/> if user types in "y", otwerwise <see langword="false"/>´.</returns>
        public static bool MakeAChoice(string text, ConsoleColor color = ConsoleColor.White)
        {
            var choice = GetInput(text, color);
            if (choice.ToLower() == "y")
            {
                return true;
            }
            return false;
        } 

        public static bool EarlyExit(string choice) => choice == "0";

        public static bool SkipEntry(string choice) => choice == "";

        public static bool IsDateTimeConvertable(string input)
        {
            try
            {
                Convert.ToDateTime(input);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Just wanted to see if I could make my own method that did the same thing as int.TryParse.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="result"></param>
        /// <returns><see langword="true"/> if the parse was successfull, otherwise <see langword="false"/>.</returns>
        public static bool TryParse(string s, out int result)
        {
            try
            {
                result = int.Parse(s);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }
    }
}