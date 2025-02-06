using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HubConnection _connection;

        public MainWindow()
        {
            InitializeComponent();
            _connection = new HubConnectionBuilder()
                            .WithUrl("http://localhost:5044/registration")
                            .Build();
        }

        private async void OnLoginOrRegisterClick(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;
            string role = (RoleComboBox.SelectedItem as ComboBoxItem).Content.ToString();

            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                    MessageBox.Show("Connected to the server!");
                }

                var result = await _connection.InvokeAsync<Tuple<string, int?, int?>>(
                    "AuthenticateOrRegisterUser", login, password, role);

                string message = result.Item1;
                int? masterId = result.Item2;
                int? clientId = result.Item3;

                ResultText.Text = message;

                if (message.Contains("registered successfully") || message.Contains("Welcome back"))
                {
                    if (role == "Client" && clientId.HasValue)
                    {
                        ClientWindow clientWindow = new ClientWindow(clientId.Value);
                        clientWindow.Show();
                    }
                    else if (role == "Master" && masterId.HasValue)
                    {
                        MasterWindow masterWindow = new MasterWindow(masterId.Value);
                        masterWindow.Show();
                    }

                    this.Close();
                }
                else
                {
                    MessageBox.Show(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}