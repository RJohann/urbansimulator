using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Urban_Simulator
{
    [System.Runtime.InteropServices.Guid("c4b106d6-e89a-47db-96ef-e200473f23d3")]
    public class UrbanSimulatorCommand : Command
    {
        public UrbanSimulatorCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }
    
        ///<summary>The only instance of this command.</summary>
        public static UrbanSimulatorCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "UrbanSimulator"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
            RhinoApp.WriteLine("The Urban Simulator has begun.");

            urbanModel theUrbanModel = new urbanModel();

            if (!getPrecinct(theUrbanModel))               //Ask user to select a surface representing the Precinct
                return Result.Failure;

            if (!generateRoadNetwork(theUrbanModel))       //Using the precint, Generate a road network
                 return Result.Failure;

            //createBlocks()                              //Using the road network, create block
            //subdivideBlock()                            //Subdivide the block into Plots
            //instantiateBuildings()                      //Place buildings on each plot      

            RhinoApp.WriteLine("The Urban Simulator is complete.");

            return Result.Success;
        }

        public bool getPrecinct(urbanModel model)
        {
            GetObject obj = new GetObject();
            obj.GeometryFilter = Rhino.DocObjects.ObjectType.Surface;
            obj.SetCommandPrompt ("Please select a surface representing your Precinct");
     

            GetResult res = obj.Get();

            if (res != GetResult.Object)
            {
                RhinoApp.WriteLine("User failed to select a surface.");
                return false;
            }

            if(obj.ObjectCount == 1)
                model.precintSrf = obj.Object(0).Surface();

            return true;
        
        }

        public bool generateRoadNetwork(urbanModel model)
        {

            int noIterations = 4;

            Random rndRoadT = new Random();

            List<Curve> obstCrvs = new List<Curve>();

            //extract the border from the precint surface
            Curve[] borderCrvs = model.precintSrf.ToBrep().DuplicateNakedEdgeCurves(true, false);

            foreach (Curve itCrv in borderCrvs)
                obstCrvs.Add(itCrv);

            if (borderCrvs.Length > 0)
            {
                int noBorders = borderCrvs.Length;

                Random rnd = new Random();
                Curve theCrv = borderCrvs[rnd.Next(noBorders)];

                recursivePerpLine(theCrv, ref obstCrvs, rndRoadT, 1, noIterations);
          
            }

            model.roadNetwork = obstCrvs;


            if(obstCrvs.Count > borderCrvs.Length)
                 return true;
            else
                return false;

        }

        public Boolean recursivePerpLine(Curve inpCrv, ref List<Curve> inpObst, Random inpRnd, int dir, int cnt)
        {
            if (cnt < 1)
                return false;

            // select random point on one of the edges
            double t = inpRnd.Next(20,80) / 100.0;
            Plane perpFrm;

            Point3d pt = inpCrv.PointAtNormalizedLength(t);
            inpCrv.PerpendicularFrameAt(t, out perpFrm);

            Point3d pt2 = Point3d.Add(pt, perpFrm.XAxis * dir);

            //draw a line perpendicular
            Line In = new Line(pt, pt2);
            Curve InExt = In.ToNurbsCurve().ExtendByLine(CurveEnd.End, inpObst);

            if (InExt == null)
                return false;

            inpObst.Add(InExt);
            
            RhinoDoc.ActiveDoc.Objects.AddLine(InExt.PointAtStart, InExt.PointAtEnd);
            RhinoDoc.ActiveDoc.Objects.AddPoint(pt);
            RhinoDoc.ActiveDoc.Views.Redraw();

            recursivePerpLine(InExt, ref inpObst, inpRnd, 1, cnt - 1);
            recursivePerpLine(InExt, ref inpObst, inpRnd, -1, cnt - 1);

            return true;
        

        }
    }
}
