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
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "TriggerTransition",
                Category = "AutomataUI Animation",
                Help = "trigger transitions",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class TriggerTranstion : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        protected IDiffSpread<EnumEntry> EnumTransition;

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

        string EnumName;

        private bool invalidate = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;

            FHost.GetNodePath(true, out EnumName); //get unique node path
            EnumName += "AutomataUI"; // add unique name to path
            InputAttribute attr = new InputAttribute("Transition"); //name of pin
            attr.EnumName = EnumName;
            //attr.DefaultEnumEntry = "Init"; //default state
            EnumTransition = FIOFactory.CreateDiffSpread<EnumEntry>(attr);
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
            if(AutomataUI[0] == null) return;
            if (invalidate || AutomataUI[0].StatesChanged)
            {
                EnumManager.UpdateEnum(EnumName, AutomataUI[0].transitionList[0].Name, AutomataUI[0].transitionList.Select(x => x.Name).ToArray());
                invalidate = false;
            }

            for (int i = 0; i < FTrigger.SliceCount; i++)
            {
                if(FTrigger[i]) AutomataUI[0].TriggerTransition(EnumTransition[0].Name, i);
            }
        }
    }
}

