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

        public void InsertPerson(string name, string lastname, string profilepic64s)
        {
            using (SQLiteConnection conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();

                string sql = $"insert into People (name, lastname, profilepic) values ('{name}', '{lastname}', '{profilepic64s}')";
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                command.ExecuteNonQuery();
            }
        }

        public bool DeletePerson(string sId)
        {
            int id;
            if (int.TryParse(sId, out id))
            {
                using (SQLiteConnection conn = new SQLiteConnection(GetConnectionString()))
                {
                    conn.Open();

                    string sql = $"delete from People where id='{id}'";
                    SQLiteCommand command = new SQLiteCommand(sql, conn);
                    command.ExecuteNonQuery();
                }
                return true;
            }
            return false;
        }

        public List<dynamic> GetPeopleList()
        {
            List<dynamic> list = new List<dynamic>();
            using (SQLiteConnection conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                string sql = "select * from People";
                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic dd = new DynamicDictionary();
                            dd.Id = reader["id"];
                            dd.Name = reader["name"];
                            dd.LastName = reader["lastname"];
                            dd.ProfilePic = reader["profilepic"];
                            list.Add(dd);
                        }
                    }
                }
            }
            return list;
        }

        public dynamic GetPerson(string sId)
        {
            dynamic dd = new DynamicDictionary();
            int id;
            if (int.TryParse(sId, out id))
            {
                using (SQLiteConnection conn = new SQLiteConnection(GetConnectionString()))
                {
                    conn.Open();
                    string sql = $"select * from People where id='{id}'";
                    using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dd.Id = reader["id"];
                                dd.Name = reader["name"];
                                dd.LastName = reader["lastname"];
                                dd.ProfilePic = reader["profilepic"];
                            }
                        }
                    }
                }
            }
            return dd;
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
                    string createTableQuery = "CREATE TABLE People (id INTEGER PRIMARY KEY AUTOINCREMENT, name VARCHAR(20), lastname VARCHAR(20), profilepic TEXT)";
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
