
namespace server.SERVER_CORE
{
    public class ClientState
    {
        public enum STATE
        {
            READING_PROLOG = 1,
            READING_HEADERS = 2,
            READING_CONTENT = 3,
            WRITING_HEADERS = 4,
            WRITING_CONTENT = 5,
            CLOSED = 6
        };
    }
}
