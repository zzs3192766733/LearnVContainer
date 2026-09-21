using VContainer;
using VContainer.Diagnostics;
using VContainer.Unity;

namespace Tutorial_01
{
    public class TestService
    {
        
    }
    
    public class SayHelloApp : IStartable
    {
        readonly SayHelloService m_SayHelloService;
        readonly SayHelloLifetimeScope m_SayHelloLifetimeScope;
        
        public SayHelloApp(SayHelloService sayHelloService, SayHelloLifetimeScope sayHelloLifetimeScope)
        {
            this.m_SayHelloLifetimeScope = sayHelloLifetimeScope;
            this.m_SayHelloService = sayHelloService;
        }
        
        public void Start()
        {
            m_SayHelloService.SayHello();
        }
    }
}