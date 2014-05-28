using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using OpenPop;
using OpenPop.Mime;
using OpenPop.Pop3;
using OpenPop.Common;

namespace Email_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string hostname = "pop.gmail.com";
        int port = 995;
        bool useSsl = true;
        string username = "recent:djda9l.scam@gmail.com";
        string password = "jahallo123";

        public List<EmailEntry> ListBoxData { get; set; } 

        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {

            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);

                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();

                // We want to download all messages
                List<Message> allMessages = new List<Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }

                client.Disconnect();

                // Now return the fetched messages
                return allMessages;
            }
        }

        public MainWindow()
        {
            InitializeComponent();            
        }

        //Functions
        public string readyUpListBoxData(MessagePart content, int truncate, OpenPop.Mime.Header.RfcMailAddress displayName) 
        {
            string noLineBreaks = content.GetBodyAsText().ToString().Replace(System.Environment.NewLine, " ");
            string partOfBody = noLineBreaks.Length <= truncate ? noLineBreaks : noLineBreaks.Substring(0, truncate) + " ..";
            string fromDisplayName = displayName.DisplayName.ToString();
            if (displayName.DisplayName == "")
            {
                fromDisplayName = displayName.Address.ToString();
            }
            fromDisplayName += " <" + displayName.Address.ToString() + ">";

            return partOfBody;
        }

        //Buttons
        private void NewEmailButton_Click(object sender, RoutedEventArgs e)
        {
               
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
            List<Message> allEmail = FetchAllMessages(hostname, port, useSsl, username, password);

            ListBoxData = new List<EmailEntry> { };
            foreach (Message singleEmail in allEmail)
            {
                MessagePart theEmailTxt = singleEmail.FindFirstPlainTextVersion();
                MessagePart theEmailHTML = singleEmail.FindFirstHtmlVersion();

                // For show in listbox
                string partOfBody = readyUpListBoxData(theEmailTxt, 40, singleEmail.Headers.From);
                //string fromDisplayName = singleEmail.Headers.From.DisplayName.ToString();
                //if (fromDisplayName == "")
                //{
                //    fromDisplayName = singleEmail.Headers.From.Address.ToString();
                //}
                //fromDisplayName += " <" + singleEmail.Headers.From.Address.ToString() + ">";


                ListBoxData.Add(new EmailEntry { from = fromDisplayName, bodyPart = partOfBody, messageID = singleEmail.Headers.MessageId.ToString() });

                // SQL
                string bodyTxt = theEmailTxt.GetBodyAsText().ToString();
                if (theEmailHTML != null)
                {
                    bodyTxt = theEmailHTML.GetBodyAsText().ToString();
                }

                SQLiteConnection conn = new SQLiteConnection("Data Source=db.s3db;Version=3;");
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand("INSERT INTO `emails`(`messageId`,`subject`,`body`,`sender`) VALUES ('" + singleEmail.Headers.MessageId + "','" + singleEmail.Headers.Subject + "',@Message,'" + singleEmail.Headers.From.Address + "')");
                cmd.Parameters.Add(new SQLiteParameter("@Message", bodyTxt));
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        private void EmailEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {                     
            EmailEntry selectedId = EmailEntry.SelectedItems[0] as EmailEntry;

            string myConnString = "Data Source=db.s3db;Version=3;";
            string mySelectQuery = "SELECT * FROM `emails` WHERE `messageId`='" + selectedId.messageID + "'";
            SQLiteConnection sqConnection = new SQLiteConnection(myConnString);
            SQLiteCommand sqCommand = new SQLiteCommand(mySelectQuery, sqConnection);
            sqConnection.Open();
            try
            {
                SQLiteDataReader sqReader = sqCommand.ExecuteReader();
                while (sqReader.Read())
                {
                    from.Text = sqReader.GetString(sqReader.GetOrdinal("sender"));
                    subject.Text = sqReader.GetString(sqReader.GetOrdinal("subject"));
                    MailBody.NavigateToString(sqReader.GetString(sqReader.GetOrdinal("body")));
                }
                sqReader.Close();
            }
            finally
            {
                sqConnection.Close();
            }
        }
    }       

    public class EmailEntry
    {
        public string from { get; set; }
        public string bodyPart { get; set; }
        public string messageID { get; set; }
    }

    public class SingleEmail
    {

    }

    class SQLiteDatabase
    {
        String dbConnection;

        /// <summary>
        ///     Default Constructor for SQLiteDatabase Class.
        /// </summary>
        public SQLiteDatabase()
        {
            dbConnection = "Data Source=db.s3db";
        }

        /// <summary>
        ///     Single Param Constructor for specifying the DB file.
        /// </summary>
        /// <param name="inputFile">The File containing the DB</param>
        public SQLiteDatabase(String inputFile)
        {
            dbConnection = String.Format("Data Source={0}", inputFile);
        }

        /// <summary>
        ///     Single Param Constructor for specifying advanced connection options.
        /// </summary>
        /// <param name="connectionOpts">A dictionary containing all desired options and their values</param>
        public SQLiteDatabase(Dictionary<String, String> connectionOpts)
        {
            String str = "";
            foreach (KeyValuePair<String, String> row in connectionOpts)
            {
                str += String.Format("{0}={1}; ", row.Key, row.Value);
            }
            str = str.Trim().Substring(0, str.Length - 1);
            dbConnection = str;
        }

        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        public DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                SQLiteConnection cnn = new SQLiteConnection(dbConnection);
                cnn.Open();
                SQLiteCommand mycommand = new SQLiteCommand(cnn);
                mycommand.CommandText = sql;
                SQLiteDataReader reader = mycommand.ExecuteReader();
                dt.Load(reader);
                reader.Close();
                cnn.Close();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return dt;
        }

        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        public int ExecuteNonQuery(string sql)
        {
            MessageBox.Show(sql);
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
            int rowsUpdated = mycommand.ExecuteNonQuery();
            cnn.Close();
            return rowsUpdated;
        }

        /// <summary>
        ///     Allows the programmer to retrieve single items from the DB.
        /// </summary>
        /// <param name="sql">The query to run.</param>
        /// <returns>A string.</returns>
        public string ExecuteScalar(string sql)
        {
            SQLiteConnection cnn = new SQLiteConnection(dbConnection);
            cnn.Open();
            SQLiteCommand mycommand = new SQLiteCommand(cnn);
            mycommand.CommandText = sql;
            object value = mycommand.ExecuteScalar();
            cnn.Close();
            if (value != null)
            {
                return value.ToString();
            }
            return "";
        }

        /// <summary>
        ///     Allows the programmer to easily update rows in the DB.
        /// </summary>
        /// <param name="tableName">The table to update.</param>
        /// <param name="data">A dictionary containing Column names and their new values.</param>
        /// <param name="where">The where clause for the update statement.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Update(String tableName, Dictionary<String, String> data, String where)
        {
            String vals = "";
            Boolean returnCode = true;
            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, String> val in data)
                {
                    vals += String.Format(" {0} = '{1}',", val.Key.ToString(), val.Value.ToString());
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", tableName, vals, where));
            }
            catch
            {
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily delete rows from the DB.
        /// </summary>
        /// <param name="tableName">The table from which to delete.</param>
        /// <param name="where">The where clause for the delete.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Delete(String tableName, String where)
        {
            Boolean returnCode = true;
            try
            {
                this.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));
            }
            catch (Exception fail)
            {
                MessageBox.Show(fail.Message);
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily insert into the DB
        /// </summary>
        /// <param name="tableName">The table into which we insert the data.</param>
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Insert(String tableName, Dictionary<String, String> data)
        {
            String columns = "";
            String values = "";
            Boolean returnCode = true;
            foreach (KeyValuePair<String, String> val in data)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                values += String.Format(" '{0}',", val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", tableName, columns, values));
            }
            catch (Exception fail)
            {
                MessageBox.Show(fail.Message);
                returnCode = false;
            }
            return returnCode;
        }

        /// <summary>
        ///     Allows the programmer to easily delete all data from the DB.
        /// </summary>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearDB()
        {
            DataTable tables;
            try
            {
                tables = this.GetDataTable("select NAME from SQLITE_MASTER where type='table' order by NAME;");
                foreach (DataRow table in tables.Rows)
                {
                    this.ClearTable(table["NAME"].ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Allows the user to easily clear all data from a specific table.
        /// </summary>
        /// <param name="table">The name of the table to clear.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearTable(String table)
        {
            try
            {

                this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }



}
