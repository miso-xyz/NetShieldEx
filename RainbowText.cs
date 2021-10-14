using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transitions;
using System.Windows.Forms;
using System.Drawing;

namespace NetShield_Protector
{
    class RainbowObj
    {
        public RainbowObj(object Obj, string PropertyName, ITransitionType TransitionType, int interval = 100)
        {
            timer = new Timer() { Interval = interval };
            timer.Tick += timerFunc;
            transitionType = TransitionType;
            propertyName = PropertyName;
            obj = Obj;
            colorCycle = new Color[] { Color.Red, Color.Magenta, Color.Blue, Color.Cyan, Color.Green, Color.Yellow };
        }

        private Timer timer { get; set; }

        public object obj {get;set;}
        public Color[] colorCycle {get;set;}
        public ITransitionType transitionType { get; set; }
        public string propertyName { get; set; }

        public void Start() { timer.Start(); }
        public void Stop() { timer.Stop(); }

        public int currentColorIndex = 0;
        private void timerFunc(object sender, EventArgs e)
        {
            if (currentColorIndex >= colorCycle.Count()) { currentColorIndex = 0; }
            if (obj is Control) { ((Control)obj).Update(); }
            Transition.run(obj, propertyName, colorCycle[currentColorIndex++], transitionType);
        }
    }
}
