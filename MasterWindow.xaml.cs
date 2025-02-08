using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace WpfApp2
{
    /// <summary>
    /// Логика взаимодействия для MasterWindow.xaml
    /// </summary>
    public class BookingDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public DateTime BookingTime { get; set; }
        public string Service { get; set; }
        public int Price { get; set; }

        // Переопределяем ToString для удобного отображения в ListBox
        public override string ToString()
        {
            return $"{Service} в {BookingTime.ToShortTimeString()}, Цена: {Price}";
        }
    }


    public partial class MasterWindow : Window
    {
        private readonly HubConnection _connection;
        private int _masterId;
        private string _login;
        private ScheduleRequest _currentRequest;
        public MasterWindow(int userId)
        {
            InitializeComponent();
            _masterId = userId;
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5044/registration")
                .Build();

            // Подписка на событие изменения состояния соединения
            _connection.Closed += async (exception) =>
            {
                MessageBox.Show("Connection closed: " + exception?.Message);
            };

            LoadMasterData();
        }

        private async void LoadMasterData()
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                }

                var masterData = await _connection.InvokeAsync<MasterUpdateRequest>("GetMasterData", _masterId);

                if (masterData != null)
                {
                    _login = masterData.Login;
                    LoginTextBlock.Text = _login; // Показываем логин мастера
                }
                else
                {
                    MessageBox.Show("Данные мастера не найдены.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void EditDataButton_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Visible; // Показываем окно редактирования
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string fio = FIOTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();

            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                }

                var result = await _connection.InvokeAsync<string>("UpdateMasterData", _masterId, fio, description);
                MessageBox.Show(result);

                EditPanel.Visibility = Visibility.Collapsed; // После сохранения скрываем окно редактирования
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}");
            }
        }

        private async void RequestsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                }

                var requests = await _connection.InvokeAsync<List<ScheduleRequest>>("GetMasterSchedules", _masterId);

                if (requests != null && requests.Count > 0)
                {
                    RequestsPanel.Visibility = Visibility.Visible;
                    RequestsListBox.ItemsSource = requests;
                }
                else
                {
                    MessageBox.Show("У вас нет заявок.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private void RequestsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsListBox.SelectedItem != null)
            {
                _currentRequest = (ScheduleRequest)RequestsListBox.SelectedItem;

                ClientNameText.Text = _currentRequest.ClientId.ToString();
                ServiceText.Text = _currentRequest.Service;
                BookingTimeText.Text = _currentRequest.BookingTime.ToString();

                ApprovalPanel.Visibility = Visibility.Visible;
            }
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRequest != null)
            {
                if (int.TryParse(PriceTextBox.Text, out int price))
                {
                    try
                    {
                        // Если SignalR-соединение отключено, запускаем его
                        if (_connection.State == HubConnectionState.Disconnected)
                        {
                            await _connection.StartAsync();
                        }

                        // Передаём _currentRequest, цену и masterId в метод ConfirmBooking
                        // Обратите внимание, что masterId должен быть доступен в данном контексте (например, храниться в переменной или извлекаться из _currentRequest)
                        var result = await _connection.InvokeAsync<string>("ConfirmBooking", _currentRequest, price, _masterId);
                        MessageBox.Show(result);

                        // Скрываем панели после подтверждения
                        ApprovalPanel.Visibility = Visibility.Collapsed;
                        RequestsPanel.Visibility = Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка подтверждения: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Введите корректную цену.");
                }
            }
        }

        private async void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                }

                // Вызываем серверный метод для получения расписания мастера
                var bookings = await _connection.InvokeAsync<List<BookingDto>>("GetMasterBookings", _masterId);

                if (bookings != null && bookings.Count > 0)
                {
                    SchedulePanel.Visibility = Visibility.Visible;
                    ScheduleListBox.ItemsSource = bookings;
                }
                else
                {
                    MessageBox.Show("Ваше расписание пока отсутствует.");
                    SchedulePanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}");
            }
        }

        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRequest != null)
            {
                try
                {
                    if (_connection.State == HubConnectionState.Disconnected)
                    {
                        await _connection.StartAsync();
                    }

                    var result = await _connection.InvokeAsync<string>("RejectSchedule", _currentRequest, _masterId);
                    MessageBox.Show(result);

                    ApprovalPanel.Visibility = Visibility.Collapsed;
                    RequestsPanel.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отклонения: {ex.Message}");
                }
            }
        }
    }

    public class ScheduleRequest
    {
        public int ClientId { get; set; }
        public string Service { get; set; }
        public DateTime BookingTime { get; set; }
    }

    public class MasterUpdateRequest
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string? FIO { get; set; }
        public string? Description { get; set; }
    }
}