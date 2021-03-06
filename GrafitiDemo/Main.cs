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
using System.Collections;
using System.Threading;
using TUIO;
using Grafiti;
using Grafiti.GestureRecognizers;
using Tao.FreeGlut;
using Tao.OpenGl;
//using System.Runtime.InteropServices;

namespace GrafitiDemo
{
    public class Viewer : TuioListener, IGrafitiClientGUIManager, IGestureListener
    {
        #region Declarations

        // Tuio client
        private static TuioClient s_client;

        // Tuio listeners:
        //
        // Grafiti's surface (listens to TuioCursors messages)
        private static Surface s_grafitiSurface;
        //
        // Manages tuio objects (listens to TuioObject messages).
        private static DemoObjectManager s_demoObjectManager;
        //
        // The instance of this class is an auxiliar listener.
        // Retrieves data from Grafiti synchronously to output a visual feedback.
        // Also calls the display function of the Tuio objects' manager.
        private static Viewer s_viewer;


        private Calculator m_calculator = new Calculator();

        // Manages the visual feedback of Grafiti's groups of fingers
        private List<DemoGroup> m_demoGroups = new List<DemoGroup>();


        // Coordinates stuff
        private int m_window_left = 0;
        private int m_window_top = 0;
        private int m_window_width = 640;
        private int m_window_height = 480;
        private float m_projectionX;
        private float m_projectionY;
        private float m_projectionW;
        private float m_projectionH;
        private float m_projectionYAngle;

        // Graphic variables
        private bool m_displayCalibrationGrid = false;
        private static int s_timerTime = 12;
        private bool m_fullscreen = false;

        private bool m_displayCalculator = true;

        // Auxiliar variables
        private static object s_lock = new object();
        private const string m_name = "Grafiti Demo";
        #endregion

        #region Constructors
        private Viewer()
        {
            GestureEventManager.SetPriorityNumber(typeof(CircleGR), 3);
            GestureEventManager.RegisterHandler(typeof(CircleGR), "Circle", OnCircleGesture);
        }
        private Viewer(float x, float y, float w, float h, float a) : this()
        {
            m_projectionX = x;
            m_projectionY = y;
            m_projectionW = w;
            m_projectionH = h;
            m_projectionYAngle = a;
        }
        #endregion

        #region Gesture event handlers
        public void OnCircleGesture(object gr, GestureEventArgs args)
        {
            CircleGREventArgs cArgs = (CircleGREventArgs)args;
            
            lock (s_lock)
            {
                m_displayCalculator = !m_displayCalculator;
                if (m_displayCalculator)
                {
                    m_calculator.X = cArgs.MeanCenterX;
                    m_calculator.Y = cArgs.MeanCenterY;
                    m_calculator.Size = cArgs.MeanRadius * 2;
                } 
            }
        }
        #endregion

        #region TuioListener members
        public void addTuioObject(TuioObject o) { }
        public void updateTuioObject(TuioObject o) { }
        public void removeTuioObject(TuioObject o) { }
        public void addTuioCursor(TuioCursor c) { }
        public void updateTuioCursor(TuioCursor c) { }
        public void removeTuioCursor(TuioCursor c) { }
        public void refresh(long timestamp)
        {
            lock (s_lock)
            {
                // Add demo groups
                foreach (Group group in s_grafitiSurface.AddedGroups)
                    m_demoGroups.Add(new DemoGroup(group));

                // Remove demo groups
                m_demoGroups.RemoveAll(delegate(DemoGroup demoGroup)
                {
                    return (s_grafitiSurface.RemovedGroups.Contains(demoGroup.Group));
                });

                foreach (DemoGroup demoGroup in m_demoGroups)
                    demoGroup.Update(timestamp);
            }
        }
        #endregion

        #region IGrafitiClientGUIManager Members
        public void GetVisualsAt(float x, float y,
            out IGestureListener zGestureListener,
            out object zControl,
            out List<ITangibleGestureListener> tangibleListeners)
        {
			if(m_displayCalculator)
				m_calculator.GetTargetAt(x, y, out zGestureListener, out zControl);
			else
			{
				zGestureListener = null;
				zControl = null;
			}
            tangibleListeners = s_demoObjectManager.HitTestTangibles(x, y);
        }
        public void PointToClient(IGestureListener target, float x, float y, out float cx, out float cy)
        {
            cx = x;
            cy = y;
        }
        #endregion



