#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Linq;
using VVVV.Nodes;
using System.Drawing;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "GetRegion",
                Category = "AutomataUI Animation",
                Help = "get a state information from AutomataUI",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class GetRegion : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        IIOContainer<IDiffSpread<EnumEntry>> RegionEnum;

        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Output("RegionActive")]
        public ISpread<bool> RegionActive;

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
            InputAttribute attr = new InputAttribute("Region");
            RegionEnum = FIOFactory.CreateIOContainer<IDiffSpread<EnumEntry>>(attr, true);
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
                var pin = RegionEnum.GetPluginIO() as IPin;
                (pin as IEnumIn).SetSubType(AutomataUI[0].myGUID + "_Regions");
                init = false;
            }

        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (AutomataUI.IsConnected)
            {
                Initialize();

                if (AutomataUI[0].regionList.Count > 0)
                {
                    RegionActive.SliceCount = RegionEnum.IOObject.SliceCount * AutomataUI[0].ActiveStateIndex.SliceCount; //set Slicecount to amount of incoming Automatas

                    for (int j = 0; j < RegionEnum.IOObject.SliceCount; j++)
                    {


                        int index = RegionEnum.IOObject[j].Index;


                        Rectangle thisRegion = AutomataUI[0].regionList[index].Bounds;


                        for (int i = 0; i < AutomataUI[0].ActiveStateIndex.SliceCount; i++) // spreaded
                        {

                            int offset = i + (j * AutomataUI[0].ActiveStateIndex.SliceCount);

                            int stateIndex = AutomataUI[0].ActiveStateIndex[i];
                            Rectangle state = AutomataUI[0].stateList[stateIndex].Bounds;


                            Rectangle transition;

                            int transitionIndex = AutomataUI[0].TransitionIndex[i];
                            if (transitionIndex == AutomataUI[0].transitionList.Count) // index ist "fake" nil transiton, dann ist keine transition aktiv
                            {
                                transitionIndex = 0;
                                transition = new Rectangle(10000000, 10000000, 0, 0);
                            }
                            else //wenn transition aktiv ist, kann kein state aktiv sein
                            {
                                transition = AutomataUI[0].transitionList[transitionIndex].Bounds;
                                state = new Rectangle(10000000, 10000000, 0, 0); ;
                            }

                            if (thisRegion.IntersectsWith(state) || thisRegion.IntersectsWith(transition)) RegionActive[offset] = true;
                            else RegionActive[offset] = false;
                        }
                    }
                }
            }
        }
    }
}
