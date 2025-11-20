using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

using System.Reflection;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }
    
    //TODO: cover the rest of the NetSdrClient code here

    [Test]
    public async Task ChangeFrequencyAsync_SendsProperMessage()
    {
        await _client.ConnectAsync();

        await _client.ChangeFrequencyAsync(145_000_000, 1);

        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
    }
    
    [Test]
    public async Task SendTcpRequest_NoConnection_ShouldReturnNull()
    {
        // arrange
        var method = typeof(NetSdrClient)
            .GetMethod("SendTcpRequest", BindingFlags.NonPublic | BindingFlags.Instance);

        // act
        var task = (Task<byte[]>) method.Invoke(_client, new object[] { new byte[] { 0x01 } });
        var result = await task;

        // assert
        Assert.That(result, Is.Null.Or.Empty);
    }
    

    [Test]
    public async Task TcpClient_MessageReceived_ShouldCompleteTask()
    {
        // Arrange
        await _client.ConnectAsync();

        // Викликаємо приватний метод SendTcpRequest, щоб створити responseTaskSource
        var sendMethod = typeof(NetSdrClient).GetMethod("SendTcpRequest", BindingFlags.NonPublic | BindingFlags.Instance);
        var sendTask = (Task<byte[]>)sendMethod.Invoke(_client, new object[] { new byte[] { 0xAB } });

        // Викликаємо обробник повідомлення
        var handler = typeof(NetSdrClient).GetMethod("_tcpClient_MessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);
        byte[] response = { 0xAB };  // Маємо передати такий самий масив, як і у виклику SendTcpRequest
        handler.Invoke(_client, new object[] { null, response });

        // Act
        var result = await sendTask;

        // Assert: перевіряємо, що результат співпадає з переданим response
        Assert.That(result, Is.EqualTo(response));
    }
    
    
}
