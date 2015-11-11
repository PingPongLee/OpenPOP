using System;
using System.ComponentModel;
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
using System.IO;
using System.Data;
using System.Data.SQLite;
using OpenPop;
using OpenPop.Mime;
using OpenPop.Pop3;
using OpenPop.Common;
using System.Reflection;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Email_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        string hostname = Properties.Settings.Default.pop_host;
        int port = Properties.Settings.Default.pop_port;
        bool useSsl = Properties.Settings.Default.pop_usessl;
        string username = "recent:" + Properties.Settings.Default.username;
        string password = Properties.Settings.Default.password;

        //public List<EmailEntry> ListBoxData { get; set; }
        public ObservableCollection<EmailEntry> ListBoxData { get; set; }
        /// <summary>
        /// Gets a List containing all emailmessages
        /// </summary>
        /// <param name="hostname">POP Hostname</param>
        /// <param name="port">POP port</param>
        /// <param name="useSsl">Use SSL</param>
        /// <param name="username">The username to logon with</param>
        /// <param name="password">Password to logon with</param>
        /// <returns>Messages</returns>
        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            using (Pop3Client client = new Pop3Client())
            {
                client.Connect(hostname, port, useSsl);
                client.Authenticate(username, password);
                int messageCount = client.GetMessageCount();
                List<Message> allMessages = new List<Message>(messageCount);
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                }
                client.Disconnect();
                return allMessages;
            }
        }
        
        public MainWindow()
        {
            ListBoxData = new ObservableCollection<EmailEntry>();
            InitializeComponent();           
            this.DataContext = this;
            getMailsFromDb();
        }

        //Functions
        /// <summary>
        /// Gets all mails from Database sorted by date and time
        /// </summary>
        public void getMailsFromDb()
        {
            string myConnString = "Data Source=db.s3db;Version=3;";
            string mySelectQuery = "SELECT * FROM `emails` ORDER BY `date` DESC, `time` DESC";
            SQLiteConnection sqConnection = new SQLiteConnection(myConnString);
            SQLiteCommand sqCommand = new SQLiteCommand(mySelectQuery, sqConnection);
            sqConnection.Open();
            MessageBox.Show("Hallo");
            try
            {
                //ListBoxData = new List<EmailEntry> { };
                SQLiteDataReader sqReader = sqCommand.ExecuteReader();
                while (sqReader.Read())
                {
                    string from = sqReader.GetString(sqReader.GetOrdinal("sender"));
                    string subject = sqReader.GetString(sqReader.GetOrdinal("subject"));
                    string msgid = sqReader.GetString(sqReader.GetOrdinal("messageId"));            
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ListBoxData.Add(new EmailEntry { from = from, subject = subject, messageID = msgid });
                        //EmailList.ItemTemplate.Triggers. <- Ændre baggrundsfarven.
                    });
                }
                sqReader.Close();
            }
            catch
            {
                MessageBox.Show("Problems reading mails from database!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                sqConnection.Close();
            }
        }
        /// <summary>
        /// Processes and returns emails with data to fit in a ListBox for overview
        /// </summary>
        /// <param name="data">The data to process</param>
        public void readyUpListBoxData(ListBoxDataClass data)
        {
            MessagePart theEmailTxt = data.theMessage.FindFirstPlainTextVersion();
            //string noLineBreaks = theEmailTxt.GetBodyAsText().ToString().Replace(System.Environment.NewLine, " ");
            //data.partOfBody = noLineBreaks.Length <= data.truncate ? noLineBreaks : noLineBreaks.Substring(0, data.truncate) + " ..";
            data.subject = data.theMessage.Headers.Subject;
            data.displayName = data.theMessage.Headers.From.DisplayName.ToString();
            if (data.displayName == "")
            {
                data.displayName = data.theMessage.Headers.From.Address.ToString();
            }
            data.displayName += " <" + data.theMessage.Headers.From.Address.ToString() + ">";
        }
        /// <summary>
        /// Simple check function, that returns true if the Message provided has no header data
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <returns>True/False</returns>
        public bool isSpam(string message) //Super smart antispam mechanism!
        {
            if(message == "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Decryption function, that decrypts the string given, based on AES encryption. Key and Initialization Vector is needed.
        /// </summary>
        /// <param name="cipherText">The message to decrypt</param>
        /// <param name="Key">AES Key</param>
        /// <param name="IV">AES Initialization vector</param>
        /// <returns>Returns plain text string</returns>
        static string DecryptMail(string cipherText, string Key, string IV) // Encryption function
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                byte[] convText = Convert.FromBase64String(cipherText);
                rijAlg.Key = Convert.FromBase64String(Key);
                rijAlg.IV = Convert.FromBase64String(IV);

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(convText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }
            return plaintext;
        }

        //Buttons
        private void NewEmailButton_Click(object sender, RoutedEventArgs e)
        {
            NewMail newemail = new NewMail();
            newemail.DataContext = this;
            newemail.Show();
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GetEmail_Click(object sender, RoutedEventArgs e)
        {
            EmailList.SelectedIndex = -1;
            ListBoxData.Clear();

            BackgroundWorker getNewMail = new BackgroundWorker();
            getNewMail.DoWork += newEmail;
            getNewMail.RunWorkerAsync();
        }
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var options = new OptionsScreen();
            options.ShowDialog();
        }

        private void EmailEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(EmailList.SelectedIndex != -1)
            {
                EmailEntry selectedId = EmailList.SelectedItems[0] as EmailEntry;

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
                        date.Content = sqReader.GetString(sqReader.GetOrdinal("time")) + " " + sqReader.GetString(sqReader.GetOrdinal("date"));
                        MailBody.NavigateToString(sqReader.GetString(sqReader.GetOrdinal("body")));

                        if (sqReader.GetString(sqReader.GetOrdinal("read")) == "0")
                        {
                            /*string myUpdateQuery = "UPDATE `emails` SET `read`='1' WHERE `messageId`='" + selectedId.messageID + "'";
                            SQLiteCommand updateFromRead = new SQLiteCommand(myUpdateQuery, sqConnection);
                            updateFromRead.ExecuteNonQuery();*/
                            // Koden ovenfor har problemer med at hvis man vælger en ny emailentry inden den er færdig med at skrive, så låser det hele.
                        }
                    }
                    sqReader.Close();
                }
                finally
                {
                    sqConnection.Close();
                }
            }  
        }
    
        // BackgroundWorkers / Threads
        private void newEmail(object sender, DoWorkEventArgs e)
        {
            List<Message> allEmail = FetchAllMessages(hostname, port, useSsl, username, password);         

            //ListBoxData = new List<EmailEntry> { };
            foreach (Message singleEmail in allEmail)
            {
                MessagePart theEmailTxt = singleEmail.FindFirstPlainTextVersion();
                MessagePart theEmailHTML = singleEmail.FindFirstHtmlVersion();

                // For show in listbox
                var mailData = new ListBoxDataClass { theMessage = singleEmail, truncate = 40 };
                readyUpListBoxData(mailData);
                if(isSpam(singleEmail.Headers.MessageId) != true)
                {
                    string bodyTxt = theEmailTxt.GetBodyAsText().ToString();
                    if (theEmailHTML != null)
                    {
                        bodyTxt = theEmailHTML.GetBodyAsText().ToString();
                    }

                    if (singleEmail.Headers.UnknownHeaders["isEncrypted"] == "true")
                    { 
                        try
                        {
                            using (RijndaelManaged myRijndael = new RijndaelManaged())
                            {
                                bodyTxt = bodyTxt.Replace("\r\n\r\n\r\n---\r\nDenne e-mail er fri for virus og malware fordi avast! Antivirus beskyttelse er aktiveret.\r\nhttp://www.avast.com\r\n", "");
                                bodyTxt = DecryptMail(bodyTxt, singleEmail.Headers.UnknownHeaders["key"], singleEmail.Headers.UnknownHeaders["iv"]);
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Problems when trying to decrypt the message!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ListBoxData.Add(new EmailEntry { from = mailData.displayName, subject = mailData.subject, messageID = singleEmail.Headers.MessageId.ToString() });
                    });
                    // SQL
                    string myConnString = "Data Source=db.s3db;Version=3;";
                    string mySelectQuery = "SELECT * FROM `emails` WHERE `messageId`='" + singleEmail.Headers.MessageId + "'";
                    SQLiteConnection conn = new SQLiteConnection(myConnString);
                    conn.Open();
                    SQLiteCommand cmdSelect = new SQLiteCommand(mySelectQuery, conn);
                    SQLiteDataReader exists = cmdSelect.ExecuteReader();
                    if (!exists.Read())
                    {
                        string mailDate = singleEmail.Headers.DateSent.Year.ToString("0000") + "-" + singleEmail.Headers.DateSent.Month.ToString("00") + "-" + singleEmail.Headers.DateSent.Day.ToString("00");
                        string mailTime = singleEmail.Headers.DateSent.Hour.ToString("00") + ":" + singleEmail.Headers.DateSent.Minute.ToString("00") + ":" + singleEmail.Headers.DateSent.Second.ToString("00");

                        SQLiteCommand cmdInsert = new SQLiteCommand("INSERT INTO `emails`(`messageId`,`subject`,`body`,`sender`, `read`, `date`, 'time') VALUES ('" + singleEmail.Headers.MessageId + "','" + singleEmail.Headers.Subject + "',@Message,'" + singleEmail.Headers.From.Address + "', '0', '" + mailDate + "', '" + mailTime + "')", conn);
                        cmdInsert.Parameters.Add(new SQLiteParameter("@Message", bodyTxt));
                        cmdInsert.ExecuteNonQuery();
                    }
                    conn.Close();
                }                
            }
            getMailsFromDb();

            // skal fjernes
            //SQLHandler jahallo = new SQLHandler();
            //SQLiteConnection nyconnection = jahallo.connect("db.s3db", 3);

           
        }
    }

    // Classes
    /// <summary>
    /// Gets / Sets data for ListBox
    /// </summary>
    public class ListBoxDataClass
    {
        public Message theMessage { get; set; }
        public int truncate { get; set; }

        public string subject { get; set; }
        public string displayName { get; set; }
    }
    /// <summary>
    /// Gets / Sets email data to ListBox
    /// </summary>
    public class EmailEntry
    {
        public string from { get; set; }
        public string subject { get; set; }
        public string messageID { get; set; }
    }
    /// <summary>
    /// A Class to control SQL Queries and connections. (Under Construction)
    /// </summary>
    public class SQLHandler
    {
        string myConnString;

        public SQLiteConnection connect(string file, int version)
        {
            myConnString = "Data Source=" + file + ";Version=" + version.ToString() + ";";
            SQLiteConnection conn = new SQLiteConnection(myConnString);
            conn.Open();
            return conn;
        }

        public void getAllMails( ObservableCollection<EmailEntry> name, SQLiteConnection conn)
        {
            string mySelectQuery = "SELECT * FROM `emails` ORDER BY `date` DESC, `time` DESC";
            SQLiteConnection sqConnection = new SQLiteConnection(myConnString);
            SQLiteCommand sqCommand = new SQLiteCommand(mySelectQuery, sqConnection);
            sqConnection.Open();
            try
            {
                //ListBoxData = new List<EmailEntry> { };
                SQLiteDataReader sqReader = sqCommand.ExecuteReader();
                while (sqReader.Read())
                {
                    string from = sqReader.GetString(sqReader.GetOrdinal("sender"));
                    string subject = sqReader.GetString(sqReader.GetOrdinal("subject"));
                    string msgid = sqReader.GetString(sqReader.GetOrdinal("messageId"));
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        name.Add(new EmailEntry { from = from, subject = subject, messageID = msgid });
                    });
                }
                sqReader.Close();
            }
            catch
            {
                MessageBox.Show("Problems reading mails from database!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                sqConnection.Close();
            }
        }

        public void insertNewMail(SQLiteConnection conn)
        {

        }
    }
}

