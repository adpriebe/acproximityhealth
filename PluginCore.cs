using System;
using System.Collections.Generic;
using System.Drawing;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;

/*
 * ProximityHealth plugin by Obliv-ion of Frostfell.
 * 
 * Plugin template created by Mag-nus. 8/19/2011, VVS added by Virindi-Inquisitor.
 * 
*/

namespace ProximityHealth
{
    //Attaches events from core
	[WireUpBaseEvents]

    //View (UI) handling
	[MVView("ProximityHealth.mainView.xml")]
    [MVWireUpControlEvents]

	[FriendlyName("ProximityHealth")]
	public class PluginCore : PluginBase
	{
        // key = objectID
        private IDictionary<int, Trackable> targets;
        private Queue<Trackable> updateQueue;

        /* configurable via view */
        public static bool pluginEnabled { get; set; }
        public static double acquireRange { get; set; }
        public static int maxTargets { get; set; }
        public static int updateFreq { get; set; }

        private const int MAX_MAX_TARGETS = 10;
        private int frameCounter = 0;

        private float scaleX = 0.5F;
        private float scaleY = 0.001F;
        private float preScale = 0.05F;
        private float textHeight = 1.1F;
        private float barHeight = 1.05F;
        private float textScale = 0.12F;

        public static bool debug = false;
        public static bool netDebug = false;
        public static bool uiDebug = false;
        public static bool targetDebug = false;

		protected override void Startup()
		{
			try
			{
				Globals.Init("ProximityHealth", Host, Core);

                //Initialize the view.
                MVWireupHelper.WireupStart(this, Host);
			}
			catch (Exception ex) { Util.LogError(ex); }
		}

		protected override void Shutdown()
		{
			try
			{
                //Destroy the view.
                MVWireupHelper.WireupEnd(this);
			}
			catch (Exception ex) { Util.LogError(ex); }
		}

        [BaseEvent("RenderFrame")]
        private void onRenderFrame(object sender, EventArgs e)
        {
            if (targets.Count > 0 && pluginEnabled)
            {
                if (frameCounter >= updateFreq)
                {
                    Util.log(LogChannels.CH_DEBUG, "Adding targets to update queue.");
                    foreach (Trackable t in targets.Values)
                    {
                        updateQueue.Enqueue(t);
                    }
                    frameCounter = 0;
                }
                else
                {
                    if (updateQueue.Count > 0 && (frameCounter % 2) == 0)
                    {
                        updateTarget(updateQueue.Dequeue().id);
                    }
                    frameCounter++;
                }
            }
        }

		[BaseEvent("LoginComplete", "CharacterFilter")]
		private void onLoginComplete(object sender, EventArgs e)
		{
			try
			{
                targets = new Dictionary<int, Trackable>();
                updateQueue = new Queue<Trackable>(MAX_MAX_TARGETS);

                //Load variables from file, or use defaults
                Util.xmlWriterSettings.Indent = true;
                Util.xmlWriterSettings.NewLineOnAttributes = true;
                if (!Util.loadFromXml())
                {
                    Util.log(LogChannels.CH_UI, "No settings file found. Using defaults.");
                    pluginEnabled = true;
                    acquireRange = 0.05;
                    maxTargets = 5;
                    updateFreq = 100;
                }

                // set the view controls
                enablePlugin.Checked = pluginEnabled;
                acquireRangeTextBox.Text = (acquireRange * 100).ToString();
                maxTargetsTextBox.Text = maxTargets.ToString();
                updateFreqTextBox.Text = updateFreq.ToString();

                // other init stuff
                enablePlugin.TooltipText = "Enable/Disable target tracking.";
                trackableCounter.TooltipText = "Current number of tracked targets.";
                acquireRangeTextBox.TooltipText = "Only track targets within this distance of your position.";
                maxTargetsTextBox.TooltipText = "Number of targets to track at once.";
                updateFreqTextBox.TooltipText = "How often - in FPS - to update tracked targets. 60 ~= 1 second.";
                saveButton.TooltipText = "Save configuration to file.";

				Util.log(LogChannels.CH_DEBUG, "Plugin loaded.");
			}
			catch (Exception ex) { Util.LogError(ex); }
		}

