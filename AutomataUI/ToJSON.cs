#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Linq;
using VVVV.Nodes;
using Newtonsoft.Json;

using VVVV.Core.Logging;
using System.Collections.Generic;
using Automata.Data;
#endregion usings

namespace VVVV.Nodes
{
    public class AutomataUIObject
    {
        public List<Transition>Transitions
        {
            get;
            set;
        }

        public List<State> States
        {
            get;
            set;
        }
    }



    #region PluginInfo
    [PluginInfo(Name = "ToJSON",
                Category = "AutomataUI Animation",
                Help = "spit out Automata structure as JSON",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class ToJSON : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Output("JSON")]
        public ISpread<string> JSONOut;

        [Import()]
        public ILogger FLogger;
        //[Import()]
        //public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        bool init = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;

        }

        private void Input_Disconnected(object sender, PinConnectionEventArgs args)
        {
            FLogger.Log(LogType.Debug, "DisConnected");
            init = true;
        }

        private void Input_Connected(object sender, PinConnectionEventArgs args)
        {
            FLogger.Log(LogType.Debug, "connected");
        }

        //get connected enum for states
        public void Initialize()
        {
            if (init)
            {             
                init = false;
            }

        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (AutomataUI.IsConnected)
            {
                Initialize();

                //create object for all automata data
                AutomataUIObject automataUIObject = new AutomataUIObject();

                //push data into object
                automataUIObject.Transitions = AutomataUI[0].transitionList;
                automataUIObject.States = AutomataUI[0].stateList;

                JSONOut[0] = JsonConvert.SerializeObject(automataUIObject, Formatting.Indented);
            }
        }
    }
}
