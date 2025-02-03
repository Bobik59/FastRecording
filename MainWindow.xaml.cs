using System.Net.Mail;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.Json;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string verificationCode;
        private static readonly HttpClient client = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();
            RoleComboBox.ItemsSource = Enum.GetValues(typeof(UserRole));
        }
        private void SendVerificationCode_Click(object sender, RoutedEventArgs e)
        {
            verificationCode = new Random().Next(1000, 9999).ToString();
            string email = EmailTextBox.Text;

            SendEmail(email, "Verification Code", $"Your verification code is: {verificationCode}");
        }

        private async void VerifyCode_Click(object sender, RoutedEventArgs e)
        {
            if (VerificationTextBox.Text == verificationCode)
            {
                MessageBox.Show("Email verified!");
                await CreateUser();
            }
            else
            {
                MessageBox.Show("Incorrect code!");
            }
        }

        private static void SendEmail(string to, string subject, string body)
        {
            string from = "loshininc@gmail.com";
            string password = "kbcx rrvo ceif ignb";

            MailMessage message = new MailMessage(from, to, subject, body);
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(from, password),
                EnableSsl = true
            };

            try
            {
                smtp.Send(message);
                MessageBox.Show("Verification code sent!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending email: " + ex.Message);
            }
        }

        private async Task CreateUser()
        {
            User newUser = new User
            {
                Id = new Random().Next(1, 10000),
                Email = EmailTextBox.Text,
                PasswordHash = PasswordTextBox.Password, // В реальном приложении нужно хешировать пароль
                IsVerified = true,
                Role = (UserRole)RoleComboBox.SelectedItem
            };

            string json = JsonSerializer.Serialize(newUser);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("https://yourserver.com/api/users", content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("User registered successfully!");
                }
                else
                {
                    MessageBox.Show("Error registering user: " + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending user data: " + ex.Message);
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsVerified { get; set; }
        public UserRole Role { get; set; }
    }

    public enum UserRole
    {
        Client,
        Master
    }
}
