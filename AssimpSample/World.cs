// -----------------------------------------------------------------------
// <file>World.cs</file>
// <copyright>Grupa za Grafiku, Interakciju i Multimediju 2013.</copyright>
// <author>Srđan Mihić</author>
// <author>Aleksandar Josić</author>
// <summary>Klasa koja enkapsulira OpenGL programski kod.</summary>
// -----------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Imaging;
using Assimp;
using System.IO;
using System.Reflection;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Quadrics;
using SharpGL.SceneGraph.Core;
using SharpGL;
using System.Windows.Threading;

namespace AssimpSample
{


    /// <summary>
    ///  Klasa enkapsulira OpenGL kod i omogucava njegovo iscrtavanje i azuriranje.
    /// </summary>
    public class World : IDisposable
    {
        #region Atributi
        /// <summary>
        ///	 Scena koja se prikazuje.
        /// </summary>
        private AssimpScene m_scene;
        private AssimpScene m_scene2;

        private float stick_length = 5.0f;
        private float stick_rotation = 0.0f;
        private float light1ambientRed = 0.9f;
        private float light1ambientGreen = 0.6f;
        private float light1ambientBlue = 0.6f;

        private bool animation_going = false;

        private float x_cylinder_move = 0.0f;
        private float y_cylinder_move = 0.0f;
        private float z_cylinder_move = 0.0f;

        private float x_white_move = 0.0f;
        private float z_white_move = 0.0f;
        private float x_red_move = 0.0f;
        private float y_red_move = 0.0f;
        private float z_red_move = 0.0f;

        private double miliseconds = 10;

        private enum Action { SET_STICK, HIT, WHITE_GOES, RED_GOES, FALL }

        private Action action; 

        private DispatcherTimer timer;

        /// <summary>
        ///	 Ugao rotacije sveta oko X ose.
        /// </summary>
        private float m_xRotation = 0.0f;

        /// <summary>
        ///	 Ugao rotacije sveta oko Y ose.
        /// </summary>
        private float m_yRotation = 0.0f;

        /// <summary>
        ///	 Udaljenost scene od kamere.
        /// </summary>
        private float m_sceneDistance = 0.0f;

        /// <summary>
        ///	 Sirina OpenGL kontrole u pikselima.
        /// </summary>
        private int m_width;

        /// <summary>
        ///	 Visina OpenGL kontrole u pikselima.
        /// </summary>
        private int m_height;

        private enum TextureObjects { Carpet = 0, Stick, Wall, Ball, WhiteBall };
        private readonly int m_textureCount = Enum.GetNames(typeof(TextureObjects)).Length;


        private uint[] m_textures = null;

        private string[] m_textureFiles = { "..//..//images//carpet2.jpg", "..//..//images//wood.jpg", "..//..//images//wall5.jpg", "..//..//images//ballmat.png", "..//..//images//whiteball.jpg" };


        #endregion Atributi

        #region Properties

        /// <summary>
        ///	 Scena koja se prikazuje.
        /// </summary>
        public AssimpScene Scene
        {
            get { return m_scene; }
            set { m_scene = value; }
        }

        /// <summary>
        ///	 Ugao rotacije sveta oko X ose.
        /// </summary>
        public float RotationX
        {
            get { return m_xRotation; }
            set { m_xRotation = value; }
        }
        public float StickLength
        {
            get { return stick_length; }
            set { stick_length = value; }
        }

        public float StickRotation
        {
            get { return stick_rotation; }
            set { stick_rotation = value; }
        }

        public float AmbientRed
        {
            get { return light1ambientRed; }
            set { light1ambientRed = value; }
        }

        public float AmbientGreen
        {
            get { return light1ambientGreen; }
            set { light1ambientGreen = value; }
        }

        public float AmbientBlue
        {
            get { return light1ambientBlue; }
            set { light1ambientBlue = value; }
        }

        public bool AnimationGoing
        {
            get { return animation_going; }
            set { animation_going = value; }
        }

