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

        public static List<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {

            // The client disconnects from the server when being disposed
            using (OpenPop.Pop3.Pop3Client client = new OpenPop.Pop3.Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);

                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();
                MessageBox.Show(client.GetMessageCount().ToString());

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

        public List<EmailEntry> ListBoxData { get; set; } 
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            List<Message> allEmail = FetchAllMessages(hostname, port, useSsl, username, password);
            /*
            ListBoxData = new List<EmailEntry>
            {
                new EmailEntry{from = "Row 1 - Data 1", bodyPart = "Row 1 - Data 2"},
                new EmailEntry{from = "Row 2 - Data 1", bodyPart = "Row 2 - Data 2"},
                new EmailEntry{from = "Row 3 - Data 1", bodyPart = "Row 3 - Data 2"},
                new EmailEntry{from = "Row 4 - Data 1", bodyPart = "Row 4 - Data 2"},
                new EmailEntry{from = "Row 5 - Data 1", bodyPart = "Row 5 - Data 2"},
                new EmailEntry{from = "Row 6 - Data 1", bodyPart = "Row 6 - Data 2"},
                new EmailEntry{from = "Row 7 - Data 1", bodyPart = "Row 7 - Data 2"},
                new EmailEntry{from = "Row 8 - Data 1", bodyPart = "Row 8 - Data 2"},
                new EmailEntry{from = "Row 9 - Data 1", bodyPart = "Row 9 - Data 2"}
            };
            */

            ListBoxData = new List<EmailEntry> { };
            foreach(Message singleEmail in allEmail) 
            {
                MessagePart theEmail = singleEmail.FindFirstPlainTextVersion();
                //MessageBox.Show(theEmail.GetBodyAsText());
                ListBoxData.Add(new EmailEntry { from = singleEmail.Headers.From.DisplayName, bodyPart = "test" });
            }
            


            MessageBox.Show(ListBoxData.Count.ToString());
            foreach (EmailEntry one in ListBoxData)
            {
                MessageBox.Show(one.ToString());
            }



        }

        //Buttons
        private void NewEmailButton_Click(object sender, RoutedEventArgs e)
        {
               
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }       

    public class EmailEntry
    {
        public string from { get; set; }
        public string bodyPart { get; set; }
    }

    public class SingleEmail
    {

    }


}
