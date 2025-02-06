using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServerC.Model;
namespace ServerC.Hubs
{
    public class BookingHub : Hub
    {
        private readonly BookingContext _context;

        public BookingHub(BookingContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает список всех мастеров
        /// </summary>
        public async Task<List<MasterDto>> GetMasters()
        {
            return await _context.Masters
                .Select(m => new MasterDto { Id = m.Id, FIO = m.FIO, Description = m.Description })
                .ToListAsync();
        }

        /// <summary>
        /// Создает заявку (Schedule) для клиента
        /// </summary>
        public async Task<string> CreateSchedule(int clientId, int masterId, DateTime bookingTime, string service)
        {
            var client = await _context.Clients.FindAsync(clientId);
            var master = await _context.Masters.FindAsync(masterId);

            if (client == null || master == null)
            {
                return "Ошибка: клиент или мастер не найдены.";
            }

            var schedule = new Schedule
            {
                Client = client,
                Master = master,
                BookingTime = bookingTime,
                Service = service,
                IsAccepted = false // Изначально заявка не подтверждена
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return "Заявка успешно создана!";
        }
    }

    /// <summary>
    /// DTO для мастера
    /// </summary>
    public class MasterDto
    {
        public int Id { get; set; }
        public string FIO { get; set; }
        public string Description { get; set; }
    }
}
    