        [STAThread]
        static void Main(String[] argv)
        {
            float x = 0, y = 0, w = 10, h = 10, a = 0;

            int port = 3333;
            switch (argv.Length)
            {
                case 0:
                    break;

                case 1:
                    port = int.Parse(argv[0], null);
                    if (port == 0) goto default;
                    break;

                case 5:
                    x = float.Parse(argv[0], null);
                    y = float.Parse(argv[1], null);
                    w = float.Parse(argv[2], null);
                    h = float.Parse(argv[3], null);
                    a = float.Parse(argv[4], null);
                    break;

                case 6:
                    x = float.Parse(argv[1], null);
                    y = float.Parse(argv[2], null);
                    w = float.Parse(argv[3], null);
                    h = float.Parse(argv[4], null);
                    a = float.Parse(argv[5], null);
                    goto case 1;

                default:
                    Console.WriteLine("Usage: [mono] GrafitiGenericDemo [port] [x y w h a]");
                    System.Environment.Exit(0);
                    break;
            }

            // Force compilation of GR classes
            new PinchingGR();
            new BasicMultiFingerGR();
            new MultiTraceGR();
            new CircleGR();
            new LazoGR();
            new RemovingLinkGR();


            // instantiate viewer
            s_viewer = new Viewer(x, y, w, h, a);

            // instantiate Grafiti
            s_grafitiSurface = Surface.Initialize(s_viewer);

            // instantiate objects' manager
            s_demoObjectManager = new DemoObjectManager(s_viewer);

            // Tuio connections
            s_client = new TuioClient(port);            
            s_client.addTuioListener(s_grafitiSurface);
            s_client.addTuioListener(s_demoObjectManager);
            s_client.addTuioListener(s_viewer);
            s_client.connect();


            // initialize glut library
            Glut.glutInit();

            // initialize viewer's graphic
            s_viewer.Init();

            // register callback functions
            Glut.glutKeyboardFunc(new Glut.KeyboardCallback(S_KeyPressed));
            Glut.glutSpecialFunc(new Glut.SpecialCallback(S_SpecialKeyPressed));
            Glut.glutDisplayFunc(new Glut.DisplayCallback(S_Display));
            Glut.glutReshapeFunc(new Glut.ReshapeCallback(S_Reshape));
            Glut.glutTimerFunc(40, new Glut.TimerCallback(S_Timer), 0);

            // main loop
            try
            {
                Glut.glutMainLoop();
            }
            catch (ThreadAbortException e)
            {
                Exit();
            }
        }

        private static void Exit()
        {
            s_client.disconnect();
            s_client.removeTuioListener(s_viewer);
            s_client.removeTuioListener(s_demoObjectManager);
            s_client.removeTuioListener(s_grafitiSurface);
            System.Environment.Exit(0);
        }



        #region Graphic and keyboard functions

        /// <summary>
        /// Initializes graphics
        /// </summary>
        private void Init()
        {
            // initialize the window settings
            Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_RGB);
            Glut.glutInitWindowSize(m_window_width, m_window_height);
            Glut.glutCreateWindow(m_name);

            Gl.glClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            Gl.glClearDepth(1.0f);
            //Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);
            //Gl.glShadeModel(Gl.GL_SMOOTH);
            Gl.glPushAttrib(Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

            ////////////
            //Gl.glClearColor(0f, 0f, 0f, 1f);
            //Gl.glShadeModel(Gl.GL_SMOOTH);
            //Gl.glClearDepth(1.0f);
            //Gl.glEnable(Gl.GL_DEPTH_TEST);
            //Gl.glDepthFunc(Gl.GL_LEQUAL);
            //Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);
            //Gl.glCullFace(Gl.GL_BACK);

            Glut.glutShowWindow();
        }

