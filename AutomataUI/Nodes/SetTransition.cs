using System;
using System.Collections.Generic;
#region usings
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Linq;
using VVVV.Nodes;

using VVVV.Core.Logging;
using Automata.Data;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "SetTransitionTime",
                Category = "AutomataUI Animation",
                Help = "setup transitions programatically ",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class SetTranstion : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        IIOContainer<IDiffSpread<EnumEntry>> TransitionEnum;

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Input("Time")]
        public ISpread<int> TransitionTime;

        [Input("Set", IsBang = true)]
        public ISpread<bool> SetTime;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        int stateindex;
        private bool init = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;

            //new way of enums
            InputAttribute attr = new InputAttribute("Transition");
            TransitionEnum = FIOFactory.CreateIOContainer<IDiffSpread<EnumEntry>>(attr, true);
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
                var pin = TransitionEnum.GetPluginIO() as IPin;
                (pin as IEnumIn).SetSubType(AutomataUI[0].myGUID + "_Transitions");
                init = false;
            }
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {

            if (AutomataUI.IsConnected)
            {
                Initialize();

                for (int i = 0; i < SpreadMax; i++)
                {
                    if (SetTime.IsChanged && SetTime[i])
                    {
                        var listOfItems = AutomataUI[0].transitionList.Where(r => r.Name == TransitionEnum.IOObject[i]).ToList();

                        foreach (var item in listOfItems)
                        {
                            item.Frames = TransitionTime[i];
                        }
                        AutomataUI[0].Invalidate();
                    }
                }
            }

            else { FLogger.Log(LogType.Debug, "No Connection"); }

        }
    }
}