        private void updateTarget(int id)
        {
            try
            {
                CoreManager.Current.Actions.RequestId(id);
                Util.log(LogChannels.CH_DEBUG, "Requesting ID for: " + id.ToString("X"));
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void onLoginComplete(object sender, LogoffEventArgs e)
        {
            try
            {
                targets.Clear();
                updateQueue.Clear();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("ServerDispatch", "EchoFilter")]
        private void onServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            try
            {
                if (e.Message.Type == 0xF7B0 && pluginEnabled == true) // GameEvent
                {
                    if (e.Message.Value<uint>("event") == 0x00C9) // Identify event
                    {
                        if ((e.Message.Value<uint>("flags") & 0x00000100) != 0 && e.Message.Value<uint>("health") > 0)
                        {
                            //Util.log(LogChannels.CH_NET, "Got message for: " + e.Message.Value<int>("object").ToString("X"));
                            if (targets.ContainsKey(e.Message.Value<int>("object")))
                            {
                                updateHealthBars(targets[e.Message.Value<int>("object")], e.Message.Value<uint>("health"), e.Message.Value<uint>("healthMax"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void updateHealthBars(Trackable target, uint currHealth, uint maxHealth)
        {
            try
            {
                if (!pluginEnabled)
                    return;

                if (target.prevHealth == currHealth)
                {
                    Util.log(LogChannels.CH_DEBUG, "Target health unchanged. Skipping redraw.");
                    return;
                }
                target.prevHealth = currHealth;
                Util.log(LogChannels.CH_DEBUG, "Updating health bar for: " + target.id.ToString("X"));

                /* clean up old gfx */
                if (target.bar != null)
                    target.bar.Dispose();
                if (target.text != null)
                    target.text.Dispose();

                double healthPercent = (double)currHealth / (double)maxHealth;
                Color barColor;

                if (healthPercent <= 0.25)
                    barColor = Color.Red;
                else if (healthPercent <= 0.50)
                    barColor = Color.Orange;
                else if (healthPercent <= 0.75)
                    barColor = Color.Yellow;
                else
                    barColor = Color.Green;

                target.text = Core.D3DService.NewD3DObj();
                target.text.Anchor(target.id, textHeight, 0, 0, 0);
                target.text.SetText(D3DTextType.Text3D, (healthPercent * 100).ToString("0") + "%", "Consolas", 0);
                target.text.Color = Color.White.ToArgb();
                target.text.Scale(textScale);
                target.text.DrawBackface = true;
                target.text.OrientToCamera(false);
                target.text.Visible = true;

                target.bar = Core.D3DService.NewD3DObj();
                target.bar.Anchor(target.id, barHeight, 0, 0, 0);
                target.bar.SetShape(D3DShape.Cylinder);
                target.bar.Color = barColor.ToArgb();
                target.bar.Color2 = Color.Black.ToArgb();

                target.bar.Scale(preScale);
                target.bar.ScaleX = Convert.ToSingle((currHealth * scaleX) / maxHealth);
                target.bar.ScaleY = scaleY;

                target.bar.DrawBackface = false; // go back to true if bar goes invisible
                target.bar.OrientToCamera(false);

                target.bar.Visible = true;
            }
            catch (Exception ex) {
                //Util.log(LogChannels.CH_DEBUG, ex.Message+" \\id:"+target.id.ToString("X")+" \\ currHealth="+currHealth+" \\ maxHealth="+maxHealth+"ScaleX="+Convert.ToSingle((float)((currHealth * scaleX) / maxHealth)));
                Util.LogError(ex);
            }
        }

        [BaseEvent("Death", "CharacterFilter")]
        private void onMyDeath(object sender, DeathEventArgs e)
        {
            try
            {
                if (targets.Count > 0)
                {
                    targets.Clear();
                    updateQueue.Clear();

                    Util.log(LogChannels.CH_TARGET, "You died! Targets cleared.");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("ChangePortalMode", "CharacterFilter")]
        private void onPortalChange(object sender, ChangePortalModeEventArgs e)
        {
            try
            {
                if (targets != null && targets.Count > 0)
                {
                    targets.Clear();
                    updateQueue.Clear();
                    trackableCounter.Text = "Tracking: " + targets.Count;
                    Util.log(LogChannels.CH_TARGET, "Portaling. Targets cleared.");
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("MoveObject", "WorldFilter")]
        private void onMoveObject(object sender, MoveObjectEventArgs e)
        {
            try
            {
                if (e.Moved.ObjectClass == ObjectClass.Monster && pluginEnabled == true)
                {
                    double distance = CoreManager.Current.WorldFilter.Distance(CoreManager.Current.CharacterFilter.Id, e.Moved.Id);

                    if (distance <= acquireRange && targets.Count < maxTargets && !targets.ContainsKey(e.Moved.Id))
                    {
                        targets.Add(e.Moved.Id, new Trackable(e.Moved.Id));
                        trackableCounter.Text = "Tracking: "+targets.Count;

                        if (!e.Moved.HasIdData)
                        {
                            CoreManager.Current.Actions.RequestId(e.Moved.Id);
                        }

                        Util.log(LogChannels.CH_TARGET, e.Moved.Name + " (id:"+e.Moved.Id.ToString("X")+ " hasid:" + e.Moved.HasIdData + ") #" + targets.Count);
                    }
                    else if (distance > acquireRange && targets.ContainsKey(e.Moved.Id))
                    {
                        targets.Remove(e.Moved.Id);
                        trackableCounter.Text = "Tracking: " + targets.Count;
                        Util.log(LogChannels.CH_TARGET, e.Moved.Name+" moved out of range. #" + targets.Count);
                    }
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("ReleaseObject", "WorldFilter")]
        private void onReleaseObject(object sender, ReleaseObjectEventArgs e)
        {
            try
            {
                if (e.Released.ObjectClass == ObjectClass.Monster && pluginEnabled == true)
                {
                    if (targets.Remove(e.Released.Id))
                    {
                        trackableCounter.Text = "Tracking: " + targets.Count;
                        Util.log(LogChannels.CH_TARGET, e.Released.Name + " released. #" + targets.Count);
                    }
                }
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlReference("EnablePlugin")]
        private ICheckBox enablePlugin = null;

        [MVControlEvent("EnablePlugin", "Change")]
        void EnablePlugin(object sender, MVCheckBoxChangeEventArgs e)
        {
            try
            {
                pluginEnabled = e.Checked;
                Util.log(LogChannels.CH_UI, "Plugin state: " + e.Checked.ToString());
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlReference("SetAcquireRange")]
        private ITextBox acquireRangeTextBox = null;

        [MVControlEvent("SetAcquireRange", "Change")]
        void setAcquireRange(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                double range;
                if (Double.TryParse(e.Text, out range))
                {
                    acquireRangeTextBox.Text = e.Text;
                    acquireRange = range / 100;
                    Util.log(LogChannels.CH_UI, "Range set: " + acquireRange);
                }
                else
                {
                    acquireRangeTextBox.Text = "";
                    acquireRange = 5 / 100;
                }
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlReference("SetMaxTargets")]
        private ITextBox maxTargetsTextBox = null;

        [MVControlEvent("SetMaxTargets", "Change")]
        void setMaxTargets(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                int max;
                if (int.TryParse(e.Text, out max))
                {
                    if (max >= 1 && max <= 10)
                    {
                        maxTargetsTextBox.Text = e.Text;
                        maxTargets = max;
                        Util.log(LogChannels.CH_UI, "Max targets set: " + maxTargets);
                        return;
                    }
                }
                maxTargetsTextBox.Text = "";
                maxTargets = 1;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlReference("UpdateFrequency")]
        private ITextBox updateFreqTextBox = null;

        [MVControlEvent("UpdateFrequency", "Change")]
        void updateFrequency(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                int freq;
                if (int.TryParse(e.Text, out freq))
                {
                    if (freq >= 80 && freq <= 300)
                    {
                        updateFreqTextBox.Text = e.Text;
                        updateFreq = freq;
                        Util.log(LogChannels.CH_UI, "Update frequency set: " + updateFreq);
                        return;
                    }
                }
                //updateFreqTextBox.Text = "";
                updateFreq = 100;
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlReference("SaveSettings")]
        private IButton saveButton = null;

        [MVControlEvent("SaveSettings", "Click")]
        void saveSettings(object sender, MVControlEventArgs e)
        {
            try
            {
                Util.writeToXml(); 
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlReference("TrackableCounter")]
        private IStaticText trackableCounter = null;

        /*
        [MVControlReference("ScaleX")]
        private ITextBox ScaleXTextBox = null;

        [MVControlReference("ScaleY")]
        private ITextBox ScaleYTextBox = null;

        [MVControlReference("PreScale")]
        private ITextBox PreScaleTextBox = null;

        [MVControlReference("PostScale")]
        private ITextBox PostScaleTextBox = null;

        [MVControlReference("TextHeight")]
        private ITextBox TextHeightTextBox = null;

        [MVControlReference("BarHeight")]
        private ITextBox BarHeightTextBox = null;

        [MVControlReference("TextScale")]
        private ITextBox TextScaleTextBox = null;

        [MVControlEvent("ScaleX", "Change")]
        void ScaleX(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                ScaleXTextBox.Text = e.Text;
                scaleX = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "ScaleX: " + scaleX);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("ScaleY", "Change")]
        void ScaleY(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                ScaleYTextBox.Text = e.Text;
                scaleY = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "ScaleY: " + scaleY);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("PreScale", "Change")]
        void PreScale(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                PreScaleTextBox.Text = e.Text;
                preScale = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "PreScale: " + preScale);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("PostScale", "Change")]
        void PostScale(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                PostScaleTextBox.Text = e.Text;
                postScale = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "PostScale: " + postScale);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("BarHeight", "Change")]
        void BarHeight(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                BarHeightTextBox.Text = e.Text;
                barHeight = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "BarHeight: " + barHeight);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("TextHeight", "Change")]
        void TextHeight(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                TextHeightTextBox.Text = e.Text;
                textHeight = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "TextHeight: " + textHeight);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }

        [MVControlEvent("TextScale", "Change")]
        void TextScale(object sender, MVTextBoxChangeEventArgs e)
        {
            try
            {
                TextScaleTextBox.Text = e.Text;
                textScale = Convert.ToSingle(e.Text);
                Util.log(LogChannels.CH_UI, "TextScale: " + textScale);
            }
            catch (Exception ex)
            {
                Util.LogError(ex);
            }
        }
         */
	}
}
