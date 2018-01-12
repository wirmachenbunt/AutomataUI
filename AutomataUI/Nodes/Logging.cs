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
    [PluginInfo(Name = "Logging",
                Category = "AutomataUI Animation",
                Help = "log state changes with time",
                Tags = "",
                AutoEvaluate = true)]
    #endregion PluginInfo
    public class Logging : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("AutomataUI")]
        public Pin<AutomataUI> AutomataUI;

        [Input("Filename", StringType = StringType.Filename)]
        public ISpread<string> FFile;

        [Input("Message")]
        public ISpread<string> FMessage;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        int stateindex;

        private bool invalidate = true;

        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            AutomataUI.Connected += Input_Connected;
            AutomataUI.Disconnected += Input_Disconnected;
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

        public async Task FileWriteAsync(string filePath, string messaage, bool append = true)
        {
            using (FileStream stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            using (StreamWriter sw = new StreamWriter(stream))
            {
                await sw.WriteLineAsync(messaage);
            }
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {

            try
            {
                if (AutomataUI[0].ActiveStateIndex[0] != stateindex)
                {
                    string DateTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff tt");
                    string StateName = AutomataUI[0].stateList[AutomataUI[0].ActiveStateIndex[0]].Name;

                    FileWriteAsync(FFile[0], DateTime + ";" + StateName + ";" + FMessage[0], true);
                    //FLogger.Log(LogType.Debug, "Write Something");

                }

                stateindex = AutomataUI[0].ActiveStateIndex[0];
            }
            catch { FLogger.Log(LogType.Debug, "No Connection"); }

        }
    }
}

