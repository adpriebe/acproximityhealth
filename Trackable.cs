using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace ProximityHealth
{
    /*
     * This class encapsulates information relating to a trackable enemy.
     */
    class Trackable
    {
        public int id { get; set; } // this enemy's WorldObject
        public D3DObj bar { get; set; } // graphical health bar using D3DRenderService, not HUD RenderService
        public D3DObj text { get; set; } // graphical representation of enemy's health
        public uint prevHealth { get; set; }

        public Trackable(int id)
        {
            this.id = id; // ID from monster's WorldObject
            bar = null;
            text = null;
            prevHealth = 0;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Trackable t = obj as Trackable;
            if ((System.Object)t == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (id == t.id);
        }

        public bool Equals(Trackable t)
        {
            // If parameter is null return false:
            if ((object)t == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (id == t.id);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ id;
        }
    }
}
