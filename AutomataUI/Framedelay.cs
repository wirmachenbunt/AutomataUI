using System;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.Generic;

namespace VVVV.Nodes
{
    //1.) do a 'replace all' for REPLACEME_CLASS with the name of your own type

    //2.) do a 'replace all' for NODECATEGORY to set the version and the class name prefix for all node (e.g. Value, Color...)

    //3.) you have to override the Copy or the CopySpread method for your type. overriding Copy is easier, CopySpread might allow some performance optimizations

    [PluginInfo(Name = "FrameDelay",
                Category = "AutomataUI Animation",
                Help = "Delays the input value one calculation frame.",
                Tags = "generic"
               )]
    public class AutomataUIFrameDelayNode : FrameDelayNode<AutomataUI>
    {
        public AutomataUIFrameDelayNode() : base(AutomataUICopier.Default) { }
    }

    class AutomataUICopier : Copier<AutomataUI>
    {
        public static readonly AutomataUICopier Default = new AutomataUICopier();

        public override AutomataUI Copy(AutomataUI value)
        {
            //row new NotImplementedException("You need to implement the Copy method");
            var myCopy = new AutomataUI();
            myCopy = value;
            return myCopy;
        }
    }
}
