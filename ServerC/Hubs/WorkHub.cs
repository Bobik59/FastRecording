using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServerC.Model;
using System.Linq;
using System.Threading.Tasks;

namespace ServerC.Hubs
{
    public class MasterUpdateRequest
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string? FIO { get; set; }
        public string? Description { get; set; }
    }

    public class ScheduleRequest
    {
        public string ClientFIO { get; set; }
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
                    ClientFIO = s.Client.FIO ?? "Не указано",
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

        public async Task<string> ConfirmBooking(ScheduleRequest request, int price)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Client)
                .Include(s => s.Master)
                .FirstOrDefaultAsync(s => s.Client.FIO == request.ClientFIO &&
                                          s.Service == request.Service &&
                                          s.BookingTime == request.BookingTime);

            if (schedule != null)
            {
                var newBooking = new Bookings
                {
                    Client = schedule.Client,
                    Master = schedule.Master,
                    BookingTime = schedule.BookingTime,
                    Service = schedule.Service,
                    Price = price
                };

                _context.Bookings.Add(newBooking);
                _context.Schedules.Remove(schedule);

                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("BookingConfirmed", newBooking);

                return "Заявка подтверждена и добавлена в систему.";
            }

            return "Заявка не найдена.";
        }

        public async Task<string> RejectSchedule(ScheduleRequest request)
        {
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.Client.FIO == request.ClientFIO &&
                                          s.Service == request.Service &&
                                          s.BookingTime == request.BookingTime);

            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
                return "Заявка отклонена.";
            }

            return "Заявка не найдена.";
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