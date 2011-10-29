using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.FrameworkBase;
using VastPark.PluginFramework;
using VastPark.Imml.Scene.Controls;

namespace Plugin.OpenNI
{
    public class StatusTextLog : ILogProvider
    {
        public Text TextElement { get; private set; }

        public LogLevel MinimumLogLevel { get; private set; }

        private FileOutputLogger _FileOutputLogger;

        public StatusTextLog(Text textElement, string outputFile, LogLevel minimumLogLevel)            
        {
            this.TextElement = textElement;
            this.MinimumLogLevel = minimumLogLevel;

            _FileOutputLogger = new FileOutputLogger(outputFile, minimumLogLevel);
        }

        public void Write(string message, LogLevel logLevel)
        {
            if (logLevel >= this.MinimumLogLevel)
            {
                var output = string.Format("[{0}] {1}", logLevel.ToString().ToUpper(), message);
                this.TextElement.Value = output;
            }

            _FileOutputLogger.Write(message, logLevel);
        }

        public void Write(Exception e, LogLevel logLevel)
        {
            this.Write(e.Message, logLevel);
        }
    }
}
