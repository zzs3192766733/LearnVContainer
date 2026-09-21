using UnityEngine;

namespace Tutorial_01
{
    public interface ILogger
    {
        void Log(string message);
    }

    public class DebugLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.LogWarning(message);
        }
    }

    public class SayHelloService
    {
        readonly ILogger logger;

        public SayHelloService(ILogger logger)
        {
            this.logger = logger;
        }

        public void SayHello()
        {
            logger.Log("Hello World!");
        }
    }
}