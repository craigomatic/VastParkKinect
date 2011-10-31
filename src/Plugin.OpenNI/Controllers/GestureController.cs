using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.PluginFramework.Controllers;
using OpenNILib = OpenNI;
using OpenNI;
using VastPark.FrameworkBase;
using VastPark.Common;
using VastPark.Data;
using Plugin.OpenNI.Model;

namespace Plugin.OpenNI.Controllers
{
    public class GestureController : IController
    {
        public ILogProvider LogProvider { get; private set; }

        public IParkEngine ParkEngine { get; private set; }

        private GestureGenerator _GestureGenerator;

        private Dictionary<string, List<string>> _EventHandlers;
        private object _ListenerLock;
 
        public GestureController(IParkEngine parkEngine, ILogProvider logProvider, Context context)
        {
            this.ParkEngine = parkEngine;
            this.LogProvider = logProvider;

            _EventHandlers = new Dictionary<string, List<string>>();
            _ListenerLock = new object();

            _GestureGenerator = new GestureGenerator(context);
            _GestureGenerator.GestureRecognized += new EventHandler<GestureRecognizedEventArgs>(_GestureGenerator_GestureRecognized);
            _GestureGenerator.GestureProgress += new EventHandler<GestureProgressEventArgs>(_GestureGenerator_GestureProgress);
            _GestureGenerator.GestureChanged += new EventHandler(_GestureGenerator_GestureChanged);
            _GestureGenerator.StartGenerating();
        }

        void _GestureGenerator_GestureChanged(object sender, EventArgs e)
        {
            
        }

        void _GestureGenerator_GestureProgress(object sender, GestureProgressEventArgs e)
        {
            if (!this.Enabled)
            {
                return;
            }

            lock (_ListenerLock)
            {
                if (_EventHandlers.ContainsKey(e.Gesture))
                {
                    _EventHandlers[e.Gesture].ForEach(l =>
                    {
                        if (this.ParkEngine.ContainsName(l))
                        {
                            var handler = this.ParkEngine.GetElementByName(l) as ITimelineExecutable;

                            if (handler != null)
                            {
                                var position = e.Position.ToVector3();
                                handler.Execute(this, new KinectGesture(e.Gesture, 100f, position, position));
                            }
                        }
                    });
                }
            }

            this.LogProvider.Write(string.Format("Gesture '{0}' progress {1}", e.Gesture, e.Progress), LogLevel.Notice);
        }

        void _GestureGenerator_GestureRecognized(object sender, GestureRecognizedEventArgs e)
        {
            if (!this.Enabled)
            {
                return;
            }

            lock (_ListenerLock)
            {
                if (_EventHandlers.ContainsKey(e.Gesture))
                {
                    _EventHandlers[e.Gesture].ForEach(l =>
                    {
                        if (this.ParkEngine.ContainsName(l))
                        {
                            var handler = this.ParkEngine.GetElementByName(l) as ITimelineExecutable;

                            if (handler != null)
                            {
                                handler.Execute(this, new KinectGesture(e.Gesture, 100f, e.IdentifiedPosition.ToVector3(), e.EndPosition.ToVector3()));
                            }
                        }
                    });
                }
            }

            this.LogProvider.Write(string.Format("Gesture '{0}' recognised", e.Gesture), LogLevel.Notice);
        }

        public void AddListener(string gesture, string handler)
        {
            lock (_ListenerLock)
            {
                if (!_EventHandlers.ContainsKey(gesture))
                {
                    //test if this gesture is valid
                    if (!_GestureGenerator.IsGestureAvailable(gesture))
                    {
                        throw new ArgumentException(string.Format("The specified gesture {0} is not valid", gesture), "gesture");
                    }

                    _EventHandlers.Add(gesture, new List<string>());
                    _GestureGenerator.AddGesture(gesture);
                }

                if (_EventHandlers[gesture].Contains(handler))
                {
                    throw new Exception(string.Format("Handler '{0}' already registered for the gesture '{1}'", handler, gesture));
                }

                _EventHandlers[gesture].Add(handler);
            }
        }

        public void RemoveListener(string gesture, string handler)
        {
            lock (_ListenerLock)
            {
                if (!_EventHandlers.ContainsKey(gesture))
                {
                    return;
                }

                _EventHandlers[gesture].Remove(handler);
                _GestureGenerator.RemoveGesture(gesture);
            }
        }


        public bool Enabled { get; set; }

        public void Update()
        {
            
        }

        public void Dispose()
        {
            _GestureGenerator.StopGenerating();
            _GestureGenerator.Dispose();
        }
    }
}
