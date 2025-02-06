using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp2
{
    /// <summary>
    /// Логика взаимодействия для ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window
    {
        private HubConnection _connection;
        private int _clientId; // ID клиента
        private int _selectedMasterId; // ID выбранного мастера
        public ClientWindow(int clientId)
        {
            InitializeComponent();

            _clientId = clientId;

            // Подключение к серверу
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5044/bookingHub")
                .Build();

            LoadMasters();
        }
        private async void LoadMasters()
        {
            try
            {
                await _connection.StartAsync();
                List<MasterDto> masters = await _connection.InvokeAsync<List<MasterDto>>("GetMasters");

                MastersListBox.ItemsSource = masters;
                MastersListBox.DisplayMemberPath = "FIO"; // Отображаем ФИО мастера
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}");
            }
        }

        private void OnMasterSelected(object sender, SelectionChangedEventArgs e)
        {
            if (MastersListBox.SelectedItem is MasterDto selectedMaster)
            {
                _selectedMasterId = selectedMaster.Id;
            }
        }

        private async void OnSubmitBooking(object sender, RoutedEventArgs e)
        {
            if (_selectedMasterId == 0)
            {
                MessageBox.Show("Выберите мастера.");
                return;
            }

            // Проверка на корректный формат времени (HH:mm)
            if (!DateTime.TryParseExact(
                $"{DatePicker.SelectedDate?.ToString("yyyy-MM-dd")} {TimeTextBox.Text}",
                "yyyy-MM-dd HH:mm",
                null,
                System.Globalization.DateTimeStyles.None,
                out DateTime bookingTime))
            {
                MessageBox.Show("Некорректное время. Используйте формат HH:mm.");
                return;
            }

            string service = ServiceTextBox.Text;
            if (string.IsNullOrWhiteSpace(service))
            {
                MessageBox.Show("Введите услугу.");
                return;
            }

            try
            {
                string result = await _connection.InvokeAsync<string>(
                    "CreateSchedule", _clientId, _selectedMasterId, bookingTime, service);

                MessageBox.Show(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заявки: {ex.Message}");
            }
        }

    }

    public class MasterDto
    {
        public int Id { get; set; }
        public string FIO { get; set; }
        public string Description { get; set; }
    }
}
