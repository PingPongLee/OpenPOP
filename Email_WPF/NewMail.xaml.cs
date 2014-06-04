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
using System.Security.Cryptography;
using System.IO;

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
            if(Properties.Settings.Default.encrypt == true)
            {
                isEncryptedTxt.Content = "This mail will be sent as Encrypted!";
            }
        }

        /// <summary>
        /// Encrypytion function, that encrypts the string given, based on AES encryption. Key and Initialization Vector is needed
        /// </summary>
        /// <param name="plainText">The message to encrypt</param>
        /// <param name="Key">AES Key</param>
        /// <param name="IV">AES Initialization vector</param>
        /// <returns>Encrypted AES message</returns>
        static byte[] EncryptMail(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }
       
        /// <summary>
        /// Function that sends the e-mail message based on SMTP settings given in Application settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void sendEmail(object sender, DoWorkEventArgs e)
        {                        
            SmtpClient smtp = new SmtpClient();
            MailMessage theNewMail = new MailMessage();

            smtp.Credentials = new NetworkCredential(Properties.Settings.Default.username, Properties.Settings.Default.password);
            smtp.Host = Properties.Settings.Default.smtp_host;
            smtp.Port = Properties.Settings.Default.smtp_port;
            smtp.EnableSsl = Properties.Settings.Default.smtp_usessl;

            string body = "";

            if(Properties.Settings.Default.encrypt == true)
            {
                try
                {
                    string original = txtFromRtb(bodyTxt);
                    using (RijndaelManaged myRijndael = new RijndaelManaged())
                    {

                        myRijndael.GenerateKey();
                        myRijndael.GenerateIV();
                        byte[] encrypted = EncryptMail(original, myRijndael.Key, myRijndael.IV);
                        body = Convert.ToBase64String(encrypted);

                        string key = Convert.ToBase64String(myRijndael.Key);
                        string IV = Convert.ToBase64String(myRijndael.IV);

                        theNewMail.Headers.Add("isEncrypted", "true");
                        theNewMail.Headers.Add("key", key);
                        theNewMail.Headers.Add("iv", IV);
                    }

                }
                catch (Exception r)
                {
                    MessageBox.Show("Problems when trying to encrypt your message. The error was:\n" + r.Message);
                }
            } 
            else
            {
               body = txtFromRtb(bodyTxt);
            }


            MessageBox.Show(body);

            theNewMail.To.Add(toRecipent);
            theNewMail.Subject = subjectTxt;
            theNewMail.From = new MailAddress(Properties.Settings.Default.username);
            theNewMail.Body = body;

            smtp.Send(theNewMail);
            success = true;
        }

        /// <summary>
        /// Reads content from a RichTextBox and returns it as a string
        /// </summary>
        /// <param name="rtb">The RichTextBox to return text from</param>
        /// <returns>String with RichTextBox content</returns>
        string txtFromRtb(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(
                rtb.Document.ContentStart,
                rtb.Document.ContentEnd
            );
            return textRange.Text;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Function that runs when a message is successfully sent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void successTasks(object sender, RunWorkerCompletedEventArgs e)
        {
            MainWindow main = (MainWindow)this.DataContext;
            main.statusBarTxt.Content = "E-mail was sent successfully";
        }
    }
}
