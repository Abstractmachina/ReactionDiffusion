using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ReactionDiffusion
{
    public class ReactionDiffusionInfo : GH_AssemblyInfo
    {
        public override string Name {
            get
            {
                return "ReactionDiffusion";
            }
        }
        public override Bitmap Icon {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id {
            get
            {
                return new Guid("ff5b6c24-6dcf-4879-864d-e451e9cab734");
            }
        }

        public override string AuthorName {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