        /// <summary>
        ///	 Ugao rotacije sveta oko Y ose.
        /// </summary>
        public float RotationY
        {
            get { return m_yRotation; }
            set { m_yRotation = value; }
        }

        /// <summary>
        ///	 Udaljenost scene od kamere.
        /// </summary>
        public float SceneDistance
        {
            get { return m_sceneDistance; }
            set { m_sceneDistance = value; }
        }

        /// <summary>
        ///	 Sirina OpenGL kontrole u pikselima.
        /// </summary>
        public int Width
        {
            get { return m_width; }
            set { m_width = value; }
        }

        /// <summary>
        ///	 Visina OpenGL kontrole u pikselima.
        /// </summary>
        public int Height
        {
            get { return m_height; }
            set { m_height = value; }
        }

        #endregion Properties

        #region Konstruktori

        /// <summary>
        ///  Konstruktor klase World.
        /// </summary>
        public World(String scenePath, String sceneFileName, String sceneFileName2, int width, int height, OpenGL gl)
        {
            this.m_scene = new AssimpScene(scenePath, sceneFileName, gl);
            this.m_scene2 = new AssimpScene(scenePath, sceneFileName2, gl);
            this.m_width = width;
            this.m_height = height;

            m_textures = new uint[m_textureCount];
        }

        /// <summary>
        ///  Destruktor klase World.
        /// </summary>
        ~World()
        {
            this.Dispose(false);
        }

        #endregion Konstruktori

        #region Metode

        /// <summary>
        ///  Korisnicka inicijalizacija i podesavanje OpenGL parametara.
        /// </summary>
        public void Initialize(OpenGL gl)
        {
            gl.FrontFace(OpenGL.GL_CCW);
            gl.Enable(OpenGL.GL_CULL_FACE);
            gl.Enable(OpenGL.GL_DEPTH_TEST);

            gl.Enable(OpenGL.GL_TEXTURE_2D);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_MODULATE);

            gl.GenTextures(m_textureCount, m_textures);
            for (int i = 0; i < m_textureCount; ++i)
            {
                // Pridruzi teksturu odgovarajucem identifikatoru
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[i]);

