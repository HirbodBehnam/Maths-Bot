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
        /// <param name="UserID"></param>
        /// <returns></returns>
        public static byte? GetPageIn(UserDatabaseType[] db,int UserID)
        {
            foreach (UserDatabaseType u in db)
                if (u.UserID == UserID)
                    return u.PageIn;
            return null;
        }
        public static UserDatabaseType[] SetPageIn(UserDatabaseType[] db, int UserID,byte Page)
        {
            for(int i = 0;i<db.Length;i++)
                if(db[i].UserID == UserID)
                {
                    db[i].PageIn = Page;
                    db[i].LastUse = DateTime.Now;
                    return db;
                }
            return new UserDatabaseType[0];
        }
    }
}
