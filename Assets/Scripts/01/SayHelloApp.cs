using VContainer;
using VContainer.Unity;

namespace Tutorial_01
{
    public class TestService
    {
        
    }
    
    public class SayHelloApp : IStartable
    {
        //readonly SayHelloService m_SayHelloService;
        readonly IObjectResolver m_Container;
        
        public SayHelloApp(/*SayHelloService sayHelloService, */IObjectResolver resolver)
        {
            //this.m_SayHelloService = sayHelloService;
            this.m_Container = resolver;
        }
        
        public void Start()
        {
            //m_SayHelloService.SayHello();

            this.m_Container.CreateScope(containerBuilder =>
            {
                containerBuilder.Register<TestService>(Lifetime.Scoped);
            });
        }
    }
}