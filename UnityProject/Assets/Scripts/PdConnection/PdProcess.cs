using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace cylvester
{
    interface IPdProcess
    {
        void Start(string mainPatch, int numInputChannels);
        void Stop();
        bool Running { get; }
    }
    
    public class PdProcess : IPdProcess
    {
        private static PdProcess instance_ = null;
        private Process pdProcess_;

        private PdProcess()
        {
        } // cannot be instantiate normally
        

        public static PdProcess Instance => instance_ ?? (instance_ = new PdProcess());

        public void Start(string mainPatch, int numInputChannels)
        {

            if (pdProcess_ != null)
            {
                pdProcess_.Refresh();
                if (!pdProcess_.HasExited)
                    return;
            }

            pdProcess_ = new Process();
            pdProcess_.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pdProcess_.StartInfo.UseShellExecute = false;
            pdProcess_.StartInfo.FileName = Application.streamingAssetsPath + "/pd/win/pd.com";

            var path = Application.streamingAssetsPath + "/pd/patch/" + mainPatch;
            pdProcess_.StartInfo.Arguments = "-nogui -rt -inchannels " + numInputChannels + " " + path;

            if (!pdProcess_.Start())
            {
                throw new Exception("Pd process failed to start");
            }
            Thread.Sleep(500);
            Debug.Log("Pd Process started");
        }
    
        public void Stop()
        {
            if (pdProcess_ == null)
                return;
            
            pdProcess_.Kill();
            pdProcess_ = null;
            Debug.Log("Pd Process stopped");
            
        }

        public bool Running
        {
            get
            {
                pdProcess_.Refresh();
                if (pdProcess_ == null)
                    return false;
                if (pdProcess_.HasExited)
                    return false;
                
                return true;
            }
        }
    }
}