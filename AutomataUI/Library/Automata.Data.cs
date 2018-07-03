using System;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace Automata.Data
{
	public class State
	{
		public string ID
		{
			get;
			set;
		}
		
		public string Name
		{
			get;
			set;
		}
		
		public bool Active
		{
			get;
			set;
		}
		
		public int Frames //how many frames is the state locked
		{
			get;
			set;
		}
	
		public Rectangle Bounds
		{
			get;
			set;
        }//size of state

        public void Move(Point currentMousePoint)
		{
			Bounds = new Rectangle(currentMousePoint, Bounds.Size);
		}
		
		public static Point Center(Rectangle rect)
		{
			return new Point(rect.Left + rect.Width/2,
			rect.Top + rect.Height / 2);
		}


//serialize deserialize
        public static string DataSerializeState(List<State> myList)
        {
            StringWriter sw = new StringWriter();
            XmlSerializer s = new XmlSerializer(myList.GetType());
            s.Serialize(sw, myList);
            return sw.ToString();
        }

        public static List<State> DataDeserializeState(string data)
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<State>));
            List<State> newList = (List<State>)xs.Deserialize(new StringReader(data));
            return newList;
        }
//serialize deserialize

        public static string RNGCharacterMask()
		{
			int maxSize  = 8 ;
			//int minSize = 5 ;
			char[] chars = new char[62];
			string a;
			a = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
			chars = a.ToCharArray();
			int size  = maxSize ;
			byte[] data = new byte[1];
			RNGCryptoServiceProvider  crypto = new RNGCryptoServiceProvider();
			crypto.GetNonZeroBytes(data) ;
			size =  maxSize ;
			data = new byte[size];
			crypto.GetNonZeroBytes(data);
			StringBuilder result = new StringBuilder(size) ;
			foreach(byte b in data )
			{ result.Append(chars[b % (chars.Length - 1)]); }
			return result.ToString();
		}
	}
	
	public class Transition
	{
		public string Name
		{
			get;
			set;
		}
		
		public State startState
		{
			get;
			set;
		}
		
		public State endState
		{
			get;
			set;
		}

        public Point startBezierPoint //bezierControlSetting Angle and Lenght
        {
            get;
            set;
        }

        public Point endBezierPoint  //bezierControlSetting Angle and Lenght
        {
            get;
            set;
        }


        public int Frames //how long does the transition take
		{
			get;
			set;
		}

        public bool IsPingPong //how long does the transition take
        {
            get;
            set;
        }

        public Rectangle Bounds //size of transition
        {
			get;
			set;
		}

        //serialize deserialize
        public static string DataSerializeTransition(List<Transition> myList)
        {
            StringWriter sw = new StringWriter();
            XmlSerializer s = new XmlSerializer(myList.GetType());
            s.Serialize(sw, myList);
            return sw.ToString();
        }

        public static List<Transition> DataDeserializeTransition(string data)
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<Transition>));
            List<Transition> newList = (List<Transition>)xs.Deserialize(new StringReader(data));
            return newList;
        }
        //serialize deserialize


    }

    public class AutomataRegion
    {
        public string Name
        {
            get;
            set;
        }

        public Rectangle Bounds
        {
            get;
            set;
        }

        public Rectangle SizeHandle
        {
            get;
            set;
        }

        //serialize deserialize
        public static string DataSerializeRegion(List<AutomataRegion> myList)
        {
            StringWriter sw = new StringWriter();
            XmlSerializer s = new XmlSerializer(myList.GetType());
            s.Serialize(sw, myList);
            return sw.ToString();
        }

        public static List<AutomataRegion> DataDeserializeRegion(string data)
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<AutomataRegion>));
            List<AutomataRegion> newList = (List<AutomataRegion>)xs.Deserialize(new StringReader(data));
            return newList;
        }
        //serialize deserialize
    }
}