        /// <summary>
        /// Called when the window changes size
        /// </summary>
        /// <param name="w">new width of the window</param>
        /// <param name="h">new height of the window</param>
        private static void S_Reshape(int w, int h)
        {
            s_viewer.Reshape(w, h);
        }
        private void Reshape(int w, int h)
        {
            if (h == 0)     // Prevent A Divide By Zero By
                h = 1;		// Making Height Equal One

            Gl.glViewport(0, 0, w, h); // Reset The Current Viewport

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();

            //int[] vPort = new int[4];
            //Gl.glGetIntegerv(Gl.GL_VIEWPORT, vPort);
            //Gl.glOrtho(vPort[0], vPort[0] + vPort[2], vPort[1] + vPort[3], vPort[1], -1, 1);

            Glu.gluPerspective(90f, 4f / 3f, 0.1f, 100f);
            //Gl.glFrustum(0, w, h, 0, 0.1, 500);
            //Gl.glOrtho(0, w, h, 0, -1, 1);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
        }

        /// <summary>
        /// Redraws periodically the screen.
        /// </summary>
        /// <param name="val">period in ms</param>
        private static void S_Timer(int val)
        {
            Glut.glutTimerFunc(s_timerTime, S_Timer, 0);
            Glut.glutPostRedisplay();
        }

        /// <summary>
        /// Display callback function
        /// </summary>
        private static void S_Display()
        {
            s_viewer.Display();
        }
        private void Display()
        {
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
            Gl.glLoadIdentity();
            Gl.glScalef(1, -1, 1);
            Gl.glTranslatef(m_projectionX - m_projectionW / 2, m_projectionY - m_projectionH / 2, -5f);
            Gl.glTranslatef(m_projectionW / 2, m_projectionH / 2, 0);
            Gl.glRotatef(m_projectionYAngle, 1, 0, 0);
            Gl.glTranslatef(-m_projectionW / 2, -m_projectionH / 2, 0);
            Gl.glScalef(m_projectionW, m_projectionH, 0);



            lock (s_lock)
            {
                // draw finger groups
                foreach (DemoGroup demoGroup in m_demoGroups)
                    demoGroup.Draw1();

                if (m_displayCalculator)
                    m_calculator.Draw();

                foreach (DemoGroup demoGroup in m_demoGroups)
                    demoGroup.Draw2();
            }

            // draw objects
            s_demoObjectManager.Draw();


            //Gl.glPushMatrix();
            //    Gl.glTranslatef(0.2f, 0.3f, 0f);
            //    Gl.glScaled(0.001, -0.001, 1);
            //    Glut.glutStrokeString(Glut.GLUT_STROKE_ROMAN, "test stroke");
            //    Glut.glutBitmapString(Glut.GLUT_BITMAP_HELVETICA_10, "test bitmap");
            //Gl.glPopMatrix();

            if (m_displayCalibrationGrid)
                DrawCalibrationGrid();

            Glut.glutSwapBuffers();
        }

        private void DrawCalibrationGrid()
        {
            float grid_width = 7, grid_height = 7;

            Gl.glPushMatrix();
            Gl.glColor4d(1, 1, 1, 1);
            Gl.glLineWidth(3.0f);

            // grid
            Gl.glBegin(Gl.GL_LINES);
            for (float k = -0f; k < 1; k += 0.999f / (grid_width - 1.0f))
            {
                Gl.glVertex2f(k, 0.0f);
                Gl.glVertex2f(k, 1.0f);
            }

            for (float k = -0f; k < 1; k += 0.999f / (grid_height - 1.0f))
            {
                Gl.glVertex2f(0f, k);
                Gl.glVertex2f(1.0f, k);
            }
            Gl.glEnd();
            // circles
            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (float angle = 0.0f; angle < Math.PI * 2; angle += ((float)Math.PI / 40.0f))
                Gl.glVertex2d((Math.Sin(angle) / 2) + 0.5, (Math.Cos(angle) / 2) + 0.5);
            Gl.glEnd();

            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (float angle = 0.0f; angle < Math.PI * 2; angle += ((float)Math.PI / 30.0f))
                Gl.glVertex2d(Math.Sin(angle) * 0.33 + 0.5, Math.Cos(angle) * 0.33 + 0.5);
            Gl.glEnd();

            Gl.glBegin(Gl.GL_LINE_LOOP);
            for (float angle = 0.0f; angle < Math.PI * 2; angle += ((float)Math.PI / 30.0f))
                Gl.glVertex2d(Math.Sin(angle) * 0.165 + 0.5, Math.Cos(angle) * 0.165 + 0.5);
            Gl.glEnd();
            Gl.glPopMatrix();
        }

