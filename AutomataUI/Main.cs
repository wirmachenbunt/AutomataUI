#region usings
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using Automata.Data;
using Automata.Drawing;


using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
    public delegate void Changed(); //delegate type
    #region PluginInfo
    [PluginInfo(Name = "AutomataUI", Category = "Animation", Help = "Statemachine", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    

    public class AutomataUI : UserControl, IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        #region fields & pins

        //[Input("Default State", EnumName = "DefaultAutomataState", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
        protected IDiffSpread<EnumEntry> DefaultState;

        [Input("Focus Window", IsBang = true,Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> FocusWindow;

        [Config("License")]
        public IDiffSpread<string> License;

        [Config("StateXML")]
        public IDiffSpread<string> StateXML;

        [Config("TransitionXML")]
        public IDiffSpread<string> TransitionXML;

        [Config("TransitionNames")]
        public IDiffSpread<string> TransitionNames;

        [Config("Joreg Mode", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> JoregMode;

        [Config("Show Slice", IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<int> ShowSlice; //which slice of automata is shown ?

        [Output("Output")]
        public ISpread<String> FOutput;

        [Output("States")]
        public ISpread<String> StatesOut;

        [Output("Active State Index")]
        public ISpread<int> ActiveStateIndex;

        [Output("Target State Index")]
        public ISpread<int> TargetStateIndex;

        [Output("Transitions")]
        public ISpread<String> TransitionsOut;

        [Output("Transition Time Settings")]
        public ISpread<int> TransitionTimeSettingOut;

        [Output("Transition Index")]
        public ISpread<int> TransitionIndex;

        [Output("Transition Time")]
        public ISpread<int> TransitionFramesOut;

        [Output("Elapsed State Time")]
        public ISpread<int> ElapsedStateTime;

        [Output("AutomataUI")]
        public ISpread<AutomataUI> AutomataUIOut;

        [Import()]
        public ILogger FLogger;
        [Import()]
        public IIOFactory FIOFactory;
        [Import()]
        IPluginHost FHost;

        #endregion fields & pins

        #region variables
        
        public int x = 0; //mouse koordinaten
        public int y = 0;
        public Point previousPosition;

        State hitState = new State(); //hit detection
        Transition hitTransition = new Transition(); //hit detection

        public List<State> stateList = new List<State>();
        public List<Transition> transitionList = new List<Transition>();

        public State selectedState = null;
        public State startConnectionState = null;
        public State targetConnectionState = null;

        public string EnumName = "";
        public InputAttribute attr;

        PaintAutomataClass p = new PaintAutomataClass(); // create AutomataPaint Object

        private bool Initialize = true;
        public bool StatesChanged = false;

        string dragState = null;

        int counter = 0;

        
        public string licenseOwner = "Automata UI // Open Source Version";

        Dictionary<string, IIOContainer> FPins = new Dictionary<string, IIOContainer>(); //dynamic pins

        #endregion variables

        #region constructor and init

        public void OnImportsSatisfied()
        {
            TransitionNames.Changed += HandleTransitionPins;

            //dynamic enum attributes with unique name
            FHost.GetNodePath(true, out EnumName); //get unique node path
            EnumName += "AutomataUI"; // add unique name to path

            attr = new InputAttribute("Default State"); //name of pin
            attr.EnumName = EnumName;
            attr.DefaultEnumEntry = "Init"; //default state
            attr.Visibility = PinVisibility.OnlyInspector; //make invisible

            DefaultState = FIOFactory.CreateDiffSpread<EnumEntry>(attr);

        }

        private void HandleTransitionPins(IDiffSpread<string> sender)
        {
            //FLogger.Log(LogType.Debug, "Update Pins");

            //empty automata tree ? -> create a reset pin
            if (TransitionNames[0] == "")
            {
                TransitionNames[0] = "Init";
            }

            // CREATE INIT
            if (stateList.Count == 0)
            {
                stateList.Add(new State()
                {
                    ID = "Init",
                    Name = "Init",
                    Bounds = new Rectangle(new Point(0, 0), new Size(p.StateSize, p.StateSize))
                });
            }

            //delete pins which are not in the new list
            foreach (var name in FPins.Keys)
                if (TransitionNames.IndexOf(name) == -1)
                    FPins[name].Dispose();

            Dictionary<string, IIOContainer> newPins = new Dictionary<string, IIOContainer>();
            foreach (var name in TransitionNames)
            {
                if (!string.IsNullOrEmpty(name)) //ignore empty slices
                {
                    if (FPins.ContainsKey(name)) //pin already exists, copy to new dict
                    {
                        newPins.Add(name, FPins[name]);
                        FPins.Remove(name);
                    }
                    else if (!newPins.ContainsKey(name)) //just checking in case of duplicate names
                    {
                        var attr = new InputAttribute(name);
                        attr.IsBang = true;
                        var type = typeof(IDiffSpread<bool>);
                        var container = FIOFactory.CreateIOContainer(type, attr);
                        newPins.Add(name, container);
                    }
                }
            }

            //FPins now only holds disposed IIOContainers, since we copied the reusable ones to newPins
            FPins = newPins;

        }

        public AutomataUI()
        {
            //setup the gui
            InitializeComponent();

        }

        void InitializeComponent()
        {
            Controls.Clear(); //clear controls in case init is called multiple times

            //bind events
            MouseMove += Form1_MouseMove; //mouse move event
            MouseDoubleClick += Form1_MouseDoubleClick; //mouse click event
            MouseDown += Form1_MouseDown; //mouse down event
            MouseUp += Form1_MouseUp;

            Paint += p.PaintAutomata; //paint event
            p.InitAutomataDrawing(); //setup textalignment, arrows

            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

}

        private void InitSettings()
        {

            //load settings
            if (stateList.Count < 2 && StateXML[0].Length > 3 && Initialize)
            {

                try
                {
                    stateList = State.DataDeserializeState(StateXML[0]);
                    transitionList = Transition.DataDeserializeTransition(TransitionXML[0]);

                }
                catch { FLogger.Log(LogType.Debug, "Loading XML Graph failed!"); }


                EnumManager.UpdateEnum(EnumName, stateList[0].Name, stateList.Select(x => x.Name).ToArray());

                //repair relation
                foreach (Transition transition in transitionList)
                {
                    transition.startState = stateList.First(x => x.ID.Contains(transition.startState.ID));
                    transition.endState = stateList.First(x => x.ID.Contains(transition.endState.ID));
                }
                
                this.Invalidate();
                previousPosition = MousePosition;
                p.StagePos.X = 0;
                p.StagePos.Y = 0;

                UpdateOutputs(); //update State and Transition Outputs
                Initialize = false;
            }
        }
       
        #endregion constructor and init

        #region mouse    

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //hit detection for various use
            hitState = stateList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x,this.y)));
            hitTransition = transitionList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x, this.y)));

            previousPosition = MousePosition;

            //delete transitions
            if (e.Button == MouseButtons.Middle) DeleteTransition(e);

            //hit detection bezier transition
            if (e.Button == MouseButtons.Right)
            {
                int i = 0;
                p.bezierEdit.HighlightTransitionIndex = null;
                p.bezierEdit.highlightTransition = null;

                foreach ( GraphicsPath path in p.transitionPaths )
                {
                    if (path.IsOutlineVisible(this.x, this.y, p.greenPen))
                    {
                        p.bezierEdit.HighlightTransitionIndex = i;
                        p.bezierEdit.highlightTransition = transitionList[i];
                        //FLogger.Log(LogType.Debug, "hit transition");
                    }
                    i++;
                }
            }

            if (p.bezierEdit.bezierStart.Contains(new Point(this.x, this.y)) && e.Button == MouseButtons.Left) dragState = "bezierStart";
            if (p.bezierEdit.bezierEnd.Contains(new Point(this.x, this.y)) && e.Button == MouseButtons.Left) dragState = "bezierEnd";





            // Override Active State by CTRL Mouseclick
            if (e.Button == MouseButtons.Left && Form.ModifierKeys == Keys.Control && hitState != null)
            {
                ActiveStateIndex[ShowSlice[0]] = TargetStateIndex[ShowSlice[0]] = stateList.IndexOf(hitState);
                ElapsedStateTime[ShowSlice[0]] = TransitionFramesOut[ShowSlice[0]] = 0;
                this.Invalidate(); //redraw
            }
            
                // Override Active Transition by CTRL Mouseclick
                if (e.Button == MouseButtons.Left && Form.ModifierKeys == Keys.Control && hitTransition != null)
            {
                TargetStateIndex[ShowSlice[0]] = stateList.IndexOf(hitTransition.endState); // set target state index
                ActiveStateIndex[ShowSlice[0]] = stateList.IndexOf(hitTransition.startState);


                TransitionFramesOut[ShowSlice[0]] = hitTransition.Frames; // get frames of transition
                TransitionIndex[ShowSlice[0]] = transitionList.IndexOf(hitTransition); //get transition
                ElapsedStateTime[ShowSlice[0]] = 0; // stop ElapsedStateTimer
                
                FLogger.Log(LogType.Debug, "force transition");
                this.Invalidate(); //redraw
            }


            // empty hit ?
            if (hitState == null)
            {
                selectedState = null;
                startConnectionState = null;
            }

            else
            {
                selectedState = hitState; //on click set selected state for dragging

                if (startConnectionState != null && targetConnectionState != null && e.Button == MouseButtons.Left)
                {
                    //Create Transition
                    if (startConnectionState.ID != targetConnectionState.ID) AddTransition(startConnectionState, hitState);

                    startConnectionState = null;
                    targetConnectionState = null;
                    selectedState = null;
                }

                //set connection start
                if (selectedState != null && e.Button == MouseButtons.Right) startConnectionState = hitState;
                else startConnectionState = null;

                //delete state and relevant transitions
                if (selectedState != null && e.Button == MouseButtons.Middle && hitState.ID != "Init") DeleteState(e);

            }

            // hit test for spreadbuttons top left
            for (int i = 0; i < p.Spreadbuttons.Count; i++)
            {
                if (p.Spreadbuttons[i].Contains(new Point(e.X, e.Y)) && e.Button == MouseButtons.Left)
                {
                    ShowSlice[0] = i;
                    break;
                }
            }

        }
        
        private void Form1_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            if (hitState == null) selectedState = startConnectionState = null; //empty interaction states
 
            if (stateList.Count > 2) StateXML[0] = State.DataSerializeState(stateList); //update config

            if (dragState != null)
            {
                dragState = null;
                TransitionXML[0] = Transition.DataSerializeTransition(transitionList);
            }

        }     

        private void Form1_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            x = Convert.ToInt32((e.X - p.StagePos.X) / p.dpi);
            y = Convert.ToInt32((e.Y - p.StagePos.Y) / p.dpi);
            TransitionFramesOut[0] = 0;

            if (hitState == null && hitTransition == null && e.Button == MouseButtons.Left) AddState("MyState"); // Add State 

            else if (hitState != null && hitTransition == null ) EditState(hitState); //Edit State

            if (hitTransition != null && hitState == null) EditTransition(hitTransition); //Edit Transition

        }
      
        private void Form1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            x = Convert.ToInt32((e.X - p.StagePos.X)/p.dpi);
            y = Convert.ToInt32((e.Y - p.StagePos.Y)/p.dpi);

            //hittest states
            hitState = stateList.FirstOrDefault(x => x.Bounds.Contains(new Point(this.x, this.y)));

            #region drag bezier handles
            if (dragState != null && e.Button == MouseButtons.Left)
            {
                //FLogger.Log(LogType.Debug, "bezierHandleStart");
                Lines.EdgePoints myEdgePoints = Lines.GetEdgePoints(State.Center(p.bezierEdit.highlightTransition.startState.Bounds), State.Center(p.bezierEdit.highlightTransition.endState.Bounds), 40, 40, 0.0);
                if (dragState == "bezierStart") p.bezierEdit.highlightTransition.startBezierPoint = new Point(this.x - myEdgePoints.A.X, this.y - myEdgePoints.A.Y);
                if (dragState == "bezierEnd") p.bezierEdit.highlightTransition.endBezierPoint = new Point(this.x - myEdgePoints.B.X, this.y - myEdgePoints.B.Y);
            } 
            #endregion

            #region drag states
            if (selectedState == null && e.Button == MouseButtons.Right)
            {
                Point mousePos = MousePosition;
                int deltaX = (mousePos.X - previousPosition.X);
                int deltaY = (mousePos.Y - previousPosition.Y);
                previousPosition = MousePosition;
                p.StagePos.X += deltaX;
                p.StagePos.Y += deltaY;
            }             
            
            if (selectedState != null && e.Button == MouseButtons.Left && dragState == null)
            {
                selectedState.Move(new Point(Convert.ToInt32(e.X/p.dpi) - (p.StateSize / 2) - Convert.ToInt32(p.StagePos.X/p.dpi), Convert.ToInt32(e.Y/p.dpi) - (p.StateSize / 2) - Convert.ToInt32(p.StagePos.Y/p.dpi)));
            }

            #endregion

            #region startConnection

            if (startConnectionState != null && hitState != null)
            {
                targetConnectionState = hitState;
            }
            else targetConnectionState = null; 
            #endregion

            this.Invalidate(); //redraw
        }

        #endregion mouse

        #region Management
        private void AddTransition(State startState, State endState)
        {

            bool exists = false;

            //check if the transition already exists
            foreach (Transition transition in transitionList) // Loop through List with foreach.
            {
                if (transition.startState.ID == startState.ID
                && transition.endState.ID == endState.ID)
                {
                    exists = true;
                    break;
                }
                else exists = false;
            }

            // transition does not exist ? ok, create it
            if (exists == false)
            {
                string input = "My Transition"; //dialog text
                int frames = 1;
                bool pingpong = false;
                {
                    if (PaintAutomataClass.Dialogs.ShowTransitionDialog(ref input, ref frames, ref pingpong, "Add Transition",p.dpi) == DialogResult.OK)
                    {
                        //add transition
                        transitionList.Add(new Transition()
                        {
                            Name = input,
                            Frames = frames,
                            startState = startState,
                            endState = endState,
                            IsPingPong = pingpong,
                            startBezierPoint = new Point(0, 0), //angle lenght
                            endBezierPoint = new Point(0, 0)
                        });
                        //update config
                        UpdateTransitionConfigs();
                        UpdateOutputs();
                    }
                }
            }


        }

        private void EditTransition(Transition transition)
        {
            string input = transition.Name;
            int frames = transition.Frames;
            bool pingpong = transition.IsPingPong;

            if (PaintAutomataClass.Dialogs.ShowTransitionDialog(ref input, ref frames, ref pingpong, "Edit Transition",p.dpi) == DialogResult.OK)
            {

                transition.Name = input;
                transition.Frames = frames;
                transition.IsPingPong = pingpong;
                this.Invalidate();

                //update transition config
                UpdateTransitionConfigs();
                UpdateOutputs();
            }
        }

        private void DeleteTransition(System.Windows.Forms.MouseEventArgs e)
        {
            transitionList.RemoveAll(x => x.Bounds.Contains(new Point(this.x, this.y)));
            p.bezierEdit.HighlightTransitionIndex = null;
            p.bezierEdit.highlightTransition = null;
            UpdateTransitionConfigs();
            UpdateOutputs();
        }

        private void AddState(string input)
        {
            int frames = 0;
            if (PaintAutomataClass.Dialogs.ShowInputDialog(ref input, ref frames, "Add State",p.dpi) == DialogResult.OK)
            {
                //add state to state list
                stateList.Add(new State()
                {
                    ID = Automata.Data.State.RNGCharacterMask(),
                    Name = input,
                    Frames = frames,
                    Bounds = new Rectangle(new Point(x - (p.StateSize / 2), y - (p.StateSize / 2)), new Size(p.StateSize, p.StateSize))
                });

                UpdateStateConfigs(); // update JSON,Enums and Redraw
                UpdateOutputs();
            }
        }

        private void EditState(State state)
        {

            string input = state.Name;
            int frames = state.Frames;
            if (input != "Init") //edit state unless its init
            {
                if (PaintAutomataClass.Dialogs.ShowInputDialog(ref input, ref frames, "Edit State",p.dpi) == DialogResult.OK)
                {
                    state.Name = input;
                    state.Frames = frames;
                    UpdateStateConfigs(); // update JSON,Enums and Redraw
                    UpdateOutputs();
                }
            }
        }

        private void DeleteState(System.Windows.Forms.MouseEventArgs e)
        {
            stateList.RemoveAll(x => x.Bounds.Contains(new Point(this.x, this.y)));
            for (int i = transitionList.Count - 1; i >= 0; i--)
            {
                Transition transition = new Transition();
                transition = transitionList.ElementAt(i);

                if (hitState.ID == transition.startState.ID || hitState.ID == transition.endState.ID)
                {
                    transitionList.RemoveAt(i);
                }
            }

            ActiveStateIndex[0] = 0; //set active State
            TargetStateIndex[0] = 0; //set active TargetState

            UpdateStateConfigs(); // update JSON,Enums and Redraw
            UpdateTransitionConfigs();
            UpdateOutputs();

            
        }

        private void UpdateTransitionConfigs()
        {
            // Update Config Pin if there is a change

            TransitionXML[0] = Transition.DataSerializeTransition(transitionList);
            TransitionTimeSettingOut.SliceCount = 0;
            TransitionNames.SliceCount = 0;
            TransitionNames.Add("Reset To Default State"); // Default Reset Transition to Init
            foreach (Transition transition in transitionList) // Loop through List with foreach.
            {
                TransitionNames.Add(transition.Name);
                TransitionTimeSettingOut.Add(transition.Frames);
            }

        }

        private void UpdateStateConfigs()
        {
            this.Invalidate();
            //update Default State Enum
            EnumManager.UpdateEnum(EnumName, stateList[0].Name, stateList.Select(x => x.Name).ToArray());
            StateXML[0] = State.DataSerializeState(stateList); //save config
            StatesChanged = true;
        }

        private void UpdateOutputs()
        {
            StatesOut.SliceCount = 0;
            foreach (State state in stateList) // Loop through List with foreach.
            {
                StatesOut.Add(state.Name);
            }


            TransitionTimeSettingOut.SliceCount = 0;
            TransitionsOut.SliceCount = 0;
            foreach (Transition transition in transitionList) // Loop through List with foreach.
            {
                TransitionsOut.Add(transition.Name);
                TransitionTimeSettingOut.Add(transition.Frames);
            }
            TransitionsOut.Add("∅");
        }

        #endregion Management

        public void StateChangeCounter()
        {
            if (StatesChanged) { counter += 1; }
            if (counter > 2)
            {
                StatesChanged = false;
                counter = 0;
            }
        }

        public void Evaluate(int SpreadMax)
        {
            
                InitSettings(); // Load previous setting and setup certain variables
            
            ActiveStateIndex.SliceCount 
                = TargetStateIndex.SliceCount 
                = TransitionIndex.SliceCount 
                = TransitionFramesOut.SliceCount 
                = ElapsedStateTime.SliceCount 
                = FOutput.SliceCount = SpreadMax; //make spreadable , set Spreadmax

            #region TriggerTransitions
            for (int ii = 0; ii < SpreadMax; ii++) //spreadable loop 01
            {
                foreach (var pin in FPins)
             
                {
                    var diffpin = pin.Value.RawIOObject as IDiffSpread<bool>;
                    if (diffpin[ii] == true && diffpin.SliceCount != 0) //diffpin.IsChanged && JONAS WUNSCHKONZERT
                    {
                        
                        //FLogger.Log(LogType.Debug,pin.ToString());
                        UpdateOutputs(); // output all States and Transitions

                        if (pin.Key == "Reset To Default State") // Reset to Init State
                        {

                            // Get Enum Index From Default State and Set Active State
                            ActiveStateIndex[ii] = DefaultState[ii].Index; // index ist 1 statt 0 beta34.2 bug
                            TargetStateIndex[ii] = DefaultState[ii].Index;
                            ElapsedStateTime[ii] = 0; // Reset Timer
                            TransitionFramesOut[ii] = 0; // Reset Timer
                            this.Invalidate();
                        }
                        else 
                        {
                            //Find Transition
                            int i = 0;
                            foreach (Transition transition in transitionList)
                            {
                                // standard transitions
                                if (transition.Name == pin.Key &&
                                    transition.startState.ID == stateList.ElementAt(ActiveStateIndex[ii]).ID &&
                                    TransitionFramesOut[ii] == 0 &&
                                    ElapsedStateTime[ii] >= transition.startState.Frames)
                                {
                                    TargetStateIndex[ii] = stateList.IndexOf(transition.endState); // set target state index
                                    TransitionFramesOut[ii] = transition.Frames; // get frames of transition
                                    TransitionIndex[ii] = i; //get transition
                                    ElapsedStateTime[ii] = 0; // stop ElapsedStateTimer
                                    this.Invalidate(); //redraw
                                    //FLogger.Log(LogType.Debug, "redraw");
                                    break;
                                }

                                //pingpong transitions - return to startstate , previous test covers transition to targetstate
                                if (transition.Name == pin.Key &&
                                    transition.endState.ID == stateList.ElementAt(ActiveStateIndex[ii]).ID &&
                                    TransitionFramesOut[ii] == 0 &&
                                    transition.IsPingPong &&
                                    ElapsedStateTime[ii] >= transition.endState.Frames)
                                {
                                    TargetStateIndex[ii] = stateList.IndexOf(transition.startState); // set target state index
                                    TransitionFramesOut[ii] = transition.Frames; // get frames of transition, hier war +1
                                    TransitionIndex[ii] = i; //get transition
                                    ElapsedStateTime[ii] = 0; // stop ElapsedStateTimer
                                    this.Invalidate(); //redraw
                                    
                                    break;
                                }
                                i++;
                            }
                        }
                            
                        }
                }
            }
            #endregion TriggerTransitions

            #region TimingAndIndices

            for (int ii = 0; ii < SpreadMax; ii++) //spreadable loop 02
            {
                // set active Transition,State and Timers 

                if (ActiveStateIndex[ii] != TargetStateIndex[ii] && TransitionFramesOut[ii] != 0) // solange target und active ungleich sind, läuft die transitions
                {
                    TransitionFramesOut[ii] -= 1; // run Transition Timer 
                    FOutput[ii] = TransitionsOut[TransitionIndex[ii]]; //set summarized output to transition
                }
                else FOutput[ii] = StatesOut[ActiveStateIndex[ii]]; // set summarized output to state

                //passiert nur einmal
                if (TransitionFramesOut[ii] == 0 && ElapsedStateTime[ii] == 0) //solange transition time und elapsedtime 0 sind, setze target und active gleich
                {
                    ActiveStateIndex[ii] = TargetStateIndex[ii]; // after transition set activestate to targetstate
                    TransitionIndex[ii] = TransitionsOut.SliceCount - 1;
                    this.Invalidate(); //redraw
                    //FLogger.Log(LogType.Debug, "Transition Ends");
                }

                if (TransitionFramesOut[ii] == 0) ElapsedStateTime[ii] += 1; // Run State Timer when TransitionTimer is 0
            }
            
            if (JoregMode.IsChanged && JoregMode[0]) p.JoregMode(this, true);   //Joreg Mode
            else if (JoregMode.IsChanged && !JoregMode[0]) p.JoregMode(this, false);
          
            if (ShowSlice.IsChanged || ActiveStateIndex.IsChanged) this.Invalidate(); // redraw if you want to see another slice of automata
            
            #endregion TimingAndIndices

            AutomataUIOut.SliceCount = 1; // set output for additional nodes
            AutomataUIOut[0] = this;

            StateChangeCounter(); // inform child nodes about state changes , pretty dirty

            if (FocusWindow[0] && FocusWindow.IsChanged) this.Focus(); //bring window to front

            

        }
    }
}
