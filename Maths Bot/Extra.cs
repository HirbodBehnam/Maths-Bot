using System;

namespace Maths_Bot
{
    public static class Extra
    {
        /// <summary>
        /// Writes an error to console
        /// </summary>
        /// <param name="message">Error message</param>
        public static void WriteError(object message)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error");
            Console.ForegroundColor = color;
            Console.Write("]: ");
            Console.WriteLine(message);
        }
        /// <summary>
        /// Writes a warning to console
        /// </summary>
        /// <param name="message">Warning message</param>
        public static void WriteWarning(object message)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Warning");
            Console.ForegroundColor = color;
            Console.Write("]: ");
            Console.WriteLine(message);
        }
        /// <summary>
        /// Get the page currently user is in.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static byte? GetPageIn(UserDatabaseType[] db,int userId)
        {
            foreach (UserDatabaseType u in db)
                if (u.UserId == userId)
                    return u.PageIn;
            return null;
        }
        public static UserDatabaseType[] SetPageIn(UserDatabaseType[] db, int userId,byte page)
        {
            for(int i = 0;i<db.Length;i++)
                if(db[i].UserId == userId)
                {
                    db[i].PageIn = page;
                    db[i].LastUse = DateTime.Now;
                    return db;
                }
            return new UserDatabaseType[0];
        }
    }
}