        private void ToggleFullScreen()
        {
            m_fullscreen = !m_fullscreen;
            if (m_fullscreen)
            {
                // get the size of the window
                int[] viewport = new int[4];
                Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
                m_window_width = viewport[2];
                m_window_height = viewport[3];

                //RECT rect = new RECT();
                //if (GetWindowRect( ?, out rect))
                m_window_left = 0;// xPos;
                m_window_top = 0;// yPos;
                Glut.glutFullScreen();
            }
            else
            {
                Glut.glutPositionWindow(m_window_left, m_window_top);
                Glut.glutReshapeWindow(m_window_width, m_window_height);
            }
        }

        #region Keyboard input functions
        private static void S_KeyPressed(byte key, int x, int y)
        {
            s_viewer.KeyPressed(key, x, y);
        }
        private void KeyPressed(byte key, int x, int y)
        {
            switch (key)
            {
                case (byte)27: // ESC
                    Thread.CurrentThread.Abort();
                    break;

                case (byte)'c':
                    m_displayCalibrationGrid = !m_displayCalibrationGrid;
                    break;

                case (byte)'f':
                    ToggleFullScreen();
                    break;
            }


            // force re-display
            Glut.glutPostRedisplay();

            //HWND z;
        }

        private static void S_SpecialKeyPressed(int key, int x, int y)
        {
            s_viewer.SpecialKeyPressed(key, x, y);
        }
        private void SpecialKeyPressed(int key, int x, int y)
        {
            int mod = Glut.glutGetModifiers();

            if (m_displayCalibrationGrid)
            {
                switch (mod)
                {
                    case 0: // no modifiers
                        switch ((char)key)
                        {
                            case (char)Glut.GLUT_KEY_RIGHT:
                                m_projectionX += 0.01f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_LEFT:
                                m_projectionX -= 0.01f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_DOWN:
                                m_projectionY += 0.01f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_UP:
                                m_projectionY -= 0.01f;
                                PrintProjectionParameters();
                                break;
                        }
                        break;

                    case Glut.GLUT_ACTIVE_SHIFT:
                        switch ((char)key)
                        {
                            case (char)Glut.GLUT_KEY_RIGHT:
                                m_projectionW += 0.01f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_LEFT:
                                m_projectionW -= 0.01f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_DOWN:
                                m_projectionH -= 0.01f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_UP:
                                m_projectionH += 0.01f;
                                PrintProjectionParameters();
                                break;
                        }
                        break;

                    case Glut.GLUT_ACTIVE_ALT:
                        switch ((char)key)
                        {
                            case (char)Glut.GLUT_KEY_DOWN:
                                m_projectionYAngle -= 0.1f;
                                PrintProjectionParameters();
                                break;
                            case (char)Glut.GLUT_KEY_UP:
                                m_projectionYAngle += 0.1f;
                                PrintProjectionParameters();
                                break;
                        }
                        break;
                }
            }

            switch ((char)key)
            {
                case (char)Glut.GLUT_KEY_F1:
                    ToggleFullScreen();
                    break;

                //case (char)Glut.GLUT_KEY_F2:
                //    if (s_timerTime > 10)
                //    {
                //        s_timerTime--;
                //        Console.WriteLine("Redraw timer cycle: " + s_timerTime + "ms (~" + (int)(1000f / (float)s_timerTime) + " fps).");
                //    }
                //    break;

                //case (char)Glut.GLUT_KEY_F3:
                //    s_timerTime++;
                //    Console.WriteLine("Redraw timer cycle: " + s_timerTime + "ms (~" + (int)(1000f / (float)s_timerTime) + " fps).");
                //    break;
            }

            // force re-display
            Glut.glutPostRedisplay();
        }

        private void PrintProjectionParameters()
        {
            Console.Write("Projection params:");
            Console.Write(" X = " + m_projectionX);
            Console.Write(", Y = " + m_projectionY);
            Console.Write(", W = " + m_projectionW);
            Console.Write(", H = " + m_projectionH);
            Console.Write(", A = " + m_projectionYAngle);
            Console.WriteLine();
        }


        //[DllImport("user32.dll", SetLastError = true)] 
        //static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        //[StructLayout(LayoutKind.Sequential)]
        //public struct RECT
        //{
        //    public int X;
        //    public int Y;
        //    public int Width;
        //    public int Height;
        //}
        #endregion
        #endregion
 
    }
}