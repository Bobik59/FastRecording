using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServerC.Model;
using System.Linq;
using System.Threading.Tasks;

namespace ServerC.Hubs
{

    public class BookingDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public DateTime BookingTime { get; set; }
        public string Service { get; set; }
        public int Price { get; set; }
    }

    public class MasterUpdateRequest
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string? FIO { get; set; }
        public string? Description { get; set; }
    }

    public class ScheduleRequest
    {
        public int ClientId { get; set; }
        public string Service { get; set; }
        public DateTime BookingTime { get; set; }
    }


    public class WorkHub : Hub
    {
        private readonly BookingContext _context;

        public WorkHub(BookingContext context)
        {
            _context = context;
        }

        public async Task<string> UpdateMasterData(int masterId, string fio, string description)
        {
            try
            {
                Console.WriteLine($"Updating master data for MasterId: {masterId}");

                var master = await _context.Masters
                                           .FirstOrDefaultAsync(m => m.Id == masterId);

                if (master == null)
                {
                    return "Master not found.";
                }

                master.FIO = string.IsNullOrWhiteSpace(fio) ? null : fio;
                master.Description = string.IsNullOrWhiteSpace(description) ? null : description;

                await _context.SaveChangesAsync();
                return "Master data updated successfully.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating master data: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
        public async Task<List<ScheduleRequest>> GetMasterSchedules(int masterId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.Master.Id == masterId)
                .Select(s => new ScheduleRequest
                {
                    ClientId = s.Client.Id,
                    Service = s.Service,
                    BookingTime = s.BookingTime
                })
                .ToListAsync();

            return schedules;
        }

        public async Task<MasterUpdateRequest> GetMasterData(int masterId)
        {
            var master = await _context.Masters
                .Where(m => m.Id == masterId)
                .Select(m => new MasterUpdateRequest
                {
                    UserId = m.UserId,
                    Login = m.User.Login,
                    FIO = m.FIO,
                    Description = m.Description
                })
                .FirstOrDefaultAsync();

            return master;
        }

        public async Task<string> ConfirmBooking(ScheduleRequest request, int price, int masterId)
        {
            // Получаем клиента по идентификатору, указанному в запросе
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == request.ClientId);

            if (client == null)
            {
                return "Клиент не найден.";
            }

            // Ищем соответствующую заявку в таблице Schedules с учетом переданного masterId
            var schedule = await _context.Schedules
                .Include(s => s.Client)
                .Include(s => s.Master)
                .FirstOrDefaultAsync(s => s.Master.Id == masterId &&
                                          s.Client.Id == client.Id &&
                                          s.Service == request.Service &&
                                          s.BookingTime == request.BookingTime);

            if (schedule == null)
            {
                return "Заявка не найдена.";
            }

            // Создаем новое бронирование на основе данных из найденной заявки
            var newBooking = new Bookings
            {
                Client = schedule.Client,
                Master = schedule.Master,
                BookingTime = schedule.BookingTime,
                Service = schedule.Service,
                Price = price
            };

            // Добавляем запись бронирования в таблицу Bookings
            _context.Bookings.Add(newBooking);
            // Удаляем заявку из таблицы Schedules
            _context.Schedules.Remove(schedule);

            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();



            return "Заявка подтверждена и добавлена в систему.";
        }

        public async Task<string> RejectSchedule(ScheduleRequest request, int masterId)
        {
            try
            {
                // Логируем информацию о заявке, которую собираемся удалить
                Console.WriteLine($"Удаление заявки: ClientId={request.ClientId}, MasterId={masterId}, Service={request.Service}, BookingTime={request.BookingTime}");

                // Ищем запись, удовлетворяющую заданным критериям
                var schedule = await _context.Schedules
                    .Where(s => s.Client.Id == request.ClientId &&
                                s.Master.Id == masterId &&
                                !s.IsAccepted &&
                                s.Service == request.Service &&
                                s.BookingTime == request.BookingTime)
                    .FirstOrDefaultAsync();

                // Если запись не найдена, сообщаем об этом
                if (schedule == null)
                {
                    Console.WriteLine("Заявка не найдена.");
                    return "Заявка не найдена.";
                }

                // Удаляем найденную запись
                _context.Schedules.Remove(schedule);

                // Сохраняем изменения в базе данных
                await _context.SaveChangesAsync();

                Console.WriteLine("Заявка успешно удалена.");
                return "Заявка отклонена.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отклонении заявки: {ex.Message}\n{ex.StackTrace}");
                return $"Ошибка на сервере: {ex.Message}";
            }
        }

        public async Task<List<BookingDto>> GetMasterBookings(int masterId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Master)
                .Where(b => b.Master.Id == masterId)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    ClientId = b.Client.Id,
                    BookingTime = b.BookingTime,
                    Service = b.Service,
                    Price = b.Price
                })
                .ToListAsync();

            return bookings;
        }


        public async Task<Tuple<string, int?, int?>> AuthenticateOrRegisterUser(string login, string password, string role)
        {
            var user = _context.Users.FirstOrDefault(u => u.Login == login);

            if (user == null)
            {
                string encryptedPassword = AESCryptography.Encrypt(password, "MySecretKey");

                var newUser = new User { Login = login, PasswordHash = encryptedPassword };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                if (role == "Master")
                {
                    var newMaster = new Master { UserId = newUser.Id };
                    _context.Masters.Add(newMaster);
                    await _context.SaveChangesAsync();
                    return Tuple.Create("Master registered successfully", (int?)newMaster.Id, (int?)null);
                }
                else
                {
                    var newClient = new Client { UserId = newUser.Id };
                    _context.Clients.Add(newClient);
                    await _context.SaveChangesAsync();
                    return Tuple.Create("Client registered successfully", (int?)null, (int?)newClient.Id);
                }
            }
            else
            {
                string decryptedPassword = AESCryptography.Decrypt(user.PasswordHash, "MySecretKey");

                if (decryptedPassword != password)
                {
                    return Tuple.Create("Incorrect password", (int?)null, (int?)null);
                }

                var master = _context.Masters.FirstOrDefault(m => m.UserId == user.Id);
                var client = _context.Clients.FirstOrDefault(c => c.UserId == user.Id);

                return Tuple.Create(
                    "Welcome back, " + role + "!",
                    master?.Id,
                    client?.Id
                );
            }
        }
    }
}