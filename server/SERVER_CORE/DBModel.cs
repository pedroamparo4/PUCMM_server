using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.SERVER_CORE
{
    public class DBModel
    {
        const string _databaseName = "MyDatabase";
        SQLiteConnection connection;

        public DBModel()
        {

        }

        public SQLiteConnection GetSQLiteInstance()
        {
            if (connection == null)
            {
                connection = new SQLiteConnection(GetConnectionString());
            }

            return connection;
        }

        public void CreateDatabase()
        {
            if (!File.Exists($"{_databaseName}.sqlite"))
            {
                SQLiteConnection.CreateFile($"{_databaseName}.sqlite");
            }            
        }

        public void CreatePeopleTable()
        {
            CreateDatabase();
            var tableExistsQuery = "SELECT * FROM sqlite_master WHERE name ='People' and type='table'";
            SQLiteConnection con = GetSQLiteInstance();
            con.Open();
            using (SQLiteCommand cmd = new SQLiteCommand(tableExistsQuery, con))
            {
                string result = (string)cmd.ExecuteScalar();

                // If table doesn't exist then create it
                if (string.IsNullOrEmpty(result))
                {
                    string createTableQuery = "CREATE TABLE People (Id INTEGER PRIMARY KEY, Name VARCHAR(20), LastName VARCHAR(20), ProfilePic TEXT)";
                    using (SQLiteCommand createTableCommand = new SQLiteCommand(createTableQuery, con))
                    {
                        createTableCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public string GetConnectionString()
        {
            return $"Data Source={_databaseName}.sqlite;Version=3;";
        }

    }
}
