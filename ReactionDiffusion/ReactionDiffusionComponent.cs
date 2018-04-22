using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace ReactionDiffusion
{
    public class ReactionDiffusionComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ReactionDiffusionComponent()
          : base("ReactionDiffusion", "RD",
              "Description",
              "Generative", "Field-based")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset Simulation", "RES", "Clear and Reload all values.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Run Simulation", "RUN", "Run Simulation", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Diffusion Rate A", "dA", "Determines how well chemical A diffuses.", GH_ParamAccess.item, 1d);
            pManager.AddNumberParameter("Diffusion Rate B", "dB", "Determines how well chemical B diffuses.", GH_ParamAccess.item, 0.3d);
            pManager.AddNumberParameter("Feed Rate", "F", "Rate of how fast B is fed in.", GH_ParamAccess.item, 0.055d);
            pManager.AddNumberParameter("Kill Rate", "K", "Rate of how fast A is killed.", GH_ParamAccess.item, 0.062d);
            pManager.AddNumberParameter("Grid Width", "X", "Resolution in X.", GH_ParamAccess.item, 100d);
            pManager.AddNumberParameter("Grid Height", "Y", "Resolution in Y.", GH_ParamAccess.item, 100d);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Debug Log", "OUT", "Message log for debugging.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh Output", "M", "Mesh output for visualization.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData(0, ref reset)) return;
            if (!DA.GetData(1, ref run)) return;

            if (reset)
            {
                double tempx = 0;
                double tempy = 0;
                double dA = 0;
                double dB = 0;
                double kill = 0;
                double feed = 0;

                if (!DA.GetData(2, ref dA)) return;
                if (!DA.GetData(3, ref dB)) return;
                if (!DA.GetData(4, ref feed)) return;
                if (!DA.GetData(5, ref kill)) return;
                if (!DA.GetData(6, ref tempx)) return;
                if (!DA.GetData(7, ref tempy)) return;

                int xRes = (int)tempx; //convert double into int
                int yRes = (int)tempy;

                Setup(xRes, yRes, dA, dB, feed, kill);
            }

            if (run) Update();

            DA.SetDataList(0, debugLog);
            DA.SetDataList(1, rd.pixelsOut);
        }

        bool reset, run;

        ReactionDiffuser rd;
        public static List<string> debugLog = new List<string>();
        public static int frameCount;


        public void Setup(int xRes_, int yRes_, double dA_, double dB_, double feed_, double kill_)
        {
            rd = new ReactionDiffuser(xRes_, yRes_, dA_, dB_, feed_, kill_);
            rd.Seed(50, 60, 50, 60);
            //Seed(20, 30, 60, 70);
            frameCount = 0;
        }


        public void Update()
        {
            debugLog.Clear();
            rd.Update();
            frameCount++;
            debugLog.Add(frameCount.ToString());
        }


        //===============================================================================

        //add classes, functions

        public class ReactionDiffuser
        {
            //world holds no geometry. pure relative location + chemical ratio
            private Cell[,] grid;
            private Cell[,] nextGrid;
            //geom for output
            public List<Mesh> pixelsOut = new List<Mesh>();

            private int xRes, yRes;
            private Random r = new Random();

            public int XRes {
                get { return xRes; }
                set { xRes = value; }
            }
            public int YRes {
                get { return YRes; }
                set { yRes = value; }
            }

            //diffusion parameters

            private double dA = 1f;
            private double dB = 0.3f;
            private double feed = 0.055f;
            private double kill = 0.062f;

            public double DA {
                get { return dA; }
                set { dA = value; }
            }
            public double DB {
                get { return dB; }
                set { dB = value; }
            }

            public double Feed {
                get { return feed; }
                set { feed = value; }
            }

            public double Kill {
                get { return kill; }
                set { kill = value; }
            }


            private int counter;

            public ReactionDiffuser(int xRes_, int yRes_, double dA_, double dB_, double feed_, double kill_)
            {
                xRes = xRes_;
                yRes = yRes_;
                dA = dA_;
                dB = dB_;
                feed = feed_;
                kill = kill_;
                CreateGrid();
                nextGrid = grid;
            }

            private void CreateGrid()
            {
                grid = new Cell[xRes, yRes];
                for (int i = 0; i < xRes; i++)
                {
                    for (int j = 0; j < yRes; j++)
                    {
                        double ca = 1d;
                        double cb = 0d;
                        Cell c = new Cell(ca, cb);
                        grid[i, j] = c;
                    }
                }
            }

            public void Update()
            {
                
                counter++;
                if (counter > 5)
                {
                    pixelsOut = MeshRender();
                    counter = 0;
                }
                ReactionDiffuse();
                SwapGrid();
            }

            private void SwapGrid()
            {
                Cell[,] tempGrid = grid;
                grid = nextGrid;
                nextGrid = tempGrid;
            }

            private void ReactionDiffuse()
            {
                double deltaTime = 1f;
                for (int x = 1; x < nextGrid.GetLength(0) - 1; x++)
                {
                    for (int y = 1; y < nextGrid.GetLength(1) - 1; y++)
                    {
                        double a = grid[x, y].a;
                        double b = grid[x, y].b;

                        double rA = a + dA * LaPlaceA(x, y) - a * b * b + feed * (1 - a) * deltaTime;
                        double rB = b + dB * LaPlaceB(x, y) + a * b * b - (kill + feed) * b * deltaTime;

                        rA = Constrain(rA, 0, 1d);
                        rB = Constrain(rB, 0, 1d);

                        nextGrid[x, y].a = rA;
                        nextGrid[x, y].b = rB;
                    }
                }
            }

            private double LaPlaceA(int x, int y)
            {
                double sumA = 0;
                sumA += grid[x, y].a * -1;
                sumA += grid[x - 1, y].a * 0.2;
                sumA += grid[x + 1, y].a * 0.2;
                sumA += grid[x, y + 1].a * 0.2;
                sumA += grid[x, y - 1].a * 0.2;
                sumA += grid[x - 1, y - 1].a * 0.05;
                sumA += grid[x + 1, y - 1].a * 0.05;
                sumA += grid[x + 1, y + 1].a * 0.05;
                sumA += grid[x - 1, y + 1].a * 0.05;
                return sumA;
            }
            private double LaPlaceB(int x, int y)
            {
                double sumB = 0;
                sumB += grid[x, y].b * -1;
                sumB += grid[x - 1, y].b * 0.2;
                sumB += grid[x + 1, y].b * 0.2;
                sumB += grid[x, y + 1].b * 0.2;
                sumB += grid[x, y - 1].b * 0.2;
                sumB += grid[x - 1, y - 1].b * 0.05;
                sumB += grid[x + 1, y - 1].b * 0.05;
                sumB += grid[x + 1, y + 1].b * 0.05;
                sumB += grid[x - 1, y + 1].b * 0.05;
                return sumB;
            }

            public void Seed(int xMin, int xMax, int yMin, int yMax)
            {
                for (int i = xMin; i < xMax; i++)
                {
                    for (int j = yMin; j < yMax; j++)
                    {
                        grid[i, j].a = 0;
                        grid[i, j].b = 1;
                    }
                }
            }


            //create a mesh-representation in 3d space for each pixel
            public List<Mesh> MeshRender()
            {

                List<Mesh> recs = new List<Mesh>();

                for (int i = 0; i < grid.GetLength(0); i++)
                {
                    for (int j = 0; j < grid.GetLength(1); j++)
                    {
                        double a = grid[i, j].a;
                        double b = grid[i, j].b;
                        int balance = (int)Constrain((a - b) * 255, 0d, 255d);
                        Color c = Color.FromArgb(255, balance, balance, balance);

                        Point3d p1 = new Point3d(i, j, 0);
                        Point3d p2 = new Point3d(p1.X + 1, p1.Y, 0);
                        Point3d p3 = new Point3d(p1.X + 1, p1.Y + 1, 0);
                        Point3d p4 = new Point3d(p1.X, p1.Y + 1, 0);

                        Mesh tempMesh = new Mesh();
                        tempMesh.Vertices.Add(p1);
                        tempMesh.VertexColors.Add(c);
                        tempMesh.Vertices.Add(p2);
                        tempMesh.VertexColors.Add(c);
                        tempMesh.Vertices.Add(p3);
                        tempMesh.VertexColors.Add(c);
                        tempMesh.Vertices.Add(p4);
                        tempMesh.VertexColors.Add(c);
                        tempMesh.Faces.AddFace(0, 1, 2, 3);
                        recs.Add(tempMesh);
                    }
                }
                return recs;
            }

        }



        public static double Constrain(double val, double min, double max)
        {
            if (val > max) val = max;
            else if (val < min) val = min;
            return val;
        }


        //Cell holding chemical ratio
        public struct Cell
        {
            public double a, b;

            public Cell(double a_, double b_)
            {
                a = a_;
                b = b_;
            }
        }







        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid("11744e20-5c94-4e5b-a5b3-5c4f537b9971"); }
        }
    }
}
