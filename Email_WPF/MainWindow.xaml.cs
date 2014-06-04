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
using System.Security.Cryptography;
using System.Collections.ObjectModel;

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
        public void getMailsFromDb()
        {
            string myConnString = "Data Source=db.s3db;Version=3;";
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
                        ListBoxData.Add(new EmailEntry { from = from, subject = subject, messageID = msgid });
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
        public void readyUpListBoxData(ListBoxDataClass data)
        {
            MessagePart theEmailTxt = data.theMessage.FindFirstPlainTextVersion();
            string noLineBreaks = theEmailTxt.GetBodyAsText().ToString().Replace(System.Environment.NewLine, " ");
            data.partOfBody = noLineBreaks.Length <= data.truncate ? noLineBreaks : noLineBreaks.Substring(0, data.truncate) + " ..";
            data.displayName = data.theMessage.Headers.From.DisplayName.ToString();
            if (data.displayName == "")
            {
                data.displayName = data.theMessage.Headers.From.Address.ToString();
            }
            data.displayName += " <" + data.theMessage.Headers.From.Address.ToString() + ">";
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
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker getNewMail = new BackgroundWorker();
            getNewMail.DoWork += newEmail;
            getNewMail.RunWorkerAsync();
            getNewMail.RunWorkerCompleted += updateList;
        }
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var options = new OptionsScreen();
            options.ShowDialog();
        }

        private void EmailEntry_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                        string myUpdateQuery = "UPDATE `emails` SET `read`='1' WHERE `messageId`='" + selectedId.messageID + "'";                       
                        SQLiteCommand updateFromRead = new SQLiteCommand(myUpdateQuery, sqConnection);
                        updateFromRead.ExecuteNonQuery();

                    }
                }
                sqReader.Close();
            }
            finally
            {
                sqConnection.Close();
            }
        }
    
        // BackgroundWorkers / Threads
        private void newEmail(object sender, DoWorkEventArgs e)
        {
            List<Message> allEmail = FetchAllMessages(hostname, port, useSsl, username, password);
            App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
            {
                ListBoxData.Clear();
            });
            

            //ListBoxData = new List<EmailEntry> { };
            foreach (Message singleEmail in allEmail)
            {
                MessagePart theEmailTxt = singleEmail.FindFirstPlainTextVersion();
                MessagePart theEmailHTML = singleEmail.FindFirstHtmlVersion();

                // For show in listbox
                var mailData = new ListBoxDataClass { theMessage = singleEmail, truncate = 40 };
                readyUpListBoxData(mailData);
                App.Current.Dispatcher.Invoke((Action)delegate // <--- HERE
                {
                    ListBoxData.Add(new EmailEntry { from = mailData.displayName, subject = mailData.partOfBody, messageID = singleEmail.Headers.MessageId.ToString() });
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
                    string bodyTxt = theEmailTxt.GetBodyAsText().ToString();
                    if (theEmailHTML != null)
                    {
                        bodyTxt = theEmailHTML.GetBodyAsText().ToString();
                    }
                    string mailDate = singleEmail.Headers.DateSent.Year.ToString("0000") + "-" + singleEmail.Headers.DateSent.Month.ToString("00") + "-" + singleEmail.Headers.DateSent.Day.ToString("00");
                    string mailTime = singleEmail.Headers.DateSent.Hour.ToString("00") + ":" + singleEmail.Headers.DateSent.Minute.ToString("00") + ":" + singleEmail.Headers.DateSent.Second.ToString("00");
                    //MessageBox.Show(mailDate + "\n" + mailTime);

                    SQLiteCommand cmdInsert = new SQLiteCommand("INSERT INTO `emails`(`messageId`,`subject`,`body`,`sender`, `read`, `date`, 'time') VALUES ('" + singleEmail.Headers.MessageId + "','" + singleEmail.Headers.Subject + "',@Message,'" + singleEmail.Headers.From.Address + "', '0', '" + mailDate + "', '" + mailTime + "')", conn);
                    cmdInsert.Parameters.Add(new SQLiteParameter("@Message", bodyTxt));
                    cmdInsert.ExecuteNonQuery();
                    conn.Close();
                }
                
            }
            getMailsFromDb();
        }
        private void updateList(object sender, RunWorkerCompletedEventArgs e)
        {
            this.DataContext = this;
            EmailList.Items.Refresh();
        }
    }

    // Classes
    public class ListBoxDataClass
    {
        public Message theMessage { get; set; }
        public int truncate { get; set; }

        public string partOfBody { get; set; }
        public string displayName { get; set; }
    }
    public class EmailEntry
    {
        public string from { get; set; }
        public string subject { get; set; }
        public string messageID { get; set; }
    }
}

