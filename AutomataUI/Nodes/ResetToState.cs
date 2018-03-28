
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
    [PluginInfo(Name = "ResetToState",
                Category = "AutomataUI Animation",
                Help = "trigger transitions",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class ResetToState : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        IIOContainer<IDiffSpread<EnumEntry>> StatesEnum;

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Input("Trigger", IsBang = true)]
        public ISpread<bool> FTrigger;

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
            if (init)
            {
                var pin = StatesEnum.GetPluginIO() as IPin;
                (pin as IEnumIn).SetSubType(AutomataUI[0].myGUID + "_States");
                init = false;
            }
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (AutomataUI[0] == null) return;

            if (AutomataUI.IsConnected)
            {
                Initialize();
                for (int i = 0; i < FTrigger.SliceCount; i++)
                {
                    if (FTrigger[i]) AutomataUI[0].TriggerTransition("Reset To Default State", i, StatesEnum.IOObject[i].Index);
                }
            }
        }
    }
}

