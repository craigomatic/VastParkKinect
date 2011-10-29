using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using VastPark.FrameworkBase.Threading;
using System.Threading;
using VastPark.FrameworkBase;
using VastPark.Imml.Scene.Controls;
using VastPark.Imml;
using Moq;
using VastPark.Common;
using VastPark.Imml.Container;

namespace Plugin.OpenNI.Test
{
    public class KinectTests
    {
        [Fact]
        public void When_The_Kinect_Is_Connected_The_Plugin_Successfully_Loads()
        {
            VastPark.Legacy.Environment.Initialise(System.IO.Path.GetTempPath());

            var timePeriod = TimeSpan.FromSeconds(30); //allow up to 30 seconds for the context to become ready
            var slimResetEvent = new SlimResetEvent(100);
            var start = DateTime.Now;
            var contextReady = false;

            ThreadPool.QueueUserWorkItem(w =>
            {
                using(var plugin = new OpenNiPlugin())
                {
                    plugin.Load();
                    plugin.Enabled = true;

                    while (DateTime.Now.Subtract(start) < timePeriod)
                    {
                        plugin.Update();

                        if (plugin.IsContextReady)
                        {
                            break;
                        }

                        Thread.Sleep(16);
                    }

                    contextReady = plugin.IsContextReady;
                    slimResetEvent.Set();
                }
            });

            slimResetEvent.Wait();

            Assert.True(contextReady);
        }

        [Fact]
        public void DepthVideo_Is_Drawn_When_RenderDepth_Is_True_And_The_DepthVideoOutputTarget_Is_Defined_And_Valid()
        {
            VastPark.Legacy.Environment.Initialise(System.IO.Path.GetTempPath());
            
            var context = new ImmlDocument();
            var parkEngine = new Mock<IParkEngine>();
            parkEngine.Setup(c => c.Context).Returns(context);

            var renderEngine = new Mock<IRenderEngine>();
            parkEngine.Setup(r => r.RenderEngine).Returns(renderEngine.Object);

            var scriptEngine = new Mock<IScriptEngine>();
            scriptEngine.Setup(s => s.WriteLine(It.IsAny<string>())).Callback<string>(a => System.Diagnostics.Debug.WriteLine(a));
            parkEngine.Setup(s => s.ScriptEngine).Returns(scriptEngine.Object);
            
            var primitive = ImmlElementFactory.CreatePrimitive();
            primitive.Name = "DepthTarget";

            context.Add(primitive);

            var timePeriod = TimeSpan.FromSeconds(30); //allow up to 30 seconds for the context to become ready
            var slimResetEvent = new SlimResetEvent(100);
            var start = DateTime.Now;
            var textureWritten = false;

            Texture.TextureBytesLoaded += delegate
            {
                textureWritten = true;
            };

            ThreadPool.QueueUserWorkItem(w =>
            {
                using (var plugin = new OpenNiPlugin())
                {
                    plugin.SetParkEngine(parkEngine.Object);
                    plugin.RenderDepth = true;
                    plugin.DepthVideoOutputTarget = primitive.Name;
                    plugin.Load();
                    plugin.Enabled = true;

                    while (DateTime.Now.Subtract(start) < timePeriod)
                    {
                        plugin.Update();

                        if (textureWritten)
                        {
                            break;
                        }

                        Thread.Sleep(16);
                    }

                    slimResetEvent.Set();
                }
            });

            slimResetEvent.Wait();

            Assert.True(textureWritten);
        }

        [Fact]
        public void StatusText_Logger_Writes_To_The_Backing_Text_Elements_Value_Property()
        {
            var text = new Text();
            var expectedValue = Guid.NewGuid().ToString();
            var tmpFile = System.IO.Path.GetTempFileName();

            ILogProvider logProvider = new StatusTextLog(text, tmpFile, LogLevel.Notice);

            logProvider.Write(expectedValue, LogLevel.Notice);

            Assert.Contains(expectedValue, text.Value);
        }
    }
}
