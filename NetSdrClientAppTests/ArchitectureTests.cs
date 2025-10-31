using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void App_Should_Not_Depend_On_EchoServer()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("EchoServer")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True);
        }

        [Test]
        public void Messages_Should_Not_Depend_On_Networking()
        {
            // Arrange
            var result = Types.InAssembly(typeof(NetSdrClientApp.Messages.NetSdrMessageHelper).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True);
        }

        [Test]
        public void Networking_Should_Not_Depend_On_Messages()
        {
            // Arrange
            var result = Types.InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            // Assert
            Assert.That(result.IsSuccessful, Is.True);
        }
        
        
    
        
        // Перевіряє, що мережевий модуль не має залежності від головного класу Program.
        [Test]
        public void Networking_Should_Not_Depend_On_Program()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.Networking.ITcpClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Program")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Networking layer should not depend on the Program class.");
        }
        
        
        // Перевіряє, що класи з суфіксом "Config" знаходяться у просторі імен Configuration.
        [Test]
        public void Config_Classes_Should_Be_In_Configuration()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .HaveNameEndingWith("Config")
                .Should()
                .ResideInNamespace("NetSdrClientApp.Configuration")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True);
        }
        
        
        // Перевіряє, що основний застосунок (App) не має залежності від тестового проєкту.
        [Test]
        public void App_Should_Not_Depend_On_Tests()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientAppTests")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True);
        }
        
        // Перевіряє, що всі класи з суфіксом "Service" розташовані у просторі імен Services.
        [Test]
        public void Service_Classes_Should_Be_In_Services_Namespace()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .HaveNameEndingWith("Service")
                .Should()
                .ResideInNamespace("NetSdrClientApp.Services")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True);
        }
        
        // Перевіряє, що всі класи у просторі імен Services мають суфікс "Service".
        [Test]
        public void Classes_In_Services_Should_Have_Service_Suffix()
        {
            var result = Types.InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Services")
                .Should()
                .HaveNameEndingWith("Service")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True);
        }
    }
}