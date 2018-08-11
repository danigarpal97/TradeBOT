using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using MySql.Data.MySqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient("620881625:AAED80oqutAKR3bme-RELgvkQaPlPWN2fKY");
        private static string MyConnection = "";
        private static string Channel = "";
        private static MySqlConnection dbConn = new MySqlConnection();

        static void Main(string[] args)
        {
            string[] configdata = File.ReadAllLines("config.txt");
            Channel         = configdata[0].Split('=')[1];
            string Dbip     = configdata[1].Split('=')[1];
            string Dbuser   = configdata[2].Split('=')[1];
            string Dbpass   = configdata[3].Split('=')[1];
            string Dbport   = configdata[4].Split('=')[1];

            Console.Write("CHANNEL:\t" + Channel + "\nSERVER:\t\t" + Dbip + "\nPORT:\t\t" + Dbport + "\nUSER:\t\t" + Dbuser + "\nPASS:\t\t" + Dbpass + "\n");

            MyConnection = "SslMode=none;database=pokemongotrading;server=" + Dbip + ";port=" + Dbport +";username=" + Dbuser +";password=" + Dbpass;
            dbConn.ConnectionString = MyConnection;

            if (CheckConnection())
            {
                Bot.OnMessage += Bot_OnMessage;
                
                Bot.StartReceiving();
                Console.ReadLine();
                Bot.StopReceiving();
            }
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                if (e.Message.Text == "/start" || e.Message.Text == "/ayuda" || e.Message.Text == "/help")
                {
                    string welcomemsg = "Bienvenido al bot de intercambios en Pokémon GO Málaga\n\nLista de comandos:\n---------------------\n" +
                        "/ayuda para volver a pedir este mensaje\n/codigo (tu código de amigo)\n /busco (Lista de pokemon que busques)\n/" +
                        "ofrezco (Lista de pokemon que ofreces)\n/crear para crear tu mensaje en el canal\n/actualizar para poner tu mensaje del canal al día\n" +
                        "/borrar para borrar tu mensaje del canal\n/anclar para hacer de tu mensaje el más reciente\n/previa para ver como quedaría tu mensaje en el canal";
                    Bot.SendTextMessageAsync(e.Message.Chat.Id, welcomemsg);
                }
                if (e.Message.Text.StartsWith("/codigo ") || e.Message.Text.StartsWith("/busco ") || e.Message.Text.StartsWith("/ofrezco "))
                {
                    CheckUserExists(e.Message.From.Id.ToString());
                    string data = e.Message.Text.Substring(e.Message.Text.Split(' ')[0].Length + 1);
                    string field = "";
                    if (e.Message.Text.StartsWith("/codigo "))
                    {
                        if (Regex.Replace(data, "[^0-9]", "").Length != 12)
                        {
                            Bot.SendTextMessageAsync(e.Message.Chat.Id,"El código de amigo introducido es incorrecto");
                            return;
                        }
                        field = "friendcode";
                        data = Regex.Replace(data, "[^0-9]", "");
                    }
                    if (e.Message.Text.StartsWith("/busco "))   field = "demand";
                    if (e.Message.Text.StartsWith("/ofrezco ")) field = "offer";
                    string query = "UPDATE trainers set " + field + "='" + data + "' where iduser='" + e.Message.From.Id + "';";
                    MySqlCommand command = new MySqlCommand(query,dbConn);
                    dbConn.Open();
                    if (command.ExecuteNonQuery() == 1) Bot.SendTextMessageAsync(e.Message.Chat.Id, "Información actualizada correctamente ✔️");
                    else Console.WriteLine("ERROR: HA HABIDO UN ERROR EN LA MODIFICACIÓN DE DATOS DE UN USUARIO");
                    dbConn.Close();
                }
                if (e.Message.Text == "/crear")
                {
                    string query = "select idpost,friendcode,offer,demand from trainers where iduser='" + e.Message.From.Id + "';";
                    MySqlCommand command = new MySqlCommand(query, dbConn);
                    dbConn.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    string usercode = reader["friendcode"].ToString();
                    string useroffer = reader["offer"].ToString();
                    string userdemand = reader["demand"].ToString();
                    string postid = reader["idpost"].ToString();
                    dbConn.Close();
                    if ( postid != "")
                    {
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Error: Ya tienes tu mensaje creado en el canal de intercambios, pero puedes ponerlo al día con /actualizar o borrarlo con /borrar si es lo que buscabas.");
                        return;
                    }
                    if (usercode == "" || useroffer == "" || userdemand == "")
                    {
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Error: Asegurate de especificar tu codigo de amigo, los pokémon que buscas y ofreces con /codigo /busco y /ofrezco respectivamente.");
                        return;
                    }
                    PostCreate(e.Message.From.Id.ToString(), e.Message.From.Username);
                    Bot.SendTextMessageAsync(e.Message.Chat.Id, "Mensaje creado correctamente ✔️");
                }
                if (e.Message.Text == "/actualizar")
                {
                    string query = "select idpost from trainers where iduser='" + e.Message.From.Id + "';";
                    MySqlCommand command = new MySqlCommand(query, dbConn);
                    dbConn.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    string postid = reader["idpost"].ToString();
                    dbConn.Close();
                    if (postid == "")
                    {
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Error: No tienes ningún mensaje en el canal pero puedes empezar creandolo con /crear siempre que hayas dado tus datos con /codigo /ofrezco y /busco respectivamente.");
                    }
                    else
                    {
                        PostUpdate(e.Message.From.Id.ToString(), e.Message.From.Username);
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Mensaje actualizado correctamente ✔️");
                    }
                }
                if (e.Message.Text == "/borrar")
                {
                    string query = "select idpost from trainers where iduser='" + e.Message.From.Id + "';";
                    MySqlCommand command = new MySqlCommand(query, dbConn);
                    dbConn.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    string postid = reader["idpost"].ToString();
                    dbConn.Close();
                    if (postid == "")
                    {
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Error: No tienes ningún mensaje en el canal pero puedes empezar creandolo con /crear siempre que hayas dado tus datos con /codigo /ofrezco y /busco respectivamente.");
                    }
                    else
                    {
                        PostDelete(e.Message.From.Id.ToString());
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Mensaje borrado correctamente ✔️");
                    }
                }
                if (e.Message.Text == "/anclar")
                {
                    string query = "select idpost from trainers where iduser='" + e.Message.From.Id + "';";
                    MySqlCommand command = new MySqlCommand(query, dbConn);
                    dbConn.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    string postid = reader["idpost"].ToString();
                    dbConn.Close();
                    if (postid == "")
                    {
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Error: No tienes ningún mensaje en el canal pero puedes empezar creandolo con /crear siempre que hayas dado tus datos con /codigo /ofrezco y /busco respectivamente.");
                    }
                    else
                    {
                        PostAnchor(e.Message.From.Id.ToString(), e.Message.From.Username);
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Mensaje anclado correctamente ✔️");
                    }
                }
                if (e.Message.Text == "/groupid")
                {
                    Bot.SendTextMessageAsync(e.Message.Chat.Id,"El ID de este grupo es: " + e.Message.Chat.Id);
                }
                if (e.Message.Text == "/previa")
                {
                    string query = "select idpost,friendcode,offer,demand from trainers where iduser='" + e.Message.From.Id + "';";
                    MySqlCommand command = new MySqlCommand(query, dbConn);
                    dbConn.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    string usercode = reader["friendcode"].ToString();
                    string useroffer = reader["offer"].ToString();
                    string userdemand = reader["demand"].ToString();
                    dbConn.Close();
                    if (usercode == "" || useroffer == "" || userdemand == "")
                    {
                        Bot.SendTextMessageAsync(e.Message.Chat.Id, "Error: Asegurate de especificar tu codigo de amigo, los pokémon que buscas y ofreces con /codigo /busco y /ofrezco respectivamente.");
                        return;
                    }
                    string postcontent = PostGetContent(e.Message.From.Id.ToString(), e.Message.From.Username);
                    Bot.SendTextMessageAsync(e.Message.Chat.Id, postcontent);
                }
            }
        }

        private static bool CheckConnection()
        {
            try
            {
                dbConn.Open();
                dbConn.Close();
                Console.WriteLine("ÉXITO: BASE DE DATOS CONFIGURADA CORRECTAMENTE");
                return true;
            }
            catch
            {
                Console.WriteLine("ERROR: BASE DE DATOS INACCESIBLE O MAL ESPECIFICADA");
                if (dbConn.State == System.Data.ConnectionState.Open) dbConn.Close();
                return false;
            }
        }
        private static void CheckUserExists(string Userid)
        {
            dbConn.Open();
            string query = "insert into trainers(iduser) values('" + Userid + "') on duplicate key update iduser = iduser;";
            MySqlCommand command = new MySqlCommand(query, dbConn);
            if (command.ExecuteNonQuery()!=1) Console.WriteLine("ERROR: HA HABIDO UN ERROR EN LA INSERCIÓN/CHEQUEO DE EXISTENCIA DE UN USUARIO");
            dbConn.Close();
        }

        private static string PostGetContent(string Userid, string Username)
        {
            try
            {
                string post = "";
                string query = "select friendcode,offer,demand from trainers where iduser='" + Userid + "';";
                MySqlCommand command = new MySqlCommand(query, dbConn);
                dbConn.Open();
                MySqlDataReader reader = command.ExecuteReader();
                reader.Read();
                string usercode = reader["friendcode"].ToString().Insert(8, "-").Insert(4, "-");
                string useroffer = reader["offer"].ToString();
                string userdemand = reader["demand"].ToString();
                dbConn.Close();
                post = "⚫Entrenador: " + usercode + " ( @" + Username + " )\n🔶Busca: " + userdemand + "\n🔷Ofrece: " + useroffer;
                return post;
            }
            catch
            {
                return "";
            }
        }

        private static bool PostCreate(string Userid, string Username)
        {
            try
            {
                string postcontent = PostGetContent(Userid, Username);
                string postid = Bot.SendTextMessageAsync(Channel, postcontent).Result.MessageId.ToString();
                string query = "update trainers set idpost='" + postid + "' where iduser='" + Userid + "';";
                MySqlCommand command = new MySqlCommand(query, dbConn);
                dbConn.Open();
                command.ExecuteNonQuery();
                dbConn.Close();
                return true;
            }
            catch
            {
                if (dbConn.State == System.Data.ConnectionState.Open) dbConn.Close();
                Console.WriteLine("ERROR: HA HABIDO UN ERROR EN LA CREACION DE UN MENSAJE");
                return false;
            }
        }
        private static bool PostAnchor(string Userid, string Username)
        {
            try
            {
                PostDelete(Userid);
                PostCreate(Userid, Username);
                return true;
            }
            catch
            {
                if (dbConn.State == System.Data.ConnectionState.Open) dbConn.Close();
                Console.WriteLine("ERROR: HA HABIDO UN ERROR EN EL ANCLAJE DE UN MENSAJE");
                return false;
            }
        }
        private static bool PostUpdate(string Userid, string Username)
        {
            try
            {
                string postcontent = PostGetContent(Userid, Username);
                string query = "select idpost from trainers where iduser='" + Userid + "';";
                MySqlCommand command = new MySqlCommand(query, dbConn);
                MySqlDataReader reader = command.ExecuteReader();
                reader.Read();
                int postid = Convert.ToInt32(reader["idpost"]);
                Bot.EditMessageTextAsync(Channel, postid, postcontent);
                return true;
            }
            catch
            {
                if (dbConn.State == System.Data.ConnectionState.Open) dbConn.Close();
                Console.WriteLine("ERROR: HA HABIDO UN ERROR EN LA ACTUALIZACION DE UN MENSAJE");
                return false;
            }
        }
        private static bool PostDelete(string Userid)
        {
            try
            {
                string query = "select idpost from trainers where iduser='" + Userid + "';";
                MySqlCommand command = new MySqlCommand(query, dbConn);
                dbConn.Open();
                MySqlDataReader reader = command.ExecuteReader();
                reader.Read();
                int postid = Convert.ToInt32(reader["idpost"]);
                reader.Close();
                Bot.DeleteMessageAsync(Channel, postid);

                string query2 = "update trainers set idpost=null where iduser='" + Userid + "';";
                MySqlCommand command2 = new MySqlCommand(query2, dbConn);
                command2.ExecuteNonQuery();
                dbConn.Close();
                return true;
            }
            catch
            {
                if (dbConn.State == System.Data.ConnectionState.Open) dbConn.Close();
                Console.WriteLine("ERROR: HA HABIDO UN ERROR EN EL BORRADO DE UN MENSAJE");
                return false;
            }
        }
    }
}
