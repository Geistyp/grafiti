﻿/*
	Grafiti Demo Application

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

using Grafiti;
using TUIO;

namespace ClientNamespace
{
    public class SimpleGR : LocalGestureRecognizer
    {
        public enum Events
        {
            SimpleGesture
        }

        public SimpleGR(GRConfiguration configuration) : base(configuration) { }

        public event GestureEventHandler SimpleGesture;

        protected virtual void OnSimpleGesture() { AppendEvent(SimpleGesture, new GestureEventArgs()); }

        public override GestureRecognitionResult Process(List<Trace> traces)
        {
            // if the gesture has been alive for 2 seconds (and is moving)
            if (traces[0].Last.TimeStamp - traces[0].First.TimeStamp >= 2000)
            {
                OnSimpleGesture();
                return new GestureRecognitionResult(false, true, false);
            }
            else
                return new GestureRecognitionResult(true, false, false);
        }
    }
}