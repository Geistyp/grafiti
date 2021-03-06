/*
	GrafitiDemo, Grafiti demo application

    Copyright 2008  Alessandro De Nardi <alessandro.denardi@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License as
    published by the Free Software Foundation; either version 3 of 
    the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Grafiti;
using TUIO;

namespace Grafiti.GestureRecognizers
{
    public interface ISelectable
    {
        float X { get; }
        float Y { get; }
    }

    public class LazoGREventArgs : GestureEventArgs
    {
        private List<ISelectable> m_selected;

        public List<ISelectable> Selected { get { return m_selected; } }

        public LazoGREventArgs(string eventId, int groupId, List<ISelectable> selected)
            : base(eventId, groupId)
        {
            m_selected = selected;
        }
    }

    public class LazoGRConfiguration : GRConfiguration
    {
        private List<ISelectable> m_currentTuioObjects;
        private float m_threshold;

        public List<ISelectable> CurrentTuioObjects { get { return m_currentTuioObjects; } }
        public float Threshold { get { return m_threshold; } }

        public LazoGRConfiguration()
            : this(null) { }

        public LazoGRConfiguration(List<ISelectable> currentTuioObjects)
            : this(currentTuioObjects, 1f) { }

        public LazoGRConfiguration(List<ISelectable> currentTuioObjects, float threshold)
            : base(true) // Default is exclusive
        {
            m_currentTuioObjects = currentTuioObjects;
            m_threshold = threshold;
        }
    }

    public class LazoGR : GlobalGestureRecognizer
    {
        private List<ISelectable> m_currentTuioObjects;
        private float m_threshold;
        private List<ISelectable> m_tuioObjectsInPoly = new List<ISelectable>();

        public LazoGR()
            : base(null) { }

        public LazoGR(GRConfiguration configuration)
            : base(configuration)
        {
            LazoGRConfiguration conf = (LazoGRConfiguration)Configuration;
            m_currentTuioObjects = conf.CurrentTuioObjects;
            m_threshold = conf.Threshold;
            DefaultEvents = new string[] { "Lazo" };
        }

        public event GestureEventHandler Lazo;

        public override void Process(List<Trace> traces)
        {
            if (Group.Traces.Count != 1)
            {
                Terminate(false);
                return;
            }

            Trace trace = traces[0];

            if (trace.State == Trace.States.REMOVED)
            {
                if (trace.First.SquareDistance(trace.Last) <= m_threshold)
                {
                    foreach (ISelectable obj in m_currentTuioObjects)
                        if (PointInPath(obj.X, obj.Y, trace))
                            m_tuioObjectsInPoly.Add(obj);

                    if (m_tuioObjectsInPoly.Count > 0)
                    {
                        AppendEvent(Lazo, new LazoGREventArgs("Lazo", Group.Id, m_tuioObjectsInPoly));
                        Terminate(true);
                        return;
                    }
                }
                Terminate(false);
            }
        }

        /// <summary>
        /// Determine if a point is inside the polygon described by the trace.
        /// Thanks to Daniel Kuppitz who posted a message with the code in a forum.
        /// </summary>
        /// <param name="x">X coordinate of the point.</param>
        /// <param name="y">Y coordinate of the point.</param>
        /// <param name="trace">The trace describing the polygon.</param>
        /// <returns>True iff the given point is inside the polygon of the trace's path.</returns>
        public static bool PointInPath(float x, float y, Trace trace)
        {
            PointF p1, p2;
            bool inside = false;

            if (trace.Count < 3)
            {
                return inside;
            }

            PointF oldPoint = new PointF(trace.Last.X, trace.Last.Y);

            for (int i = 0; i < trace.Count; i++)
            {
                PointF newPoint = new PointF(trace[i].X, trace[i].Y);

                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.X < x) == (x <= oldPoint.X) &&
                    ((double)y - (double)p1.Y) * (double)(p2.X - p1.X) <
                    ((double)p2.Y - (double)p1.Y) * (double)(x - p1.X))
                {
                    inside = !inside;
                }

                oldPoint = newPoint;
            }

            return inside;
        }

        private struct PointF
        {
            public float X, Y;
            public PointF(float x, float y)
            {
                X = x;
                Y = y;
            }
        }
    }
}