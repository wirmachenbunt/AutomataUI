
#region usings
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Linq;
using VVVV.Nodes;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Undo",
                Category = "AutomataUI Animation",
                Help = "Undo Statemachine Progress",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class Undo : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Input("Undo", IsBang = true)]
        public IDiffSpread<bool> FTrigger;

        [Input("Enable Output", Visibility = PinVisibility.OnlyInspector, DefaultBoolean = false)]
        public ISpread<bool> FUpdateHistOutput;

        [Input("Reset History", IsBang = true)]
        public IDiffSpread<bool> FReset;

        [Output("State Indices",Visibility = PinVisibility.OnlyInspector)]
        public ISpread<int> FStateIndexHistory;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        int stateindex;
        Stack<int> stateIndexHistory = new Stack<int>();

        private bool init = true;

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

        public void UpdateOutput()
        {
            if(FUpdateHistOutput[0])
            {
                FStateIndexHistory.SliceCount = 0;
                FStateIndexHistory.AddRange(stateIndexHistory.ToList<int>());
            }
        }


        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (AutomataUI[0] == null) return;



            if (AutomataUI.IsConnected)
            {
                //Reset Stack on bang
                if(FReset[0] && FReset.IsChanged)
                {
                    stateIndexHistory.Clear();
                }

                // in case there is no stack
                if (stateIndexHistory.Count == 0)
                {
                    stateIndexHistory.Push(AutomataUI[0].ActiveStateIndex[0]);
                    UpdateOutput();
                }
                
                
                //keep track of statechanges
                if (AutomataUI[0].ActiveStateIndex[0] != stateindex)
                {

                    stateIndexHistory.Push(AutomataUI[0].ActiveStateIndex[0]);
                    UpdateOutput();

                }

                if(FTrigger[0] && FTrigger.IsChanged && stateIndexHistory.Count > 1)
                {
                    stateIndexHistory.Pop();

                    AutomataUI[0].TriggerTransition("Reset To Default State", 0, stateIndexHistory.Peek());

                    UpdateOutput();
                }


                stateindex = AutomataUI[0].ActiveStateIndex[0];


                
            }
        }
    }
}

