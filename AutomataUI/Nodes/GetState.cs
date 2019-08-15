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
        //protected IDiffSpread<EnumEntry> EnumState;
        IIOContainer<IDiffSpread<EnumEntry>> StatesEnum;

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Output("ElapsedStateTime")]
        public ISpread<int> ElapsedStateTime;

        [Output("FadeInOut")]
        public ISpread<double> FadeInOut;

        [Output("In")]
        public ISpread<bool> FIn;

        [Output("Out")]
        public ISpread<bool> FOut;

        [Output("Lock Time")]
        public ISpread<int> FLockTime;

        [Output("StateActive")]
        public ISpread<bool> StateActive;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        bool init = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;

            //new way of enums
            InputAttribute attr = new InputAttribute("State");
            StatesEnum = FIOFactory.CreateIOContainer<IDiffSpread<EnumEntry>>(attr, true);          
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
            if(init)
            {
                var pin = StatesEnum.GetPluginIO() as IPin;
                (pin as IEnumIn).SetSubType(AutomataUI[0].myGUID + "_States");
                init = false;
            }
            
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (AutomataUI.IsConnected)
            {
                Initialize();

                StateActive.SliceCount = StatesEnum.IOObject.SliceCount * AutomataUI[0].ActiveStateIndex.SliceCount; //set Slicecount to amount of incoming Automatas
                FadeInOut.SliceCount = ElapsedStateTime.SliceCount = FIn.SliceCount = FOut.SliceCount = FLockTime.SliceCount = StateActive.SliceCount;

                for (int j = 0; j < StatesEnum.IOObject.SliceCount; j++)
                {
                    for (int i = 0; i < AutomataUI[0].ActiveStateIndex.SliceCount; i++) // spreaded
                    {
                        int offset = i + (j * AutomataUI[0].ActiveStateIndex.SliceCount);
                        //FLogger.Log(LogType.Debug, Convert.ToString(offset));

                        // find out if selected state is active
                        if (AutomataUI[0].ActiveStateIndex[i] == StatesEnum.IOObject[j].Index && // Selected State is Active and Time is running ?
                            AutomataUI[0].ElapsedStateTime[i] > 0)
                        {
                            StateActive[offset] = true;
                            FIn[offset] = false;
                            ElapsedStateTime[offset] = AutomataUI[0].ElapsedStateTime[i];                
                        }
                        else
                        {
                            StateActive[offset] = false;
                            ElapsedStateTime[offset] = 0;
                            FIn[offset] = false;
                            FOut[offset] = false;
                        }

                        try
                        {
                            FLockTime[offset] = AutomataUI[0].stateList.ElementAt(StatesEnum.IOObject[j].Index).Frames;
                        }
                        catch (Exception)
                        {

                           //do nothjing
                        }

                           
                        
                        

                        //output in timing
                        if (AutomataUI[0].TransitionFramesOut[i] > 0 &&
                            AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).endState == AutomataUI[0].stateList.ElementAt(StatesEnum.IOObject[j].Index)) // is the selected state the target state of the active transition ?
                        {
                            FadeInOut[offset] = 1.0 - ((1.0 / AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).Frames) * AutomataUI[0].TransitionFramesOut[i]);
                            FIn[offset] = true;
                        }
                        else FadeInOut[offset] = Convert.ToDouble(StateActive[offset]);

                        if (AutomataUI[0].TransitionFramesOut[i] > 0 &&
                            AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).startState == AutomataUI[0].stateList.ElementAt(StatesEnum.IOObject[j].Index)) // is the selected state the target state of the active transition ?
                        {
                            FadeInOut[offset] = (1.0 / AutomataUI[0].transitionList.ElementAt(AutomataUI[0].TransitionIndex[i]).Frames) * AutomataUI[0].TransitionFramesOut[i];
                            FOut[offset] = true;
                        }
                    }
                }

            }
        }
    }
}
