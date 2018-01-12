#region usings
using System;
using System.ComponentModel.Composition;

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
    [PluginInfo(Name = "GetState",
                Category = "AutomataUI Animation",
                Help = "get a state information from AutomataUI",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class GetState : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        protected IDiffSpread<EnumEntry> EnumState;

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Output("ElapsedStateTime")]
        public ISpread<int> ElapsedStateTime;

        [Output("FadeInOut")]
        public ISpread<double> FadeInOut;

        [Output("FadeDirection")]
        public ISpread<string> FadeDirection;

        [Output("StateActive")]
        public ISpread<bool> StateActive;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        string EnumName;

        private bool invalidate = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;

            FHost.GetNodePath(true, out EnumName); //get unique node path
            EnumName += "AutomataUI"; // add unique name to path
            InputAttribute attr = new InputAttribute("State"); //name of pin
            attr.EnumName = EnumName;
            attr.DefaultEnumEntry = "Init"; //default state
            EnumState = FIOFactory.CreateDiffSpread<EnumEntry>(attr);
        }

        private void Input_Disconnected(object sender, PinConnectionEventArgs args)
        {
            FLogger.Log(LogType.Debug, "DisConnected");
            invalidate = true;
        }

        private void Input_Connected(object sender, PinConnectionEventArgs args)
        {

            invalidate = true;
            FLogger.Log(LogType.Debug, "connected");
        }


        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            //ElapsedStateTime.SliceCount = SpreadMax;

            if (AutomataUI.IsConnected)
            {
                if (invalidate || AutomataUI[0].StatesChanged)
                {
                    EnumManager.UpdateEnum(EnumName, AutomataUI[0].stateList[0].Name, AutomataUI[0].stateList.Select(x => x.Name).ToArray());
                    invalidate = false;
                }

                StateActive.SliceCount = EnumState.SliceCount * AutomataUI[0].ActiveStateIndex.SliceCount; //set Slicecount to amount of incoming Automatas
                FadeInOut.SliceCount = ElapsedStateTime.SliceCount = FadeDirection.SliceCount = StateActive.SliceCount;

                for (int j = 0; j < EnumState.SliceCount; j++)
                {
                    for (int i = 0; i < AutomataUI[0].ActiveStateIndex.SliceCount; i++) // spreaded
                    {
                        int offset = i + (j * AutomataUI[0].ActiveStateIndex.SliceCount);
                        //FLogger.Log(LogType.Debug, Convert.ToString(offset));

                        // find out if selected state is active
                        if (AutomataUI[0].ActiveStateIndex[i] == EnumState[j].Index && // Selected State is Active and Time is running ?
                            AutomataUI[0].ElapsedStateTime[i] > 0)
                        {
                            StateActive[offset] = true;
                            ElapsedStateTime[offset] = AutomataUI[0].ElapsedStateTime[i];
                            //FadeDirection[offset] = "reached";
                        }
                        else
                        {
                            StateActive[offset] = false;
                            ElapsedStateTime[offset] = 0;
                            FadeDirection[offset] = "";
                        }

                        //output in timing
                        if (AutomataUI[0].TransitionFramesOut[i] > 0 &&
                            AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).endState == AutomataUI[0].stateList.ElementAt(EnumState[j].Index)) // is the selected state the target state of the active transition ?
                        {
                            FadeInOut[offset] = 1.0 - ((1.0 / AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).Frames) * AutomataUI[0].TransitionFramesOut[i]);
                            FadeDirection[offset] = "in";
                        }
                        else FadeInOut[offset] = Convert.ToDouble(StateActive[offset]);

                        if (AutomataUI[0].TransitionFramesOut[i] > 0 &&
                            AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).startState == AutomataUI[0].stateList.ElementAt(EnumState[j].Index)) // is the selected state the target state of the active transition ?
                        {
                            FadeInOut[offset] = (1.0 / AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).Frames) * AutomataUI[0].TransitionFramesOut[i];
                            FadeDirection[offset] = "out";
                        }
                    }
                }

            }
        }
    }
}
