using DescriptorClustering.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptorClustering.Simple
{
    class ClusteringSimpleGPU : ClusteringSimpleBase
    {
        //protected float[]

        public ClusteringSimpleGPU(Descriptor[] descriptors) : base(descriptors)
        {
        }

        protected override void Assign()
        {
            throw new NotImplementedException();
        }

        protected override void AssignClosestDescriptors()
        {
            throw new NotImplementedException();
        }

        protected override int DropEmptyCentroids()
        {
            throw new NotImplementedException();
        }

        protected override double Update()
        {
            throw new NotImplementedException();
        }
    }
}
