using NodaTime;

namespace NotesApi.Services
{
    public interface IDateTimeService
    {
        DateTime Now { get; }

        Instant InstanceNow { get; }

        Instant InstanceDate { get; }

        Instant ToUtcInstance(DateTime time);
    }


    public class DateTimeService : IDateTimeService
    {
        public DateTime Now => DateTime.UtcNow;

        public Instant InstanceNow => Instant.FromDateTimeUtc(Now);

        public Instant InstanceDate => Instant.FromDateTimeUtc(Now.Date);

        public Instant ToUtcInstance(DateTime time) => Instant.FromDateTimeUtc(DateTime.SpecifyKind(time, DateTimeKind.Utc));
    }
}
