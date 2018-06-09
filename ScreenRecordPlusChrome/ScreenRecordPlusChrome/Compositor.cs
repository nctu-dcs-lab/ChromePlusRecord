/****************************************************************************
While the underlying libraries are covered by LGPL, this sample is released 
as public domain.  It is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  
*****************************************************************************/

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using Direct3D = SlimDX.Direct3D9;
using DirectShowLib;

using System.Collections.Generic;

namespace ScreenRecordPlusChrome
{
    public class Compositor : IVMRImageCompositor9, IDisposable
    {
        #region Variable
        private IntPtr unmanagedDevice;
        private Device device;
        private Sprite sprite;

        private Texture _mouseCursorTex;
        private Size _mouseCursorSize;
        private Texture _gazeCursorTex;
        private Size _gazeCursorSize;

        private Direct3D.Font _d3dMouseFixtionFont;
        private Direct3D.Font _d3dGazeFixtionFont;
        private System.Drawing.Font _gdiMouseFixtionFont;
        private System.Drawing.Font _gdiGazeFixtionFont;

        private Direct3D.Line _d3dMouseTrackLine;
        private Direct3D.Line _d3dMouseCursorCircleLine;
        private Direct3D.Line _d3dMouseFixationLine;
        private Direct3D.Line _d3dMouseScanpathLine;

        private Direct3D.Line _d3dGazeTrackLine;
        private Direct3D.Line _d3dGazeCursorCircleLine;
        private Direct3D.Line _d3dGazeFixationLine;
        private Direct3D.Line _d3dGazeScanpathLine;

        private List<Vector2> _mouseTrack = null;
        private List<Vector2> _gazeTrack = null;
        private List<FixationInfo> _mouseFixation = null;
        private List<FixationInfo> _gazeFixation = null;
        #endregion

        #region Mouse Control Variable
        public bool _mouseTrackDraw = true;
        public Color _mouseTrackColor = Color.Red;
        public int _mouseTrackLineWidth = 3;

        public bool _mouseCursorDraw = true;
        public bool _mouseCursorCircleDraw = true;
        public Color _mouseCursorCircleColor = Color.Red;
        public int _mouseCursorCircleLineWidth = 3;

        public bool _mouseFixationDraw = true;
        public Color _mouseFixationColor = Color.Red;
        public int _mouseFixationLineWidth = 3;

        public bool _mouseScanpathDraw = true;
        public Color _mouseScanpathColor = Color.Red;
        public int _mouseScanpathLineWidth = 3;

        public bool _mouseFixationIDDraw = true;
        public Color _mouseFixationIDColor = Color.Red;
        #endregion

        #region Gaze Control Variable
        public bool _gazeTrackDraw = true;
        public Color _gazeTrackColor = Color.Blue;
        public int _gazeTrackLineWidth = 3;

        public bool _gazeCursorDraw = true;
        public bool _gazeCursorCircleDraw = true;
        public Color _gazeCursorCircleColor = Color.Blue;
        public int _gazeCursorCircleLineWidth = 3;

        public bool _gazeFixationDraw = true;
        public Color _gazeFixationColor = Color.Blue;
        public int _gazeFixationLineWidth = 3;

        public bool _gazeScanpathDraw = true;
        public Color _gazeScanpathColor = Color.Blue;
        public int _gazeScanpathLineWidth = 3;

        public bool _gazeFixationIDDraw = true;
        public Color _gazeFixationIDColor = Color.Blue;
        #endregion

        public Compositor()
        {
            //Device.IsUsingEventHandlers = false;
        }

        private Vector2[] InitCircle(int CircleResolution, Vector2 center, double size)
        {
            Vector2[] v = new Vector2[CircleResolution + 1];
            for (int i = 0; i < CircleResolution; ++i)
            {
                double x = Math.Cos(2 * Math.PI * i / CircleResolution) * size + center.X;
                double y = Math.Sin(2 * Math.PI * i / CircleResolution) * size + center.Y;
                v[i] = new Vector2((float)x, (float)y);
            }
            v[CircleResolution] = new Vector2((float)(Math.Cos(2 * Math.PI * 0 / CircleResolution) * size + center.X), (float)(Math.Sin(2 * Math.PI * 0 / CircleResolution) * size + center.Y));
            return v;
        }

        public void SetPartTrack(TrackData tinfo)
        {
            _mouseTrack = tinfo.mouseTracks;
            _gazeTrack = tinfo.gazeTracks;
            _mouseFixation = tinfo.mouseFixations;
            _gazeFixation = tinfo.gazeFixations;
            /*
            if (mousetrack == null)
                mousetrack = tinfo.mouseTrack;
            else
                mousetrack.AddRange(tinfo.mouseTrack);
            if (gazetrack == null)
                gazetrack = tinfo.gazeTrack;
            else
                gazetrack.AddRange(tinfo.gazeTrack);*/
        }

