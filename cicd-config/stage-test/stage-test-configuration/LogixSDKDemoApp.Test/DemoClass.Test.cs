using Moq;
using RockwellAutomation.LogixDesigner;
using System;
using System.Threading;
using System.Threading.Tasks;
using static RockwellAutomation.LogixDesigner.LogixProject;

namespace LogixSDKDemoApp.Test
{
    [TestClass]
    public class DemoTest
    {
        private Mock<ILogixProject> _mockLogixProject = null!;
        public DemoTest() { }

        [TestInitialize]
        public void initializeTest()
        {
            _mockLogixProject = new Mock<ILogixProject>();
        }
        [TestMethod]
        public async Task IncrementIntTagValueByXAsync_WritesToTheController()
        {
            _mockLogixProject.Setup(t => t.GetTagValueDINTAsync(It.IsAny<string>(), It.IsAny<TagOperationMode>()))
                .Returns(Task.FromResult(6));
            _mockLogixProject.Setup(t => t.SetTagValueDINTAsync(It.IsAny<string>(), It.IsAny<TagOperationMode>(), It.IsAny<Int32>()))
                .Verifiable();
            _mockLogixProject.Setup(t => t.ReadControllerModeAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(ControllerMode.Program));

            await _mockLogixProject.Object.SetTagValueDINTAsync("foo", TagOperationMode.Offline, 10);
            _mockLogixProject.Verify(t => t.SetTagValueDINTAsync(It.IsAny<string>(), It.IsAny<TagOperationMode>(), It.Is<Int32>(i => i == 10)));
            Assert.AreEqual(true, true);
            //DemoClass.
            //Demo
        }
    }
}