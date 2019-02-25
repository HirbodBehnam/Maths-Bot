using System;
using System.IO;
using Newtonsoft.Json;

namespace Maths_Bot
{
    public struct UserDatabaseType
    {
        public int UserID; //Telegram User ID
        public byte PageIn; //The function user is using
        public DateTime LastUse; //Last usage of bot; if more than 30 days, bot will delete user from database
    }
    public static class UserDatabaseActions
    {
        private struct UserDatabaseTypeArray
        {
            public UserDatabaseType[] db;
        }
        /// <summary>
        /// Saves database file
        /// </summary>
        /// <param name="db">Data base object</param>
        /// <param name="path">Path of database(default to current directory and user_database.json</param>
        /// <returns></returns>
        public static bool Save(UserDatabaseType[] db,string path)
        {
            try
            {
                UserDatabaseTypeArray dataAry = new UserDatabaseTypeArray
                {
                    db = db
                };
                File.WriteAllText(path,JsonConvert.SerializeObject(dataAry));
            }catch(Exception ex)
            {
                Extra.WriteError(ex.Message);
                return false;
            }
            return true;
        }
        /// <summary>
        /// Loads the database
        /// </summary>
        /// <param name="path">Path of database(default to current directory and user_database.json</param>
        /// <returns></returns>
        public static UserDatabaseType[] Load(string path)
        {
            try
            {
                string SavedData = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UserDatabaseTypeArray>(SavedData).db;
            }
            catch (FileNotFoundException)
            {
                Extra.WriteError("File \"user_database.js\" not found.");
                return null;
            }
            catch (Exception ex)
            {
                Extra.WriteError(ex.Message);
                return null;
            }
        }
    }
}