                // Ucitaj sliku i podesi parametre teksture 
                Bitmap image = new Bitmap(m_textureFiles[i]);
                // rotiramo sliku zbog koordinantog sistema opengl-a
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                // RGBA format (dozvoljena providnost slike tj. alfa kanal)
                BitmapData imageData = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                                      System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                gl.Build2DMipmaps(OpenGL.GL_TEXTURE_2D, (int)OpenGL.GL_RGBA8, image.Width, image.Height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, imageData.Scan0);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);		// Linear Filtering
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);		// Linear Filtering


                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_REPEAT);
                gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_REPEAT);

                /*
                gl.Enable(OpenGL.GL_TEXTURE_GEN_S);
                gl.Enable(OpenGL.GL_TEXTURE_GEN_T);

                gl.Hint(OpenGL.GL_GENERATE_MIPMAP_SGIS, OpenGL.GL_NICEST);
                gl.Hint(OpenGL.GL_PERSPECTIVE_CORRECTION_HINT, OpenGL.GL_NICEST);
                gl.Hint(OpenGL.GL_POLYGON_SMOOTH_HINT, OpenGL.GL_NICEST);
                gl.Hint(OpenGL.GL_TEXTURE_COMPRESSION_HINT, OpenGL.GL_NICEST);
                gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
                gl.Hint(OpenGL.GL_POINT_SMOOTH_HINT, OpenGL.GL_NICEST);*/

                image.UnlockBits(imageData);
                image.Dispose();
            }

            SetupLighting(gl);
            
            m_scene.LoadScene();
            m_scene.Initialize();
            m_scene2.LoadScene();
            m_scene2.Initialize();
        }

        private void SetupLighting(OpenGL gl)
        {
            
            //float[] global_ambient = new float[] { 0.4f, 0.4f, 0.4f, 1.0f };
            //gl.LightModel(OpenGL.GL_LIGHT_MODEL_AMBIENT, global_ambient);

            float[] light0pos = new float[] { 0.0f, 4.0f, 50.0f, 1.0f };
            float[] light0ambient = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };
            float[] light0diffuse = new float[] { 0.7f, 0.7f, 0.7f, 1.0f };
            float[] light0specular = new float[] { 0.9f, 0.9f, 0.9f, 1.0f };
            //float[] light0emission = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };

            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, light0pos);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, light0ambient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, light0diffuse);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, light0specular);
            //gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_EMISSION, light0emission);

            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPOT_CUTOFF, 180.0f);

            gl.Enable(OpenGL.GL_LIGHTING);
            gl.Enable(OpenGL.GL_LIGHT0);


            float[] light1pos = new float[] { 0.0f, 0.0f, 50.0f, 1.0f };
            float[] light1ambient = new float[] { light1ambientRed, light1ambientGreen, light1ambientBlue, 1.0f };
            float[] light1diffuse = new float[] { 0.4f, 0.4f, 0.4f, 1.0f };
            float[] light1specular = new float[] { 1.0f, 0.0f, 0.0f, 1.0f };
            //float[] light1emission = new float[] { 1.0f, 0.0f, 0.0f, 1.0f };
            float[] light1direction = new float[] { 0.0f, -1.0f, 0.0f };
            
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_POSITION, light1pos);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_AMBIENT, light1ambient);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_DIFFUSE, light1diffuse);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPECULAR, light1specular);
           // gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_EMISSION, light1emission);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_DIRECTION, light1direction);
            gl.Enable(OpenGL.GL_LIGHT1);
            gl.Light(OpenGL.GL_LIGHT1, OpenGL.GL_SPOT_CUTOFF, 25.0f);

            //gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SPECULAR, light1specular);
            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SHININESS, 100.0f);

            //Uikljuci color tracking mehanizam
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);

            // Podesi na koje parametre materijala se odnose pozivi glColor funkcije
            gl.ColorMaterial(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_AMBIENT_AND_DIFFUSE);

            // Ukljuci automatsku normalizaciju nad normalama
            gl.Enable(OpenGL.GL_NORMALIZE);

            //m_normals = LightingUtilities.ComputeVertexNormals(m_vertices);

            gl.ShadeModel(OpenGL.GL_SMOOTH);
            
        }

        /// <summary>
        ///  Iscrtavanje OpenGL kontrole.
        /// </summary>
        
        public void StartAnimation()
        {
            animation_going = true;
            miliseconds = 10;
            action = Action.SET_STICK;
            stick_length = 5.0f;
            stick_rotation = -25.0f;
            x_cylinder_move = 0.0f;
            y_cylinder_move = 0.0f;
            z_cylinder_move = 0.0f;
            x_white_move = 0.0f;
            z_white_move = 0.0f;
            x_red_move = 0.0f;
            y_red_move = 0.0f;
            z_red_move = 0.0f;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(miliseconds);
            timer.Tick += new EventHandler(UpdateAnimation);
            timer.Start();
        }

        private void UpdateAnimation(object sender, EventArgs e)
        {
            if(action == Action.SET_STICK)
            {
                if (x_cylinder_move > 10.0f)
                    action = Action.HIT;
                if (x_cylinder_move < 7.5f)
                {
                    x_cylinder_move += 0.15f;
                    y_cylinder_move -= 0.2f;
                }
                else
                    x_cylinder_move += 0.2f;
            }
            else if(action == Action.HIT)
            {
                if (x_cylinder_move < 4.5f)
                    action = Action.WHITE_GOES;
                else
                {
                    timer.Interval = TimeSpan.FromMilliseconds(5);
                    x_cylinder_move -= 0.7f;
                }

            }
            else if(action == Action.WHITE_GOES)
            {
                if (x_white_move <= -9.0f)
                    action = Action.RED_GOES;
                else
                    x_white_move -= 0.3f;
            }
            else if(action == Action.RED_GOES)
            {
                if (x_red_move <= -4.3)
                    action = Action.FALL;
                else
                {
                    x_red_move -= 0.1f;
                    z_red_move -= 0.1f;
                    if (!(x_white_move <= -12.0f))
                    {
                        x_white_move -= 0.07f;
                        z_white_move += 0.04f;
                    }
                }
            }
            else if(action == Action.FALL)
            {
                if (y_red_move < -1.0f)
                {
                    animation_going = false;
                    timer.Stop();
                }
                else
                {
                    y_red_move -= 0.2f;
                }
            }
            
        }
        public void Draw(OpenGL gl)
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            SetupLighting(gl);
            gl.Viewport(0, 0, m_width, m_height);
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Perspective(60f, (double)m_width / m_height, 1f, 20000f);
            gl.LookAt(0.0f, 15.0f, 10.0f, 0, 10, 0, 0, 1, 0);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

            //float[] smer = { 0.0f, 0.0f, -1.0f };

            //gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPOT_DIRECTION, smer);


            //float[] m_spotPosition = new float[] { 0.0f, 5.0f, 50.0f, 1.0f };
            //gl.Translate(m_spotPosition[0], m_spotPosition[1], m_spotPosition[2]);
            gl.PushMatrix();
            gl.Translate(0.0f, 0.0f, m_sceneDistance);
            gl.Rotate(m_xRotation, 1.0f, 0.0f, 0.0f);
            gl.Rotate(m_yRotation, 0.0f, 1.0f, 0.0f);


            drawTable(gl);

            drawBall(gl);
            drawBall2(gl);
           
            gl.Color(0.8f, 0.8f, 0.8f);
            drawCube(gl);
            drawCube2(gl);
            drawCube3(gl);

            gl.Color(1.0f, 1.0f, 1.0f);
            drawQuad(gl);

            gl.Color(0.7f, 0.7f, 0.7f);
            drawCylinder(gl);

            gl.Viewport((m_width / 4) * 3, 0, m_width / 4, m_height / 2);
            drawText(gl);

            gl.PopMatrix();

            gl.Flush();
        }

        private void drawQuad(OpenGL gl)
        {
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_DECAL);
            gl.PushMatrix();
            gl.Translate(0.0f, -10.0f, -30);
            gl.Scale(5.0f, 1.0f, 1.0f);

            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Carpet]);

            gl.MatrixMode(OpenGL.GL_TEXTURE);
            gl.PushMatrix();
            gl.Scale(1.5f, 1.5f, 1.5f);

            gl.Begin(OpenGL.GL_QUADS);
            //gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Carpet]);
            gl.Normal(0.0f, 1.0f, 0.0f);
            gl.TexCoord(0.0f, 0.0f);
            gl.Vertex(-5, 0, -40);
            gl.TexCoord(0.0f, 1.0f);
            gl.Vertex(-5, 0, 5);
            gl.TexCoord(1.0f, 1.0f);
            gl.Vertex(5, 0, 5);
            gl.TexCoord(1.0f, 0.0f);
            gl.Vertex(5, 0, -40);
            gl.End();

            gl.PopMatrix();
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_MODULATE);
        }

        private void drawCube(OpenGL gl)
        {
            Cube cube = new Cube();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Wall]);
            gl.PushMatrix();
            gl.Translate(0.0f, 2.0f, -70.0f);
            gl.Scale(25.0f, 12.0f, 0.5f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            cube.Render(gl, RenderMode.Render);
            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
        }

        private void drawCube2(OpenGL gl)
        {
            Cube cube = new Cube();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Wall]);
            gl.PushMatrix();
            gl.Translate(-25.0f, 2.0f, -47.0f);
            gl.Rotate(0.0f, -90.0f, -0.0f);
            gl.Scale(22.0f, 12.0f, 0.5f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            cube.Render(gl, RenderMode.Render);
            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
        }

        private void drawCube3(OpenGL gl)
        {
            Cube cube = new Cube();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Wall]);
            gl.PushMatrix();
            gl.Translate(25.0f, 2.0f, -47.0f);
            gl.Rotate(0.0f, 90.0f, -0.0f);
            gl.Scale(22.0f, 12.0f, 0.5f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            cube.Render(gl, RenderMode.Render);
            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
        }

        private void drawCylinder(OpenGL gl)
        {
            Cylinder cylinder = new Cylinder();
            cylinder.NormalGeneration = Normals.Smooth;
            cylinder.CreateInContext(gl);
            cylinder.Height = 0.2f;
            cylinder.BaseRadius = 0.9f;
            cylinder.TopRadius = 0.9f;
            gl.PushMatrix();
            gl.Translate(5.0f + x_cylinder_move, 5.0f + y_cylinder_move, -45.0f + z_cylinder_move);
            gl.Rotate(90.0f, 30.0f, 0.0f);
            gl.Rotate(0.0f, stick_rotation, 0.0f);
            gl.Scale(stick_length, 1.0f, 1.0f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            cylinder.TextureCoords = true;
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Stick]);
            cylinder.Render(gl, SharpGL.SceneGraph.Core.RenderMode.Render);
            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
        }

        private void drawText(OpenGL gl)
        {
            int space = m_height/16;
            gl.DrawText(0, 5 * space, 255.0f, 255.0f, 255.0f, "Arial Bold", m_width/80, "Predmet: Racunarska grafika");
            gl.DrawText(0, 4 * space, 255.0f, 255.0f, 255.0f, "Arial Bold", m_width / 80, "Sk.god: 2020/21");
            gl.DrawText(0, 3 * space, 255.0f, 255.0f, 255.0f, "Arial Bold", m_width / 80, "Ime: Andrej");
            gl.DrawText(0, 2 * space, 255.0f, 255.0f, 255.0f, "Arial Bold", m_width / 80, "Prezime: Hlozan");
            gl.DrawText(0, space, 255.0f, 255.0f, 255.0f, "Arial Bold", m_width / 80, "Sifra zad: 1.1-Bilijar");
        }

        private void drawTable(OpenGL gl)
        {
            gl.PushMatrix();
            gl.Translate(-3.0f, -10.0f, -45.0f);
            gl.Scale(0.25f, 0.15f, 0.25f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            m_scene.Draw();
            gl.PopMatrix();
        }

        private void drawBall(OpenGL gl)
        {
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_DECAL);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.Ball]);
            gl.PushMatrix();
            gl.Translate(-6.0f + x_red_move, -5.5f + y_red_move, -45.0f + z_red_move);
            gl.Scale(0.5f, 0.5f, 0.5f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            m_scene2.Draw();
            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_MODULATE);
        }

        private void drawBall2(OpenGL gl)
        {
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_DECAL);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, m_textures[(int)TextureObjects.WhiteBall]);
            gl.PushMatrix();
            gl.Translate(4.0f + x_white_move, -5.5f, -45.0f + z_white_move);
            gl.Scale(0.5f, 0.5f, 0.5f);
            //gl.Rotate(m_xRotation, 0.0f, 0.0f);
            //gl.Rotate(0.0f, m_yRotation, 0.0f);
            m_scene2.Draw();
            gl.PopMatrix();
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, 0);
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_MODULATE);
        }

        /// <summary>
        /// Podesava viewport i projekciju za OpenGL kontrolu.
        /// </summary>
        public void Resize(OpenGL gl, int width, int height)
        {
            m_width = width;
            m_height = height;
            gl.Viewport(0, 0, m_width, m_height);
            gl.MatrixMode(OpenGL.GL_PROJECTION);      // selektuj Projection Matrix
            gl.LoadIdentity();
            gl.Perspective(60f, (double)width / height, 1f, 20000f);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();                // resetuj ModelView Matrix
        }

        /// <summary>
        ///  Implementacija IDisposable interfejsa.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_scene.Dispose();
            }
        }

        #endregion Metode

        #region IDisposable metode

        /// <summary>
        ///  Dispose metoda.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable metode
    }
}