        #region IVMRImageCompositor9 Membres

        public int CompositeImage(IntPtr pD3DDevice, IntPtr pddsRenderTarget, AMMediaType pmtRenderTarget, long rtStart, long rtEnd, int dwClrBkGnd, VMR9VideoStreamInfo[] pVideoStreamInfo, int cStreams)
        {
            try
            {
                // Just in case the filter call CompositeImage before InitCompositionDevice (this sometime occure)
                if (unmanagedDevice != pD3DDevice)
                {
                    SetManagedDevice(pD3DDevice);
                }

                // Create a managed Direct3D surface (the Render Target) from the unmanaged pointer.
                // The constructor don't call IUnknown.AddRef but the "destructor" seem to call IUnknown.Release
                // Direct3D seem to be happier with that according to the DirectX log
                Marshal.AddRef(pddsRenderTarget);
                Surface renderTarget = Surface.FromPointer(pddsRenderTarget);
                SurfaceDescription renderTargetDesc = renderTarget.Description;
                Rectangle renderTargetRect = new Rectangle(0, 0, renderTargetDesc.Width, renderTargetDesc.Height);

                // Same thing for the first video surface
                // WARNING : This Compositor sample only use the video provided to the first pin.
                Marshal.AddRef(pVideoStreamInfo[0].pddsVideoSurface);
                Surface surface = Surface.FromPointer(pVideoStreamInfo[0].pddsVideoSurface);
                SurfaceDescription surfaceDesc = surface.Description;
                Rectangle surfaceRect = new Rectangle(0, 0, surfaceDesc.Width, surfaceDesc.Height);


                // Set the device's render target (this doesn't seem to be needed)
                device.SetRenderTarget(0, renderTarget);

                // Copy the whole video surface into the render target
                // it's a de facto surface cleaning...
                device.StretchRectangle(surface, surfaceRect, renderTarget, renderTargetRect, TextureFilter.None);

                // sprite's methods need to be called between device.BeginScene and device.EndScene
                device.BeginScene();

                // Init the sprite engine for AlphaBlending operations
                sprite.Begin(SpriteFlags.AlphaBlend | SpriteFlags.DoNotSaveState);

                //mouse
                if (_mouseTrack != null)
                {
                    if (_mouseTrack.Count > 1)
                    {
                        if (_mouseTrackDraw)
                        {
                            _d3dMouseTrackLine.Width = _mouseTrackLineWidth;
                            _d3dMouseTrackLine.Begin();
                            _d3dMouseTrackLine.Draw(_mouseTrack.ToArray(), _mouseTrackColor);
                            _d3dMouseTrackLine.End();
                        }
                    }
                    if (_mouseTrack.Count > 0)
                    {
                        if (_mouseCursorCircleDraw)
                        {
                            _d3dMouseCursorCircleLine.Width = _mouseCursorCircleLineWidth;
                            _d3dMouseCursorCircleLine.Begin();
                            _d3dMouseCursorCircleLine.Draw(InitCircle(100, _mouseTrack[_mouseTrack.Count - 1], 50), _mouseCursorCircleColor);
                            _d3dMouseCursorCircleLine.End();
                        }

                        if (_mouseCursorDraw)
                        {
                            Vector3 cursorPos = new Vector3(_mouseTrack[_mouseTrack.Count - 1], 0.5f);
                            sprite.Draw(_mouseCursorTex, Vector3.Zero, cursorPos, Color.White);
                        }
                    }
                }
                if (_mouseFixationDraw && _mouseFixation != null)
                {
                    if (_mouseFixation.Count > 0) {
                        List<Vector2> scanpath = new List<Vector2>();
                        for (int i = 0; i < _mouseFixation.Count; i++)
                        {
                            //Fixation
                            _d3dMouseFixationLine.Width = _mouseFixationLineWidth;
                            _d3dMouseFixationLine.Begin();
                            _d3dMouseFixationLine.Draw(InitCircle(100, _mouseFixation[i].fixation, _mouseFixation[i].fsize), _mouseFixationColor);
                            _d3dMouseFixationLine.End();

                            //ScanPath
                            scanpath.Add(_mouseFixation[i].fixation);
                            if (i >= 1 && _mouseScanpathDraw)
                            {
                                _d3dMouseScanpathLine.Width = _mouseScanpathLineWidth;
                                _d3dMouseScanpathLine.Begin();
                                _d3dMouseScanpathLine.Draw(scanpath.ToArray(), _mouseScanpathColor);
                                _d3dMouseScanpathLine.End();
                            }

                            //Fixation ID
                            if (_mouseFixationIDDraw)
                                _d3dMouseFixtionFont.DrawString(sprite, (i + 1).ToString(), new Rectangle(new Point((int)_mouseFixation[i].fixation.X, (int)_mouseFixation[i].fixation.Y), new Size(50, 50)), 0, _mouseFixationIDColor);
                        }
                    }
                }
                //gaze
                if (_gazeTrack != null) {
                    if (_gazeTrack.Count > 1) {
                        if (_gazeTrackDraw)
                        {
                            _d3dGazeTrackLine.Width = _gazeTrackLineWidth;
                            _d3dGazeTrackLine.Begin();
                            _d3dGazeTrackLine.Draw(_gazeTrack.ToArray(), _gazeTrackColor);
                            _d3dGazeTrackLine.End();
                        }
                    }
                    if (_gazeTrack.Count > 0) {
                        if (_gazeCursorCircleDraw)
                        {
                            _d3dGazeCursorCircleLine.Width = _gazeCursorCircleLineWidth;
                            _d3dGazeCursorCircleLine.Begin();
                            _d3dGazeCursorCircleLine.Draw(InitCircle(100, _gazeTrack[_gazeTrack.Count - 1], 50), _gazeCursorCircleColor);
                            _d3dGazeCursorCircleLine.End();
                        }

                        if (_gazeCursorDraw)
                        {
                            Vector3 cursorPos = new Vector3(_gazeTrack[_gazeTrack.Count - 1], 0.5f);
                            sprite.Draw(_gazeCursorTex, Vector3.Zero, cursorPos, Color.White);
                        }
                    }
                }
                if (_gazeFixationDraw && _gazeFixation != null)
                {
                    if (_gazeFixation.Count > 0)
                    {
                        List<Vector2> scanpath = new List<Vector2>();
                        for (int i = 0; i < _gazeFixation.Count; i++)
                        {
                            //Fixation
                            _d3dGazeFixationLine.Width = _gazeFixationLineWidth;
                            _d3dGazeFixationLine.Begin();
                            _d3dGazeFixationLine.Draw(InitCircle(100, _gazeFixation[i].fixation, _gazeFixation[i].fsize), _gazeFixationColor);
                            _d3dGazeFixationLine.End();

                            //ScanPath
                            scanpath.Add(_gazeFixation[i].fixation);
                            if (i >= 1 && _gazeScanpathDraw)
                            {
                                _d3dGazeScanpathLine.Width = _gazeScanpathLineWidth;
                                _d3dGazeScanpathLine.Begin();
                                _d3dGazeScanpathLine.Draw(scanpath.ToArray(), _gazeScanpathColor);
                                _d3dGazeScanpathLine.End();
                            }
                            //Fixation ID
                            if (_gazeFixationIDDraw)
                                _d3dGazeFixtionFont.DrawString(sprite, (i + 1).ToString(), new Rectangle(new Point((int)_gazeFixation[i].fixation.X, (int)_gazeFixation[i].fixation.Y), new Size(50, 50)), 0, _gazeFixationIDColor);
                        }
                    }
                }
                

                // End the spite engine (drawings take place here)
                sprite.Flush();
                sprite.End();


                // End the sceen. 
                device.EndScene();

                // No Present requiered because the rendering is on a render target... 
                // device.Present();

                // Dispose the managed surface
                surface.Dispose();
                surface = null;

                // and the managed render target
                renderTarget.Dispose();
                renderTarget = null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            // return a success to the filter
            return 0;
        }

        public int InitCompositionDevice(IntPtr pD3DDevice)
        {
            try
            {
                // Init the compositor with this unamanaged device
                if (unmanagedDevice != pD3DDevice)
                {
                    SetManagedDevice(pD3DDevice);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            // return a success to the filter
            return 0;
        }

        public int SetStreamMediaType(int dwStrmID, AMMediaType pmt, bool fTexture)
        {
            // This method is called many times with pmt == null
            if (pmt == null)
                return 0;

            // This sample don't use this method... but return a success
            return 0;
        }

        public int TermCompositionDevice(IntPtr pD3DDevice)
        {
            try
            {
                // Free the resources each time this method is called
                unmanagedDevice = IntPtr.Zero;
                FreeResources();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            // return a success to the filter
            return 0;
        }

        #endregion

        #region IDisposable Membres

        public void Dispose()
        {
            // free resources
            FreeResources();
        }

        #endregion

        private void FreeResources()
        {
            #region Text Dispose
            if (_d3dMouseFixtionFont != null)
            {
                _d3dMouseFixtionFont.Dispose();
                _d3dMouseFixtionFont = null;
            }
            if (_d3dGazeFixtionFont != null)
            {
                _d3dGazeFixtionFont.Dispose();
                _d3dGazeFixtionFont = null;
            }

            if (_gdiMouseFixtionFont != null)
            {
                _gdiMouseFixtionFont.Dispose();
                _gdiMouseFixtionFont = null;
            }
            if (_gdiGazeFixtionFont != null)
            {
                _gdiGazeFixtionFont.Dispose();
                _gdiGazeFixtionFont = null;
            }
            #endregion

            #region Texture Dispose
            if (_mouseCursorTex != null) {
                _mouseCursorTex.Dispose();
                _mouseCursorTex = null;
            }
            if (_gazeCursorTex != null) {
                _gazeCursorTex.Dispose();
                _gazeCursorTex = null;
            }
            #endregion

            #region Line Dispose
            if (_d3dMouseTrackLine != null)
            {
                _d3dMouseTrackLine.Dispose();
                _d3dMouseTrackLine = null;
            }

            if (_d3dMouseCursorCircleLine != null) {
                _d3dMouseCursorCircleLine.Dispose();
                _d3dMouseCursorCircleLine = null;
            }

            if (_d3dMouseFixationLine != null) {
                _d3dMouseFixationLine.Dispose();
                _d3dMouseFixationLine = null;
            }

            if (_d3dMouseScanpathLine != null) {
                _d3dMouseScanpathLine.Dispose();
                _d3dMouseScanpathLine = null;
            }

            if (_d3dGazeTrackLine != null)
            {
                _d3dGazeTrackLine.Dispose();
                _d3dGazeTrackLine = null;
            }

            if (_d3dGazeCursorCircleLine != null)
            {
                _d3dGazeCursorCircleLine.Dispose();
                _d3dGazeCursorCircleLine = null;
            }

            if (_d3dGazeFixationLine != null)
            {
                _d3dGazeFixationLine.Dispose();
                _d3dGazeFixationLine = null;
            }

            if (_d3dGazeScanpathLine != null)
            {
                _d3dGazeScanpathLine.Dispose();
                _d3dGazeScanpathLine = null;
            }
            #endregion

            if (sprite != null)
            {
                sprite.Dispose();
                sprite = null;
            }

            if (device != null)
            {
                device.Dispose();
                device = null;
            }
            
        }

        private void SetManagedDevice(IntPtr unmanagedDevice)
        {
            // Start by freeing everything
            FreeResources();

            // Create a managed Device from the unmanaged pointer
            // The constructor don't call IUnknown.AddRef but the "destructor" seem to call IUnknown.Release
            // Direct3D seem to be happier with that according to the DirectX log
            Marshal.AddRef(unmanagedDevice);
            this.unmanagedDevice = unmanagedDevice;
            device = Device.FromPointer(unmanagedDevice);


            //text
            sprite = new Sprite(device);
            _gdiMouseFixtionFont = new System.Drawing.Font("Tahoma", 30);
            _d3dMouseFixtionFont = new Direct3D.Font(device, _gdiMouseFixtionFont);
            _gdiGazeFixtionFont = new System.Drawing.Font("Tahoma", 30);
            _d3dGazeFixtionFont = new Direct3D.Font(device, _gdiGazeFixtionFont);

            //mouse
            _d3dMouseTrackLine = new Direct3D.Line(device);
            _d3dMouseTrackLine.Width = _mouseTrackLineWidth;

            _d3dMouseCursorCircleLine = new Direct3D.Line(device);
            _d3dMouseCursorCircleLine.Width = _mouseCursorCircleLineWidth;

            _d3dMouseFixationLine = new Direct3D.Line(device);
            _d3dMouseFixationLine.Width = _mouseFixationLineWidth;

            _d3dMouseScanpathLine = new Direct3D.Line(device);
            _d3dMouseScanpathLine.Width = _mouseScanpathLineWidth;

            //gaze
            _d3dGazeTrackLine = new Direct3D.Line(device);
            _d3dGazeTrackLine.Width = _gazeTrackLineWidth;

            _d3dGazeCursorCircleLine = new Direct3D.Line(device);
            _d3dGazeCursorCircleLine.Width = _gazeCursorCircleLineWidth;

            _d3dGazeFixationLine = new Direct3D.Line(device);
            _d3dGazeFixationLine.Width = _gazeFixationLineWidth;

            _d3dGazeScanpathLine = new Direct3D.Line(device);
            _d3dGazeScanpathLine.Width = _gazeScanpathLineWidth;

            // Load a png file to the teature
            _mouseCursorTex = Texture.FromFile(device, @"..\..\..\Resource\Picture\cursor1.png");
            SurfaceDescription spiderDesc = _mouseCursorTex.GetLevelDescription(0);
            _mouseCursorSize = new Size(spiderDesc.Width, spiderDesc.Height);

            _gazeCursorTex = Texture.FromFile(device, @"..\..\..\Resource\Picture\eyecursor.png");
            spiderDesc = _gazeCursorTex.GetLevelDescription(0);
            _gazeCursorSize = new Size(spiderDesc.Width, spiderDesc.Height);

        }

    }

}
