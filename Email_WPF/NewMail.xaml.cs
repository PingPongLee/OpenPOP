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
using System.Windows.Shapes;
using System.Net.Mail;
using System.Net;
using System.ComponentModel;

namespace Email_WPF
{
    /// <summary>
    /// Interaction logic for NewMail.xaml
    /// </summary>
    public partial class NewMail : Window
    {
        string subjectTxt;
        string toRecipent;
        bool success = false;

        public NewMail()
        {
            InitializeComponent();
        }

        public void sendEmail(object sender, DoWorkEventArgs e)
        {                        
            SmtpClient smtp = new SmtpClient();
            MailMessage theNewMail = new MailMessage();

            smtp.Credentials = new NetworkCredential(Properties.Settings.Default.username, Properties.Settings.Default.password);
            smtp.Host = Properties.Settings.Default.smtp_host;
            smtp.Port = Properties.Settings.Default.smtp_port;
            smtp.EnableSsl = Properties.Settings.Default.smtp_usessl;

            string body = txtFromRtb(bodyTxt);

            theNewMail.To.Add(toRecipent);
            theNewMail.Subject = subjectTxt;
            theNewMail.From = new MailAddress(Properties.Settings.Default.username);
            theNewMail.Body = body;

            smtp.Send(theNewMail);
            success = true;
        }

        string txtFromRtb(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(
                rtb.Document.ContentStart,
                rtb.Document.ContentEnd
            );
            return textRange.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                subjectTxt = subject.Text;
                toRecipent = to.Text;
                MainWindow main = (MainWindow)this.DataContext;
                main.statusBarTxt.Content = "Sending e-mail...";
                BackgroundWorker sendNewEmail = new BackgroundWorker();
                sendNewEmail.DoWork += sendEmail;
                sendNewEmail.RunWorkerAsync();
                if (success == true)
                {
                    sendNewEmail.RunWorkerCompleted += successTasks;
                }
                this.Close();
            }
            catch
            {
                MessageBox.Show("There was an error when trying to send an email. Perhaps your SMTP settings are wrong?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void successTasks(object sender, RunWorkerCompletedEventArgs e)
        {
            MainWindow main = (MainWindow)this.DataContext;
            main.statusBarTxt.Content = "E-mail was sent successfully";
        }
    }
}
